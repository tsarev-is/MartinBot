using System.Threading.Channels;

namespace MartinBot.Backtesting;

/// <summary>
/// Unbounded single-reader channel carrying walk-forward run ids between the HTTP controller
/// and the <see cref="WalkForwardRunnerService"/> background worker.
/// </summary>
public sealed class WalkForwardQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(long runId, CancellationToken ct) => _channel.Writer.WriteAsync(runId, ct);

    public IAsyncEnumerable<long> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
