using System.Threading.Channels;

namespace MartinBot.Backtesting;

/// <summary>
/// Unbounded single-reader channel carrying parameter-sweep run ids between the HTTP controller
/// and the <see cref="ParameterSweepRunnerService"/> background worker.
/// </summary>
public sealed class ParameterSweepQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(long sweepId, CancellationToken ct) => _channel.Writer.WriteAsync(sweepId, ct);

    public IAsyncEnumerable<long> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
