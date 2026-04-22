using MartinBot.Backtesting;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Entities;
using MartinBot.Domain.Entities.Models;
using MartinBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace MartinBot.Private;

[ApiController]
[Route("private/sweep")]
public sealed class ParameterSweepController : ControllerBase
{
    private const int MaxCombinations = 1000;

    private readonly BotContext _db;
    private readonly ParameterSweepQueue _queue;
    private readonly BacktestStrategyFactory _factory;

    public ParameterSweepController(BotContext db, ParameterSweepQueue queue, BacktestStrategyFactory factory)
    {
        _db = db;
        _queue = queue;
        _factory = factory;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Enqueue([FromBody] ParameterSweepRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Pair) || string.IsNullOrWhiteSpace(body.Timeframe))
            return BadRequest(new { error = "pair and timeframe are required" });
        if (body.From >= body.To)
            return BadRequest(new { error = "from must be earlier than to" });
        if (body.InitialCash <= 0m)
            return BadRequest(new { error = "initialCash must be positive" });

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

        var total = ParameterGrid.CountCombinations(body.ParameterGrid);
        if (total == 0)
            return BadRequest(new { error = "parameterGrid yields zero combinations" });
        if (total > MaxCombinations)
            return BadRequest(new { error = $"parameterGrid yields {total} combinations, exceeds limit of {MaxCombinations}" });

        try
        {
            var proxy = body.ParameterGrid?.ToDictionary(kv => kv.Key, _ => 0m);
            _factory.ValidateParameters(strategy, proxy);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var gridJson = ParameterGridSerializer.Serialize(body.ParameterGrid);
        var now = DateTimeOffset.UtcNow;
        var sweep = new ParameterSweepRunEntity(id: 0, pair: body.Pair, timeframe: body.Timeframe,
            from: body.From, to: body.To, initialCash: body.InitialCash,
            feeBps: body.FeeBps, slippageBps: body.SlippageBps, strategyName: strategy,
            parameterGridJson: gridJson, optimizationMetric: metric,
            status: ParameterSweepRunStatus.Queued,
            totalCombinations: null, completedCombinations: null,
            bestParametersJson: null, bestMetricValue: null,
            bestTotalReturn: null, bestMaxDrawdown: null, bestSharpe: null,
            bestTradeCount: null, bestWinRate: null, bestDroppedIntents: null,
            errorMessage: null, startedAt: null, completedAt: null,
            createdAt: now, updatedAt: now);

        _db.AddParameterSweepRun(sweep);
        await _db.SaveChangesAsync(ct);
        await _queue.EnqueueAsync(sweep.Id, ct);

        return Accepted(new { sweepId = sweep.Id });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var sweep = await _db.GetParameterSweepRunAsync(id, ct);
        if (sweep is null)
            return NotFound();
        return Ok(sweep);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var sweeps = await _db.ListRecentParameterSweepRunsAsync(50, ct);
        return Ok(sweeps);
    }
}
