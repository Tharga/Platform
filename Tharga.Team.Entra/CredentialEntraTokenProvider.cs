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

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        _credential ??= CreateCredential();
        var token = await _credential.GetTokenAsync(new TokenRequestContext([_options.Scope]), cancellationToken);
        return token.Token;
    }

    private TokenCredential CreateCredential()
    {
        if (_options.Credential != null) return _options.Credential;

        if (string.IsNullOrEmpty(_options.TenantId) || string.IsNullOrEmpty(_options.ClientId) || string.IsNullOrEmpty(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                $"The Entra user directory is not configured: set {nameof(EntraDirectoryOptions.TenantId)}, " +
                $"{nameof(EntraDirectoryOptions.ClientId)} and {nameof(EntraDirectoryOptions.ClientSecret)} " +
                $"(bound from the 'AzureAd' configuration section), or provide a {nameof(EntraDirectoryOptions.Credential)}.");
        }

        return new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
    }
}
