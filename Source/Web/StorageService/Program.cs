namespace StorageService
{
    using Serilog;

    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .CreateLogger();
            CreateHostBuilder(args)
                .UseSerilog((context, config) =>
                                config.ReadFrom.Configuration(context.Configuration))
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
}
