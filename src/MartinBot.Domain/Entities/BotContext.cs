using MartinBot.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace MartinBot.Domain.Entities;

/// <summary>
/// SQLite persistence root — only the state the bot can't reconstruct from EXMO on restart.
/// </summary>
public sealed class BotContext : DbContext
{
    public BotContext(DbContextOptions<BotContext> options) : base(options)
    { }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<ApiStateEntity> ApiState => Set<ApiStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelConfiguration.ConfigureOrder(modelBuilder);
        ModelConfiguration.ConfigureApiState(modelBuilder);
    }
}
