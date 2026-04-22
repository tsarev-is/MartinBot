using MartinBot.Domain.Backtesting;
using MartinBot.Domain.Entities;
using MartinBot.Domain.Entities.Models;
using MartinBot.Domain.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MartinBot.Tests;

public sealed class BotContextTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<BotContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<BotContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new BotContext(_options);
        ctx.Database.Migrate();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Order_RoundTrip_PreservesAllFields()
    {
        var createdAt = new DateTimeOffset(2026, 4, 19, 1, 0, 0, TimeSpan.Zero);
        var order = new OrderEntity(id: 0, clientId: "c-1", exmoOrderId: 42, pair: "BTC_USD",
            side: OrderSide.Buy, price: 50000m, quantity: 0.01m, filledQuantity: 0m,
            status: OrderStatus.Open, createdAt: createdAt, updatedAt: createdAt);

        await using (var ctx = new BotContext(_options))
        {
            ctx.AddOrder(order);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var loaded = (await ctx.ListOrdersAsync()).Single();
            Assert.That(loaded.ClientId, Is.EqualTo("c-1"));
            Assert.That(loaded.ExmoOrderId, Is.EqualTo(42L));
            Assert.That(loaded.Pair, Is.EqualTo("BTC_USD"));
            Assert.That(loaded.Side, Is.EqualTo(OrderSide.Buy));
            Assert.That(loaded.Price, Is.EqualTo(50000m));
            Assert.That(loaded.Quantity, Is.EqualTo(0.01m));
            Assert.That(loaded.FilledQuantity, Is.EqualTo(0m));
            Assert.That(loaded.Status, Is.EqualTo(OrderStatus.Open));
            Assert.That(loaded.CreatedAt, Is.EqualTo(createdAt));
        }
    }

    [Test]
    public void Order_DuplicateClientId_ThrowsOnSave()
    {
        var now = DateTimeOffset.UtcNow;

        using var ctx = new BotContext(_options);
        ctx.AddOrder(new OrderEntity(0, "dup", null, "BTC_USD", OrderSide.Buy, 1m, 1m, 0m,
            OrderStatus.Pending, now, now));
        ctx.AddOrder(new OrderEntity(0, "dup", null, "ETH_USD", OrderSide.Sell, 2m, 1m, 0m,
            OrderStatus.Pending, now, now));

        Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Test]
    public async Task BacktestRun_RoundTrip_PreservesStrategyParametersJson()
    {
        var now = new DateTimeOffset(2026, 4, 21, 1, 0, 0, TimeSpan.Zero);
        const string paramsJson = "{\"emaPeriod\":50,\"entryRsi\":25}";

        await using (var ctx = new BotContext(_options))
        {
            ctx.AddBacktestRun(new BacktestRunEntity(
                id: 0, pair: "BTC_USD", timeframe: "h1", from: now, to: now.AddHours(1),
                initialCash: 1_000m, feeBps: 0m, slippageBps: 0m, strategyName: "dca_mr",
                strategyParametersJson: paramsJson, status: BacktestRunStatus.Queued,
                finalEquity: null, totalReturn: null, maxDrawdown: null, sharpe: null,
                tradeCount: null, winRate: null, errorMessage: null,
                createdAt: now, updatedAt: now));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var run = await ctx.ListRecentBacktestRunsAsync(1);
            Assert.That(run, Has.Count.EqualTo(1));
            Assert.That(run[0].StrategyParametersJson, Is.EqualTo(paramsJson));
        }
    }

    [Test]
    public async Task BacktestRun_RoundTrip_NullStrategyParametersJson()
    {
        var now = new DateTimeOffset(2026, 4, 21, 1, 0, 0, TimeSpan.Zero);

        await using (var ctx = new BotContext(_options))
        {
            ctx.AddBacktestRun(new BacktestRunEntity(
                id: 0, pair: "BTC_USD", timeframe: "h1", from: now, to: now.AddHours(1),
                initialCash: 1_000m, feeBps: 0m, slippageBps: 0m, strategyName: "buy_and_hold",
                strategyParametersJson: null, status: BacktestRunStatus.Queued,
                finalEquity: null, totalReturn: null, maxDrawdown: null, sharpe: null,
                tradeCount: null, winRate: null, errorMessage: null,
                createdAt: now, updatedAt: now));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var run = await ctx.ListRecentBacktestRunsAsync(1);
            Assert.That(run[0].StrategyParametersJson, Is.Null);
        }
    }

    [Test]
    public async Task ParameterSweepRun_RoundTrip_PreservesAllFields()
    {
        var now = new DateTimeOffset(2026, 4, 22, 1, 0, 0, TimeSpan.Zero);
        const string gridJson = "{\"emaPeriod\":[100,200],\"entryRsi\":[25,30]}";
        const string bestJson = "{\"emaPeriod\":200,\"entryRsi\":25}";

        await using (var ctx = new BotContext(_options))
        {
            var sweep = new ParameterSweepRunEntity(
                id: 0, pair: "BTC_USD", timeframe: "60", from: now, to: now.AddDays(30),
                initialCash: 1_000m, feeBps: 30m, slippageBps: 20m, strategyName: "dca_mr",
                parameterGridJson: gridJson, optimizationMetric: OptimizationMetric.Sharpe,
                status: ParameterSweepRunStatus.Succeeded,
                totalCombinations: 4, completedCombinations: 4,
                bestParametersJson: bestJson, bestMetricValue: 1.37m,
                bestTotalReturn: 0.22m, bestMaxDrawdown: 0.08m, bestSharpe: 1.37m,
                bestTradeCount: 12, bestWinRate: 0.6m, bestDroppedIntents: 0,
                errorMessage: null, startedAt: now, completedAt: now.AddMinutes(2),
                createdAt: now, updatedAt: now.AddMinutes(2));
            ctx.AddParameterSweepRun(sweep);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var sweeps = await ctx.ListRecentParameterSweepRunsAsync(1);
            Assert.That(sweeps, Has.Count.EqualTo(1));
            var s = sweeps[0];
            Assert.That(s.Pair, Is.EqualTo("BTC_USD"));
            Assert.That(s.ParameterGridJson, Is.EqualTo(gridJson));
            Assert.That(s.OptimizationMetric, Is.EqualTo(OptimizationMetric.Sharpe));
            Assert.That(s.Status, Is.EqualTo(ParameterSweepRunStatus.Succeeded));
            Assert.That(s.TotalCombinations, Is.EqualTo(4));
            Assert.That(s.CompletedCombinations, Is.EqualTo(4));
            Assert.That(s.BestParametersJson, Is.EqualTo(bestJson));
            Assert.That(s.BestMetricValue, Is.EqualTo(1.37m));
            Assert.That(s.BestTotalReturn, Is.EqualTo(0.22m));
            Assert.That(s.BestMaxDrawdown, Is.EqualTo(0.08m));
            Assert.That(s.BestSharpe, Is.EqualTo(1.37m));
            Assert.That(s.BestTradeCount, Is.EqualTo(12));
            Assert.That(s.BestWinRate, Is.EqualTo(0.6m));
            Assert.That(s.BestDroppedIntents, Is.EqualTo(0));
            Assert.That(s.StartedAt, Is.EqualTo(now));
            Assert.That(s.CompletedAt, Is.EqualTo(now.AddMinutes(2)));
        }
    }

    [Test]
    public async Task ParameterSweepRun_RoundTrip_NullableBestSummary()
    {
        var now = new DateTimeOffset(2026, 4, 22, 1, 0, 0, TimeSpan.Zero);

        await using (var ctx = new BotContext(_options))
        {
            var sweep = new ParameterSweepRunEntity(
                id: 0, pair: "BTC_USD", timeframe: "60", from: now, to: now.AddDays(1),
                initialCash: 1_000m, feeBps: 0m, slippageBps: 0m, strategyName: "buy_and_hold",
                parameterGridJson: null, optimizationMetric: OptimizationMetric.TotalReturn,
                status: ParameterSweepRunStatus.Queued,
                totalCombinations: null, completedCombinations: null,
                bestParametersJson: null, bestMetricValue: null,
                bestTotalReturn: null, bestMaxDrawdown: null, bestSharpe: null,
                bestTradeCount: null, bestWinRate: null, bestDroppedIntents: null,
                errorMessage: null, startedAt: null, completedAt: null,
                createdAt: now, updatedAt: now);
            ctx.AddParameterSweepRun(sweep);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var s = (await ctx.ListRecentParameterSweepRunsAsync(1))[0];
            Assert.That(s.Status, Is.EqualTo(ParameterSweepRunStatus.Queued));
            Assert.That(s.BestParametersJson, Is.Null);
            Assert.That(s.StartedAt, Is.Null);
            Assert.That(s.CompletedAt, Is.Null);
        }
    }

    [Test]
    public async Task WalkForwardRun_RoundTrip_NullableAggregatesAndNoWindows()
    {
        var now = new DateTimeOffset(2026, 4, 23, 1, 0, 0, TimeSpan.Zero);

        await using (var ctx = new BotContext(_options))
        {
            var run = new WalkForwardRunEntity(
                id: 0, pair: "BTC_USD", timeframe: "60", from: now, to: now.AddDays(100),
                initialCash: 1_000m, feeBps: 30m, slippageBps: 20m, strategyName: "dca_mr",
                parameterGridJson: null, optimizationMetric: OptimizationMetric.Sharpe,
                trainDays: 30, testDays: 10, stepDays: 10,
                status: WalkForwardRunStatus.Queued,
                totalWindows: null, completedWindows: null,
                aggregateTotalReturn: null, aggregateMaxDrawdown: null, aggregateSharpe: null,
                errorMessage: null, startedAt: null, completedAt: null,
                createdAt: now, updatedAt: now);
            ctx.AddWalkForwardRun(run);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var r = (await ctx.ListRecentWalkForwardRunsAsync(1))[0];
            Assert.That(r.Status, Is.EqualTo(WalkForwardRunStatus.Queued));
            Assert.That(r.TrainDays, Is.EqualTo(30));
            Assert.That(r.TestDays, Is.EqualTo(10));
            Assert.That(r.StepDays, Is.EqualTo(10));
            Assert.That(r.AggregateSharpe, Is.Null);
            Assert.That(r.CompletedWindows, Is.Null);
        }
    }

    [Test]
    public async Task WalkForwardRun_WithWindows_EagerLoadReturnsChildren()
    {
        var now = new DateTimeOffset(2026, 4, 23, 1, 0, 0, TimeSpan.Zero);
        long runId;

        await using (var ctx = new BotContext(_options))
        {
            var run = new WalkForwardRunEntity(
                id: 0, pair: "BTC_USD", timeframe: "60", from: now, to: now.AddDays(90),
                initialCash: 1_000m, feeBps: 0m, slippageBps: 0m, strategyName: "dca_mr",
                parameterGridJson: "{\"emaPeriod\":[100,200]}",
                optimizationMetric: OptimizationMetric.TotalReturn,
                trainDays: 30, testDays: 10, stepDays: 10,
                status: WalkForwardRunStatus.Succeeded,
                totalWindows: 2, completedWindows: 2,
                aggregateTotalReturn: 0.05m, aggregateMaxDrawdown: 0.02m, aggregateSharpe: 0.7m,
                errorMessage: null, startedAt: now, completedAt: now.AddMinutes(5),
                createdAt: now, updatedAt: now.AddMinutes(5));
            ctx.AddWalkForwardRun(run);
            await ctx.SaveChangesAsync();
            runId = run.Id;

            ctx.AddWalkForwardWindow(new WalkForwardWindowEntity(
                id: 0, runId: runId, windowIndex: 0,
                trainFrom: now, trainTo: now.AddDays(30),
                testFrom: now.AddDays(30), testTo: now.AddDays(40),
                bestParametersJson: "{\"emaPeriod\":100}", inSampleMetricValue: 0.05m,
                outOfSampleTotalReturn: 0.02m, outOfSampleMaxDrawdown: 0.01m,
                outOfSampleSharpe: 0.5m, outOfSampleTradeCount: 3, createdAt: now));
            ctx.AddWalkForwardWindow(new WalkForwardWindowEntity(
                id: 0, runId: runId, windowIndex: 1,
                trainFrom: now.AddDays(10), trainTo: now.AddDays(40),
                testFrom: now.AddDays(40), testTo: now.AddDays(50),
                bestParametersJson: "{\"emaPeriod\":200}", inSampleMetricValue: 0.08m,
                outOfSampleTotalReturn: 0.03m, outOfSampleMaxDrawdown: 0.015m,
                outOfSampleSharpe: 0.9m, outOfSampleTradeCount: 4, createdAt: now));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var loaded = await ctx.GetWalkForwardRunWithWindowsAsync(runId);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Windows, Has.Count.EqualTo(2));
            Assert.That(loaded.Windows[0].WindowIndex, Is.EqualTo(0));
            Assert.That(loaded.Windows[0].BestParametersJson, Is.EqualTo("{\"emaPeriod\":100}"));
            Assert.That(loaded.Windows[1].OutOfSampleSharpe, Is.EqualTo(0.9m));
            Assert.That(loaded.AggregateTotalReturn, Is.EqualTo(0.05m));
        }
    }

    [Test]
    public async Task ApiState_RoundTrip_PreservesValue()
    {
        var now = new DateTimeOffset(2026, 4, 19, 1, 0, 0, TimeSpan.Zero);

        await using (var ctx = new BotContext(_options))
        {
            ctx.AddApiState(new ApiStateEntity("exmo.nonce", 1738000000123L, now));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new BotContext(_options))
        {
            var entry = await ctx.FindApiStateAsync("exmo.nonce");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Value, Is.EqualTo(1738000000123L));
            Assert.That(entry.UpdatedAt, Is.EqualTo(now));
        }
    }
}
