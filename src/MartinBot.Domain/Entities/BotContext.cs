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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelConfiguration.ConfigureOrder(modelBuilder);
        ModelConfiguration.ConfigureApiState(modelBuilder);
        ModelConfiguration.ConfigureBacktestRun(modelBuilder);
    }
}
