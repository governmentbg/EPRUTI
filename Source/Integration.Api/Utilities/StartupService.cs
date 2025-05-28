namespace Integration.Api.Utilities
{
    using System.Threading;
    using System.Threading.Tasks;

    using Ais.Common.Logger;
    using Ais.Utilities.Helpers;

    using Microsoft.Extensions.Localization;

    public class StartupService : IHostedService
    {
        private readonly IServiceProvider serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using (var scope = this.serviceProvider.CreateAsyncScope())
            {
                LogManager.Logger = scope.ServiceProvider.GetService<ILogger<LogManager>>();
                StringLocalizer.Instance = scope.ServiceProvider.GetRequiredService<IStringLocalizer>();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
