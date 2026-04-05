using AzureLogShipper;
using AzureLogShipper.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

//"LogAnalyticsWorkspace__WorkspaceID": "5b991192-3da6-45c1-b66d-a3a53240479f",
//    "LogAnalyticsWorkspace__MessageLogTableName": "AMACIntegrationLogs",
//    "LogAnalyticsWorkspace__SharedKey": "1W1qJCzcLkE0LioBTPVf7/KMvOEyB0fjTZq4jAuklZoONuSZtMZoxvb2tltEghuWJ6vLQT2qtgQEW0ovIHyMRg=="


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // Register LogShipperClient as a singleton — reads from local.settings.json / App Settings
        services.AddSingleton(_ => new LogShipperClient(new LogShipperOptions
        {
            WorkspaceId = "5b991192-3da6-45c1-b66d-a3a53240479f",
            AuthMode = AuthMode.WorkspaceKey,
            SharedKey = "1W1qJCzcLkE0LioBTPVf7/KMvOEyB0fjTZq4jAuklZoONuSZtMZoxvb2tltEghuWJ6vLQT2qtgQEW0ovIHyMRg=="
        }));
    })
    .Build();

host.Run();