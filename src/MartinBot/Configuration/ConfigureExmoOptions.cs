using MartinBot.Integration.Configuration;
using Microsoft.Extensions.Options;

namespace MartinBot.Configuration;

internal sealed class ConfigureExmoOptions : IConfigureOptions<ExmoOptions>
{
    private readonly ExmoSettings _settings;

    public ConfigureExmoOptions(IOptions<ExmoSettings> settings)
    {
        _settings = settings.Value;
    }

    public void Configure(ExmoOptions options)
    {
        options.BaseUrl = _settings.BaseUrl;
        options.ApiKey = _settings.ApiKey;
        options.Secret = _settings.Secret;
    }
}
