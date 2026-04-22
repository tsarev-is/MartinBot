namespace MartinBot.Domain.Backtesting;

/// <summary>
/// Generates a sequence of train/test windows for walk-forward validation.
/// For a range <c>[from, to]</c>, window <c>i</c> has:
/// <c>trainFrom_i = from + i*step</c>, <c>trainTo_i = trainFrom_i + trainDuration</c>,
/// <c>testFrom_i = trainTo_i</c>, <c>testTo_i = testFrom_i + testDuration</c>.
/// Yielding stops as soon as <c>testTo_i > to</c>.
/// </summary>
public static class WalkForwardWindowGenerator
{
    public static IEnumerable<WalkForwardWindow> Generate(DateTimeOffset from, DateTimeOffset to,
        TimeSpan trainDuration, TimeSpan testDuration, TimeSpan stepDuration)
    {
        if (from >= to)
            throw new ArgumentException("from must be earlier than to", nameof(from));
        if (trainDuration <= TimeSpan.Zero)
            throw new ArgumentException("trainDuration must be positive", nameof(trainDuration));
        if (testDuration <= TimeSpan.Zero)
            throw new ArgumentException("testDuration must be positive", nameof(testDuration));
        if (stepDuration <= TimeSpan.Zero)
            throw new ArgumentException("stepDuration must be positive", nameof(stepDuration));

        var index = 0;
        var trainFrom = from;
        while (true)
        {
            var trainTo = trainFrom + trainDuration;
            var testFrom = trainTo;
            var testTo = testFrom + testDuration;
            if (testTo > to)
                yield break;
            yield return new WalkForwardWindow(index, trainFrom, trainTo, testFrom, testTo);
            index++;
            trainFrom += stepDuration;
        }
    }
}
