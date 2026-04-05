using AzureLogShipper.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AzureLogShipper.FunctionApp;

public class LogShipperFunction(LogShipperClient _shipper, ILogger<LogShipperFunction> _logger)
{

    private const string LogType = "AzureLogShipperTest";

    /// <summary>
    /// HTTP trigger — GET/POST /api/test-log
    /// Sends a test MonitoringLog entry and returns the result.
    /// </summary>
    [Function("TestLog")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "test-log")] HttpRequestData req,
        FunctionContext context)
    {
        _logger.LogInformation("TestLog function triggered.");

        var log = new MonitoringLog
        {
            EventId = DateTime.UtcNow,
            AzureResource = "FunctionApp",
            SubAzureResource = "LogShipperFunction",
            Process = "TestLog",
            RunId = Guid.NewGuid().ToString(),
            CorrelationKey = context.InvocationId,
            Source = "HttpTrigger",
            Target = "LogAnalytics",
            Success = true,
            Information = "AzureLogShipper NuGet package smoke test — entry sent successfully."
        };

        try
        {
            await _shipper.SendAsync(log, LogType);

            _logger.LogInformation("Log entry shipped to table {LogType}_CL.", LogType);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new
            {
                status = "success",
                message = $"Log entry shipped to '{LogType}_CL' in Log Analytics.",
                runId = log.RunId,
                sentAt = log.EventId
            });
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ship log entry.");

            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteAsJsonAsync(new
            {
                status = "error",
                message = ex.Message,
                stackTrace = ex.StackTrace  
            });
            return err;
        }
    }
}