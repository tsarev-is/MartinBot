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
