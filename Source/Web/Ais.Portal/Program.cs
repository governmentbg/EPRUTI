using Serilog;

/// <summary>
/// Class Program.
/// </summary>
public class Program
{
    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .CreateLogger();
        CreateHostBuilder(args)
            .UseSerilog(
                (context, serviceProvider, config) =>
                {
                    config.ReadFrom.Configuration(context.Configuration);
                    config.ReadFrom.Services(serviceProvider);
                })
            .Build().Run();
    }

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>IHostBuilder.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                });
}
