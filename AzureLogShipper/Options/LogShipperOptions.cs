using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureLogShipper.Options
{


    /// <summary>
    /// Authentication mode for the Log Analytics client.
    /// </summary>
    public enum AuthMode
    {
        /// <summary>Workspace ID + Shared Key (HMAC-SHA256). Simple but legacy.</summary>
        WorkspaceKey,

        /// <summary>Azure AD / Entra ID token via Azure.Identity (recommended).</summary>
        AzureAD
    }

    /// <summary>
    /// Configuration options for <see cref="LogShipperClient"/>.
    /// </summary>
    public class LogShipperOptions
    {
        /// <summary>The Log Analytics Workspace ID (GUID).</summary>
        public required string WorkspaceId { get; set; }

        /// <summary>Authentication mode. Defaults to WorkspaceKey.</summary>
        public AuthMode AuthMode { get; set; } = AuthMode.WorkspaceKey;

        // ── WorkspaceKey auth ──────────────────────────────────────────────────

        /// <summary>
        /// The primary or secondary shared key for the workspace.
        /// Required when <see cref="AuthMode"/> is <see cref="AuthMode.WorkspaceKey"/>.
        /// </summary>
        public string? SharedKey { get; set; }

        // ── Azure AD auth ──────────────────────────────────────────────────────

        /// <summary>
        /// Optional: explicitly set a Tenant ID.
        /// When null, DefaultAzureCredential resolves the tenant automatically
        /// (managed identity, CLI login, environment variables, etc.).
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Optional: Client ID for a specific managed identity or service principal.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Optional: Client secret for service-principal auth via Azure AD.
        /// </summary>
        public string? ClientSecret { get; set; }
    }

}
