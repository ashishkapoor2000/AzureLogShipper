A lightweight .NET 8 client by Ashish Kapoor for shipping structured logs to Azure Log Analytics custom tables.

- Bring your own model — use any serializable POCO, or use the included MonitoringLog
- Workspace Key (HMAC-SHA256) — simple, legacy
- Azure AD / Entra ID — recommended for production (managed identity, service principal, CLI)

## Installation

```bash
dotnet add package AzureLogShipper
```

## Usage

### Bring your own model

```csharp
using AzureLogShipper;
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
```

### Or use the built-in MonitoringLog

```csharp
using AzureLogShipper.Models;

await client.SendAsync(new MonitoringLog
{
    AzureResource    = "MyFactory",
    SubAzureResource = "InRiver",
    Process          = "ProductSync",
    RunId            = Guid.NewGuid().ToString(),
    Success          = true,
    Information      = "Sync completed successfully"
}, logType: "AmacMonitoring");
```

### Azure AD — Managed Identity (recommended for Azure-hosted apps)

```csharp
var client = new LogShipperClient(new LogShipperOptions
{
    WorkspaceId = "<your-workspace-id>",
    AuthMode    = AuthMode.AzureAD
    // DefaultAzureCredential picks up managed identity automatically
});
```

### Azure AD — Service Principal

```csharp
var client = new LogShipperClient(new LogShipperOptions
{
    WorkspaceId  = "<your-workspace-id>",
    AuthMode     = AuthMode.AzureAD,
    TenantId     = "<tenant-id>",
    ClientId     = "<client-id>",
    ClientSecret = "<client-secret>"
});
```

### Batch send

```csharp
var logs = new List<MyAppLog> { log1, log2, log3 };
await client.SendAsync(logs, logType: "MyAppLogs");
```

### Field names on the wire

Your POCO's property names are sent as-is — no casing transform is applied. `CorrelationKey` stays
`CorrelationKey`, not `correlationKey`. Use `[JsonPropertyName("...")]` on a property if you want to
override its serialized name. (The bundled `AzureLogShipper.Models.MonitoringLog` already does this on
every property, so it always serializes the same way regardless of your own models.)

### Custom event timestamp (`TimeGeneratedField`)

By default, `TimeGenerated` on ingested rows is the time Log Analytics *received* the request. If your
model carries its own event timestamp and you want that used instead, set `TimeGeneratedField` to the
**serialized** property name (same as your C# property name, unless overridden with `[JsonPropertyName]`):

```csharp
public class MyAppLog
{
    public DateTime EventTimeUtc { get; set; }   // serializes to "EventTimeUtc"
    public string Service { get; set; }
}

var client = new LogShipperClient(new LogShipperOptions
{
    WorkspaceId        = "<your-workspace-id>",
    AuthMode           = AuthMode.WorkspaceKey,
    SharedKey          = "<your-shared-key>",
    TimeGeneratedField = "EventTimeUtc"
});
```

Leave it unset if you're not sure what your model serializes to — a mismatched field name means Log
Analytics can't find it and silently falls back to ingestion time.

## Log Table

Logs are sent to a custom table named `<logType>_CL` in your workspace.
e.g. `logType: "AmacMonitoring"` → table `AmacMonitoring_CL`.

## Configuration from appsettings.json

```json
{
  "AzureLogShipper": {
    "WorkspaceId": "<workspace-id>",
    "AuthMode": "WorkspaceKey",
    "SharedKey": "<shared-key>"
  }
}
```

```csharp
var opts = builder.Configuration.GetSection("AzureLogShipper").Get<LogShipperOptions>();
builder.Services.AddSingleton(new LogShipperClient(opts!));
```

## License

MIT © Ashish Kapoor
