using MartinBot.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MartinBot.Private;

[ApiController]
[Route("private/debug")]
public sealed class DebugController : ControllerBase
{
    private readonly IExmoService _exmo;

    public DebugController(IExmoService exmo)
    {
        _exmo = exmo;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var balances = await _exmo.GetBalancesAsync(ct);
        return Ok(balances);
    }

    [HttpGet("ticker/{pair}")]
    public async Task<IActionResult> GetTicker(string pair, CancellationToken ct)
    {
        var ticker = await _exmo.GetTickerAsync(pair, ct);
        return Ok(ticker);
    }

    [HttpGet("open-orders")]
    public async Task<IActionResult> GetOpenOrders(CancellationToken ct)
    {
        var orders = await _exmo.GetOpenOrdersAsync(ct);
        return Ok(orders);
    }
}
