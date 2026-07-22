

using Azure.Core;
using Azure.Identity;
using AzureLogShipper.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// Sends any serializable log object to an Azure Log Analytics custom table.
/// Use your own POCO or the built-in <c>MonitoringLog</c> from
/// <c>AzureLogShipper.Models</c>.
/// Supports Workspace Key (HMAC-SHA256) and Azure AD (Entra ID) authentication.
/// </summary>


/// <summary>
/// Sends any serializable log object to an Azure Log Analytics custom table.
/// Use your own POCO or the built-in <c>MonitoringLog</c> from
/// <c>AzureLogShipper.Models</c>.
/// Supports Workspace Key (HMAC-SHA256) and Azure AD (Entra ID) authentication.
/// </summary>
public sealed class LogShipperClient : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly LogShipperOptions _options;
    private readonly HttpClient _http;
    private readonly TokenCredential? _credential;

    // ── Constructors ───────────────────────────────────────────────────────

    /// <summary>Creates the client with the supplied options and an optional pre-built HttpClient.</summary>
    public LogShipperClient(LogShipperOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _http = httpClient ?? new HttpClient();

        if (_options.AuthMode == AuthMode.AzureAD)
            _credential = BuildCredential(options);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Posts a single log object to the specified custom table.</summary>
    public Task SendAsync<T>(T log, string logType, CancellationToken ct = default)
        => SendAsync([log!], logType, ct);

    /// <summary>Posts a batch of log objects to the specified custom table.</summary>
    public async Task SendAsync<T>(IEnumerable<T> logs, string logType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(logs);
        ArgumentException.ThrowIfNullOrWhiteSpace(logType);

        var body = JsonSerializer.Serialize(logs, _jsonOptions);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var dateString = DateTime.UtcNow.ToString("r");   // RFC 1123 format

        using var request = _options.AuthMode == AuthMode.WorkspaceKey
            ? BuildWorkspaceKeyRequest(body, bodyBytes, dateString, logType)
            : await BuildAzureAdRequestAsync(body, logType, ct);

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var exbody = await response.Content.ReadAsStringAsync(ct);
            var ex = new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {exbody}",
                inner: null,
                statusCode: response.StatusCode);
            ex.Data["ResponseBody"] = body;
            throw ex;
        }
    }

    // ── Workspace Key (HMAC-SHA256) ────────────────────────────────────────

    private HttpRequestMessage BuildWorkspaceKeyRequest(
        string body, byte[] bodyBytes, string dateString, string logType)
    {
        if (string.IsNullOrWhiteSpace(_options.SharedKey))
            throw new InvalidOperationException("SharedKey is required for WorkspaceKey auth.");

        // Use UTF8 without BOM — must match exactly what is sent over the wire
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var exactBytes = utf8NoBom.GetBytes(body);

        var signature = BuildHmacSignature(exactBytes.Length, dateString, _options.SharedKey);
        var authHeader = $"SharedKey {_options.WorkspaceId}:{signature}";
        var url = $"https://{_options.WorkspaceId}.ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

        var content = new ByteArrayContent(exactBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentLength = exactBytes.Length;

        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.Add("Log-Type", logType);
        req.Headers.Add("x-ms-date", dateString);
        if (!string.IsNullOrWhiteSpace(_options.TimeGeneratedField))
            req.Headers.Add("time-generated-field", _options.TimeGeneratedField);
        req.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        return req;
    }

    private static string BuildHmacSignature(int contentLength, string dateString, string sharedKey)
    {
        // Exact format required by the Log Analytics Data Collector API:
        // VERB + \n + Content-Length + \n + Content-Type + \n + x-ms-date: + date + \n + /api/logs
        var stringToSign = $"POST\n{contentLength}\napplication/json\nx-ms-date:{dateString}\n/api/logs";
        var keyBytes = Convert.FromBase64String(sharedKey);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(hashBytes);
    }

    // ── Azure AD (Entra ID) ────────────────────────────────────────────────

    private async Task<HttpRequestMessage> BuildAzureAdRequestAsync(string body, string logType, CancellationToken ct)
    {
        const string scope = "https://monitor.azure.com//.default";
        var tokenCtx = new TokenRequestContext([scope]);
        var token = await _credential!.GetTokenAsync(tokenCtx, ct);

        // Azure Monitor Logs Ingestion API endpoint
        var url = $"https://{_options.WorkspaceId}.ods.opinsights.azure.com" +
                  $"/api/logs?api-version=2016-04-01";

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Log-Type", logType);
        if (!string.IsNullOrWhiteSpace(_options.TimeGeneratedField))
            req.Headers.Add("time-generated-field", _options.TimeGeneratedField);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return req;
    }

    private static TokenCredential BuildCredential(LogShipperOptions opts)
    {
        // Service principal with client secret
        if (!string.IsNullOrWhiteSpace(opts.TenantId) &&
            !string.IsNullOrWhiteSpace(opts.ClientId) &&
            !string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            return new ClientSecretCredential(opts.TenantId, opts.ClientId, opts.ClientSecret);
        }

        // Managed Identity with explicit client ID
        if (!string.IsNullOrWhiteSpace(opts.ClientId))
            return new ManagedIdentityCredential(opts.ClientId);

        // DefaultAzureCredential: works for managed identity, CLI, env vars, etc.
        return new DefaultAzureCredential();
    }

    public void Dispose() => _http.Dispose();
}