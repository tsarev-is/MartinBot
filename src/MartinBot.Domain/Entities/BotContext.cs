using MartinBot.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace MartinBot.Domain.Entities;

/// <summary>
/// SQLite persistence root — only the state the bot can't reconstruct from EXMO on restart.
/// </summary>
public sealed class BotContext : DbContext
{
    private DbSet<OrderEntity> Orders => Set<OrderEntity>();
    private DbSet<ApiStateEntity> ApiState => Set<ApiStateEntity>();
    private DbSet<BacktestRunEntity> BacktestRuns => Set<BacktestRunEntity>();
    private DbSet<ParameterSweepRunEntity> ParameterSweepRuns => Set<ParameterSweepRunEntity>();
    private DbSet<WalkForwardRunEntity> WalkForwardRuns => Set<WalkForwardRunEntity>();
    private DbSet<WalkForwardWindowEntity> WalkForwardWindows => Set<WalkForwardWindowEntity>();

    public BotContext(DbContextOptions<BotContext> options) : base(options)
    { }

    public void AddOrder(OrderEntity order) => Orders.Add(order);

    public Task<List<OrderEntity>> ListOrdersAsync(CancellationToken ct = default)
        => Orders.AsNoTracking().ToListAsync(ct);

    public void AddApiState(ApiStateEntity state) => ApiState.Add(state);

    public Task<ApiStateEntity?> FindApiStateAsync(string key, CancellationToken ct = default)
        => ApiState.AsNoTracking().SingleOrDefaultAsync(s => s.Key == key, ct);

    public void AddBacktestRun(BacktestRunEntity run) => BacktestRuns.Add(run);

    public Task<BacktestRunEntity?> FindBacktestRunAsync(long id, CancellationToken ct = default)
        => BacktestRuns.SingleOrDefaultAsync(p => p.Id == id, cancellationToken: ct);

    public Task<BacktestRunEntity?> GetBacktestRunAsync(long id, CancellationToken ct = default)
        => BacktestRuns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<BacktestRunEntity>> ListRecentBacktestRunsAsync(int limit, CancellationToken ct = default)
        => BacktestRuns.AsNoTracking().OrderByDescending(r => r.Id).Take(limit).ToListAsync(ct);

    public void AddParameterSweepRun(ParameterSweepRunEntity run) => ParameterSweepRuns.Add(run);

    public Task<ParameterSweepRunEntity?> FindParameterSweepRunAsync(long id, CancellationToken ct = default)
        => ParameterSweepRuns.SingleOrDefaultAsync(r => r.Id == id, cancellationToken: ct);

    public Task<ParameterSweepRunEntity?> GetParameterSweepRunAsync(long id, CancellationToken ct = default)
        => ParameterSweepRuns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<ParameterSweepRunEntity>> ListRecentParameterSweepRunsAsync(int limit,
        CancellationToken ct = default)
        => ParameterSweepRuns.AsNoTracking().OrderByDescending(r => r.Id).Take(limit).ToListAsync(ct);

    public void AddWalkForwardRun(WalkForwardRunEntity run) => WalkForwardRuns.Add(run);

    public void AddWalkForwardWindow(WalkForwardWindowEntity window) => WalkForwardWindows.Add(window);

    public Task<WalkForwardRunEntity?> FindWalkForwardRunAsync(long id, CancellationToken ct = default)
        => WalkForwardRuns.SingleOrDefaultAsync(r => r.Id == id, cancellationToken: ct);

    public Task<WalkForwardRunEntity?> GetWalkForwardRunAsync(long id, CancellationToken ct = default)
        => WalkForwardRuns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);

    public Task<WalkForwardRunEntity?> GetWalkForwardRunWithWindowsAsync(long id, CancellationToken ct = default)
        => WalkForwardRuns.AsNoTracking()
            .Include(r => r.Windows.OrderBy(w => w.WindowIndex))
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<WalkForwardRunEntity>> ListRecentWalkForwardRunsAsync(int limit,
        CancellationToken ct = default)
        => WalkForwardRuns.AsNoTracking().OrderByDescending(r => r.Id).Take(limit).ToListAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelConfiguration.ConfigureOrder(modelBuilder);
        ModelConfiguration.ConfigureApiState(modelBuilder);
        ModelConfiguration.ConfigureBacktestRun(modelBuilder);
        ModelConfiguration.ConfigureParameterSweepRun(modelBuilder);
        ModelConfiguration.ConfigureWalkForwardRun(modelBuilder);
        ModelConfiguration.ConfigureWalkForwardWindow(modelBuilder);
    }
}
