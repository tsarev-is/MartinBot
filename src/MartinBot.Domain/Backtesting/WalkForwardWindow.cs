namespace MartinBot.Domain.Backtesting;

/// <summary>
/// A single train/test split in a walk-forward validation run. The test window immediately
/// follows the train window; <see cref="TrainTo"/> equals <see cref="TestFrom"/>.
/// </summary>
public sealed class WalkForwardWindow
{
    public WalkForwardWindow(int index, DateTimeOffset trainFrom, DateTimeOffset trainTo,
        DateTimeOffset testFrom, DateTimeOffset testTo)
    {
        Index = index;
        TrainFrom = trainFrom;
        TrainTo = trainTo;
        TestFrom = testFrom;
        TestTo = testTo;
    }

    public int Index { get; }

    public DateTimeOffset TrainFrom { get; }

    public DateTimeOffset TrainTo { get; }

    public DateTimeOffset TestFrom { get; }

    public DateTimeOffset TestTo { get; }
}
