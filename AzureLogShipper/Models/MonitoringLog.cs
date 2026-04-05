using System.Text.Json.Serialization;

namespace AzureLogShipper.Models
{

    /// <summary>
    /// Represents a structured monitoring log entry sent to a custom Azure Log Analytics table.
    /// </summary>
    public class MonitoringLog
    {
        /// <summary>UTC timestamp of the event.</summary>
        [JsonPropertyName("eventId")]
        public DateTime EventId { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("azureResource")]
        public string? AzureResource { get; set; }

        [JsonPropertyName("subAzureResource")]
        public string? SubAzureResource { get; set; }

        [JsonPropertyName("process")]
        public string? Process { get; set; }

        [JsonPropertyName("runId")]
        public string? RunId { get; set; }

        [JsonPropertyName("correlationKey")]
        public string? CorrelationKey { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("target")]
        public string? Target { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("information")]
        public string? Information { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("openAPIObservation")]
        public string? OpenAPIObservation { get; set; }
    }
}
