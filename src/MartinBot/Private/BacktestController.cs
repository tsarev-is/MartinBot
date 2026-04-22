using MartinBot.Backtesting;
using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Entities;
using MartinBot.Domain.Entities.Models;
using MartinBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace MartinBot.Private;

[ApiController]
[Route("private/backtest")]
public sealed class BacktestController : ControllerBase
{
    private readonly BotContext _db;
    private readonly BacktestQueue _queue;
    private readonly BacktestStrategyFactory _factory;

    public BacktestController(BotContext db, BacktestQueue queue, BacktestStrategyFactory factory)
    {
        _db = db;
        _queue = queue;
        _factory = factory;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Enqueue([FromBody] BacktestRunRequest body, CancellationToken ct)
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

        try
        {
            _factory.ValidateParameters(strategy, body.StrategyParameters);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var parametersJson = StrategyParametersSerializer.Serialize(body.StrategyParameters);
        var now = DateTimeOffset.UtcNow;
        var run = new BacktestRunEntity(id: 0, pair: body.Pair, timeframe: body.Timeframe,
            from: body.From, to: body.To, initialCash: body.InitialCash,
            feeBps: body.FeeBps, slippageBps: body.SlippageBps, strategyName: strategy,
            strategyParametersJson: parametersJson, status: BacktestRunStatus.Queued,
            finalEquity: null, totalReturn: null, maxDrawdown: null,
            sharpe: null, tradeCount: null, winRate: null, errorMessage: null,
            createdAt: now, updatedAt: now);

        _db.AddBacktestRun(run);
        await _db.SaveChangesAsync(ct);
        await _queue.EnqueueAsync(run.Id, ct);

        return Accepted(new { runId = run.Id });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var run = await _db.GetBacktestRunAsync(id, ct);
        if (run is null)
            return NotFound();
        return Ok(run);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var runs = await _db.ListRecentBacktestRunsAsync(50, ct);
        return Ok(runs);
    }
}
