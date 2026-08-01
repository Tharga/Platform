using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace Tharga.Team.Entra;

/// <summary>
/// Default <see cref="IEntraTokenProvider"/>: acquires app-only Graph tokens with the configured
/// <see cref="EntraDirectoryOptions.Credential"/>, or a <see cref="ClientSecretCredential"/> built from
/// TenantId/ClientId/ClientSecret. The credential instance is reused, so MSAL's in-memory token cache
/// avoids a token request per call.
/// </summary>
public sealed class CredentialEntraTokenProvider : IEntraTokenProvider
{
    private readonly EntraDirectoryOptions _options;
    private TokenCredential _credential;

    public CredentialEntraTokenProvider(IOptions<EntraDirectoryOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Answers the same question <see cref="CreateCredential"/> asks, from the same fields, so the two
    /// cannot disagree: a provider that reports configured must not then throw, and one that reports
    /// unconfigured must not silently work.
    /// </remarks>
    public bool IsConfigured => HasCredentials(_options);

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        _credential ??= CreateCredential();
        var token = await _credential.GetTokenAsync(new TokenRequestContext([_options.Scope]), cancellationToken);
        return token.Token;
    }

    private static bool HasCredentials(EntraDirectoryOptions options)
        => options.Credential != null
           || (!string.IsNullOrEmpty(options.TenantId)
               && !string.IsNullOrEmpty(options.ClientId)
               && !string.IsNullOrEmpty(options.ClientSecret));

    private TokenCredential CreateCredential()
    {
        if (_options.Credential != null) return _options.Credential;

        if (!HasCredentials(_options))
        {
            throw new InvalidOperationException(
                $"The Entra user directory is not configured: set {nameof(EntraDirectoryOptions.TenantId)}, " +
                $"{nameof(EntraDirectoryOptions.ClientId)} and {nameof(EntraDirectoryOptions.ClientSecret)} " +
                $"(bound from the 'AzureAd' configuration section), or provide a {nameof(EntraDirectoryOptions.Credential)}.");
        }

        return new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
    }
}
