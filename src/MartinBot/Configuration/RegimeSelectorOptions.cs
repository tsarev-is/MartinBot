namespace MartinBot.Configuration;

public sealed class RegimeSelectorOptions
{
    public bool Enabled { get; set; } = false;

    public int EmaShortPeriod { get; set; } = 50;

    public int EmaLongPeriod { get; set; } = 200;

    public int AdxPeriod { get; set; } = 14;

    public decimal AdxThreshold { get; set; } = 25m;
}
