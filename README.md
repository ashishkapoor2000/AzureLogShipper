A lightweight .NET 8 client by Ashish Kapoor for shipping structured logs to Azure Log Analytics custom tables.

Bring your own model — use any serializable POCO, or use the included MonitoringLog
Workspace Key (HMAC-SHA256) — simple, legacy
Azure AD / Entra ID — recommended for production (managed identity, service principal, CLI)


Installation
bashdotnet add package AzureLogShipper

Usage
Bring your own model
csharpusing AzureLogShipper;
using AzureLogShipper.Options;

// Any POCO works — no base class or interface needed
public class MyAppLog
{
    public string Service { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}

var client = new LogShipperClient(new LogShipperOptions
{
    WorkspaceId = "<your-workspace-id>",
    AuthMode    = AuthMode.WorkspaceKey,
    SharedKey   = "<your-shared-key>"
});

await client.SendAsync(new MyAppLog { Service = "OrderApi", Success = true, Message = "Done" },
    logType: "MyAppLogs");
Or use the built-in MonitoringLog
csharpusing AzureLogShipper.Models;

await client.SendAsync(new MonitoringLog
{
    AzureResource    = "MyFactory",
    SubAzureResource = "InRiver",
    Process          = "ProductSync",
    RunId            = Guid.NewGuid().ToString(),
    Success          = true,
    Information      = "Sync completed successfully"
}, logType: "AmacMonitoring");
Azure AD — Managed Identity (recommended for Azure-hosted apps)
csharpvar client = new LogShipperClient(new LogShipperOptions
{
    WorkspaceId = "<your-workspace-id>",
    AuthMode    = AuthMode.AzureAD
    // DefaultAzureCredential picks up managed identity automatically
});
Azure AD — Service Principal
csharpvar client = new LogShipperClient(new LogShipperOptions
{
    WorkspaceId  = "<your-workspace-id>",
    AuthMode     = AuthMode.AzureAD,
    TenantId     = "<tenant-id>",
    ClientId     = "<client-id>",
    ClientSecret = "<client-secret>"
});
Batch send
csharpvar logs = new List<MyAppLog> { log1, log2, log3 };
await client.SendAsync(logs, logType: "MyAppLogs");

Log Table
Logs are sent to a custom table named <logType>_CL in your workspace.
e.g. logType: "AmacMonitoring" → table AmacMonitoring_CL.

Configuration from appsettings.json
json{
  "AzureLogShipper": {
    "WorkspaceId": "<workspace-id>",
    "AuthMode": "WorkspaceKey",
    "SharedKey": "<shared-key>"
  }
}
csharpvar opts = builder.Configuration.GetSection("AzureLogShipper").Get<LogShipperOptions>();
builder.Services.AddSingleton(new LogShipperClient(opts!));

License
MIT © Ashish Kapoor