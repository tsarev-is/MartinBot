using MartinBot.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace MartinBot.Domain.Entities;

public static class ModelConfiguration
{
    public static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<OrderEntity>();
        order.HasKey(o => o.Id);
        order.Property(o => o.Id).ValueGeneratedOnAdd();
        order.Property(o => o.ClientId).IsRequired();
        order.Property(o => o.Pair).IsRequired();
        order.HasIndex(o => o.ClientId).IsUnique();
        order.HasIndex(o => o.ExmoOrderId).IsUnique();
    }

    public static void ConfigureApiState(ModelBuilder modelBuilder)
    {
        var apiState = modelBuilder.Entity<ApiStateEntity>();
        apiState.HasKey(s => s.Key);
        apiState.Property(s => s.Key).IsRequired();
    }
}
