using Azure.Core;

namespace Tharga.Team.Entra;

/// <summary>
/// Options for the Entra user-directory provider. <see cref="TenantId"/>, <see cref="ClientId"/> and
/// <see cref="ClientSecret"/> are bound from the <c>AzureAd</c> configuration section by
/// <c>AddThargaEntraUserDirectory</c>; alternatively set <see cref="Credential"/> to authenticate with
/// any <see cref="TokenCredential"/> (certificate, managed identity, …) instead of a client secret.
/// </summary>
public class EntraDirectoryOptions
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    /// <summary>Overrides secret-based authentication with a custom credential when set.</summary>
    public TokenCredential Credential { get; set; }

    /// <summary>Microsoft Graph base address. Trailing slash required.</summary>
    public Uri GraphBaseAddress { get; set; } = new("https://graph.microsoft.com/v1.0/");

    /// <summary>The token scope requested for Graph calls.</summary>
    public string Scope { get; set; } = "https://graph.microsoft.com/.default";
}
