using MartinBot;
using NLog.Web;

public class Program
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static void Main(string[] args)
    {
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Stopped program because of exception.");
            throw;
        }
        finally
        {
            NLog.LogManager.Shutdown();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseNLog()
                    .UseStartup<Startup>();
            });
    }
}
