namespace MartinBot.Configuration;

public sealed class ExmoSettings
{
    public string BaseUrl { get; set; } = "https://api.exmo.com/v1.1/";

    public string ApiKey { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}
