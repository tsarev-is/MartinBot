using MartinBot.Backtesting;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Entities;
using MartinBot.Domain.Entities.Models;
using MartinBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace MartinBot.Private;

[ApiController]
[Route("private/walkforward")]
public sealed class WalkForwardController : ControllerBase
{
    private const int MaxCombinations = 1000;

    private readonly BotContext _db;
    private readonly WalkForwardQueue _queue;
    private readonly BacktestStrategyFactory _factory;

    public WalkForwardController(BotContext db, WalkForwardQueue queue, BacktestStrategyFactory factory)
    {
        _db = db;
        _queue = queue;
        _factory = factory;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Enqueue([FromBody] WalkForwardRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Pair) || string.IsNullOrWhiteSpace(body.Timeframe))
            return BadRequest(new { error = "pair and timeframe are required" });
        if (body.From >= body.To)
            return BadRequest(new { error = "from must be earlier than to" });
        if (body.InitialCash <= 0m)
            return BadRequest(new { error = "initialCash must be positive" });
        if (body.TrainDays <= 0 || body.TestDays <= 0 || body.StepDays <= 0)
            return BadRequest(new { error = "trainDays, testDays, and stepDays must all be positive" });

        var rangeDays = (body.To - body.From).TotalDays;
        if (body.TrainDays + body.TestDays > rangeDays)
            return BadRequest(new { error = "trainDays + testDays exceeds the available range" });

        var strategy = string.IsNullOrWhiteSpace(body.StrategyName)
            ? BacktestStrategyFactory.BuyAndHold
            : body.StrategyName;

        if (!OptimizationMetricSelector.TryParse(body.OptimizationMetric ?? "total_return", out var metric))
            return BadRequest(new { error = $"Unknown optimizationMetric '{body.OptimizationMetric}'" });

        if (body.ParameterGrid is not null)
        {
            foreach (var (key, values) in body.ParameterGrid)
            {
                if (values is null || values.Length == 0)
                    return BadRequest(new { error = $"parameterGrid['{key}'] must have at least one value" });
            }
        }

        var comboCount = ParameterGrid.CountCombinations(body.ParameterGrid);
        if (comboCount == 0)
            return BadRequest(new { error = "parameterGrid yields zero combinations" });
        if (comboCount > MaxCombinations)
            return BadRequest(new { error = $"parameterGrid yields {comboCount} combinations per window, exceeds limit of {MaxCombinations}" });

        try
        {
            var proxy = body.ParameterGrid?.ToDictionary(kv => kv.Key, _ => 0m);
            _factory.ValidateParameters(strategy, proxy);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var windowCount = WalkForwardWindowGenerator.Generate(body.From, body.To,
            TimeSpan.FromDays(body.TrainDays), TimeSpan.FromDays(body.TestDays),
            TimeSpan.FromDays(body.StepDays)).Count();
        if (windowCount == 0)
            return BadRequest(new { error = "range + train/test/step do not produce any windows" });

        var gridJson = ParameterGridSerializer.Serialize(body.ParameterGrid);
        var now = DateTimeOffset.UtcNow;
        var run = new WalkForwardRunEntity(id: 0, pair: body.Pair, timeframe: body.Timeframe,
            from: body.From, to: body.To, initialCash: body.InitialCash,
            feeBps: body.FeeBps, slippageBps: body.SlippageBps, strategyName: strategy,
            parameterGridJson: gridJson, optimizationMetric: metric,
            trainDays: body.TrainDays, testDays: body.TestDays, stepDays: body.StepDays,
            status: WalkForwardRunStatus.Queued,
            totalWindows: null, completedWindows: null,
            aggregateTotalReturn: null, aggregateMaxDrawdown: null, aggregateSharpe: null,
            errorMessage: null, startedAt: null, completedAt: null,
            createdAt: now, updatedAt: now);

        _db.AddWalkForwardRun(run);
        await _db.SaveChangesAsync(ct);
        await _queue.EnqueueAsync(run.Id, ct);

        return Accepted(new { runId = run.Id });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var run = await _db.GetWalkForwardRunWithWindowsAsync(id, ct);
        if (run is null)
            return NotFound();
        return Ok(run);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var runs = await _db.ListRecentWalkForwardRunsAsync(50, ct);
        return Ok(runs);
    }

    [HttpGet("{id:long}/report")]
    public async Task<IActionResult> Report(long id, CancellationToken ct)
    {
        var run = await _db.GetWalkForwardRunWithWindowsAsync(id, ct);
        if (run is null)
            return NotFound();
        return Ok(WalkForwardReportBuilder.Build(run));
    }

    [HttpGet("{id:long}/report.csv")]
    public async Task<IActionResult> ReportCsv(long id, CancellationToken ct)
    {
        var run = await _db.GetWalkForwardRunWithWindowsAsync(id, ct);
        if (run is null)
            return NotFound();
        var csv = WalkForwardCsvExporter.ToCsv(WalkForwardReportBuilder.Build(run));
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
            $"walkforward-{id}-report.csv");
    }
}
