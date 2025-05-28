namespace StorageService
{
    using Ais.Common.Logger;
    using Ais.Utilities.Helpers;

    using Calzolari.Grpc.AspNetCore.Validation;

    using Microsoft.AspNetCore.HttpOverrides;

    using StorageService.Services;
    using StorageService.Utilities.Extensions;

    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddGrpc(
                options =>
                {
                    options.EnableMessageValidation();
                    options.MaxReceiveMessageSize = null;
                    options.MaxSendMessageSize = null;
                });
            services.AddGrpcReflection();
            services.AddValidation();
            services.AddCustomOptions(this.configuration);

            services.AddDataContext(this.configuration);
            services.AddDataRepositories();
            services.AddApplicationServices();

            services.AddJwtAuthorization(this.configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CurrentConfiguration.Instance = this.configuration;

            // Breaking changes Npgsql 7.0 CommandType.StoredProcedure now invokes procedures instead of functions https://www.npgsql.org/doc/release-notes/7.0.html#commandtypestoredprocedure-now-invokes-procedures-instead-of-functions
            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

            app.UseRouting();

            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                    endpoints.MapGet("health", () => "Healthy");
                    endpoints.MapGrpcService<AuthenticationService>();
                    endpoints.MapGrpcService<StorageService>();

                    if (env.IsDevelopment())
                    {
                        endpoints.MapGrpcReflectionService();
                    }
                });

            // TODO - must be in IHostedService
            AsyncHelper.RunSync(
                async () =>
                {
                    await using var scope = app.ApplicationServices.CreateAsyncScope();
                    LogManager.Logger = scope.ServiceProvider.GetService<ILogger<LogManager>>();
                });
        }
    }
}
