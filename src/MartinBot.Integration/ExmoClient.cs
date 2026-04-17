using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MartinBot.Domain;
using MartinBot.Domain.Models;
using MartinBot.Integration.Configuration;
using MartinBot.Integration.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinBot.Integration;

public sealed class ExmoClient : IExmoService
{
    private readonly HttpClient _http;
    private readonly ExmoOptions _options;
    private readonly ILogger<ExmoClient> _logger;
    private long _nonce;

    public ExmoClient(HttpClient http, IOptions<ExmoOptions> options, ILogger<ExmoClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public async Task<Ticker> GetTickerAsync(string pair, CancellationToken ct = default)
    {
        _logger.LogDebug($"EXMO ticker request for {pair}");
        using var doc = await GetPublicAsync("ticker", ct).ConfigureAwait(false);
        var root = doc.RootElement;
        if (!root.TryGetProperty(pair, out var node))
            throw new ExmoApiException($"Unknown pair: {pair}");

        return new Ticker(
            pair: pair,
            bid: ParseDecimal(node, "buy_price"),
            ask: ParseDecimal(node, "sell_price"),
            last: ParseDecimal(node, "last_trade"),
            updatedAt: DateTimeOffset.FromUnixTimeSeconds(node.GetProperty("updated").GetInt64()));
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetBalancesAsync(CancellationToken ct = default)
    {
        using var doc = await PostPrivateAsync("user_info", new Dictionary<string, string>(), ct).ConfigureAwait(false);
        var balances = doc.RootElement.GetProperty("balances");
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in balances.EnumerateObject())
            result[prop.Name] = decimal.Parse(prop.Value.GetString()!, CultureInfo.InvariantCulture);
        return result;
    }

    public async Task<CreatedOrder> CreateLimitOrderAsync(string pair, OrderSide side, decimal quantity, decimal price,
        CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["pair"] = pair,
            ["quantity"] = quantity.ToString(CultureInfo.InvariantCulture),
            ["price"] = price.ToString(CultureInfo.InvariantCulture),
            ["type"] = side == OrderSide.Buy ? "buy" : "sell"
        };

        using var doc = await PostPrivateAsync("order_create", parameters, ct).ConfigureAwait(false);
        var orderId = doc.RootElement.GetProperty("order_id").GetInt64();
        _logger.LogInformation($"EXMO limit {side} order created: {orderId} {pair} {quantity.ToString(CultureInfo.InvariantCulture)}@{price.ToString(CultureInfo.InvariantCulture)}");
        return new CreatedOrder(orderId);
    }

    public async Task CancelOrderAsync(long orderId, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["order_id"] = orderId.ToString(CultureInfo.InvariantCulture)
        };
        using var _ = await PostPrivateAsync("order_cancel", parameters, ct).ConfigureAwait(false);
        _logger.LogInformation($"EXMO order cancelled: {orderId}");
    }

    public async Task<IReadOnlyList<OpenOrder>> GetOpenOrdersAsync(CancellationToken ct = default)
    {
        using var doc = await PostPrivateAsync("user_open_orders", new Dictionary<string, string>(), ct).ConfigureAwait(false);
        var result = new List<OpenOrder>();
        foreach (var pairNode in doc.RootElement.EnumerateObject())
        {
            foreach (var order in pairNode.Value.EnumerateArray())
            {
                var type = order.GetProperty("type").GetString();
                var side = string.Equals(type, "buy", StringComparison.OrdinalIgnoreCase)
                    ? OrderSide.Buy
                    : OrderSide.Sell;

                var quantity = ParseDecimal(order, "quantity");
                var remaining = order.TryGetProperty("amount", out var amountNode) && amountNode.ValueKind != JsonValueKind.Null
                    ? decimal.Parse(amountNode.GetString()!, CultureInfo.InvariantCulture)
                    : quantity;

                result.Add(new OpenOrder(
                    orderId: long.Parse(order.GetProperty("order_id").GetString()!, CultureInfo.InvariantCulture),
                    pair: pairNode.Name,
                    side: side,
                    price: ParseDecimal(order, "price"),
                    quantity: quantity,
                    remainingQuantity: remaining,
                    createdAt: DateTimeOffset.FromUnixTimeSeconds(
                        long.Parse(order.GetProperty("created").GetString()!, CultureInfo.InvariantCulture))));
            }
        }
        return result;
    }

    private async Task<JsonDocument> GetPublicAsync(string method, CancellationToken ct)
    {
        using var response = await _http.GetAsync(method, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
    }

    private async Task<JsonDocument> PostPrivateAsync(string method, IDictionary<string, string> parameters,
        CancellationToken ct)
    {
        parameters["nonce"] = Interlocked.Increment(ref _nonce).ToString(CultureInfo.InvariantCulture);

        var body = string.Join("&", parameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var signature = Sign(body, _options.Secret);

        using var request = new HttpRequestMessage(HttpMethod.Post, method)
        {
            Content = new StringContent(body, Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        request.Headers.Add("Key", _options.ApiKey);
        request.Headers.Add("Sign", signature);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.False)
        {
            var error = root.TryGetProperty("error", out var e) ? e.GetString() : null;
            doc.Dispose();
            _logger.LogWarning($"EXMO private call {method} returned error: {error}");
            throw new ExmoApiException(error ?? "Unknown EXMO error");
        }

        return doc;
    }

    private static string Sign(string body, string secret)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static decimal ParseDecimal(JsonElement node, string property)
    {
        var value = node.GetProperty(property);
        return value.ValueKind switch
        {
            JsonValueKind.String => decimal.Parse(value.GetString()!, CultureInfo.InvariantCulture),
            JsonValueKind.Number => value.GetDecimal(),
            _ => throw new ExmoApiException($"Unexpected kind for {property}: {value.ValueKind}")
        };
    }
}
