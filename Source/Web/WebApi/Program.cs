using Serilog;

using WebApi;

public static class Program
{
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

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
}
