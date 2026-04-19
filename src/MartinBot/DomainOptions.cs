using MartinBot.Configuration;
using MartinBot.Domain;
using MartinBot.Domain.Entities;
using MartinBot.Integration;
using MartinBot.Integration.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MartinBot;

public static class DomainOptions
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ExmoSettings>(config.GetSection("Exmo"));
        services.AddSingleton<IConfigureOptions<ExmoOptions>, ConfigureExmoOptions>();

        services.AddDbContext<BotContext>(o => o.UseSqlite(config.GetConnectionString("Bot")));

        services.AddHttpClient<IExmoService, ExmoClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<ExmoOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }
}