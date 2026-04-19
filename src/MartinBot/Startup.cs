using System.Text.Json.Serialization;
using MartinBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MartinBot;

public sealed class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDomain(_configuration);
        services
            .AddControllers()
            .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddHealthChecks();
        services.AddSwagger();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        using (var scope = app.ApplicationServices.CreateScope())
            scope.ServiceProvider.GetRequiredService<BotContext>().Database.Migrate();

        if (env.IsDevelopment())
        {
            app.UseSwaggerAtRoot();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapControllers();
        });
    }
}
