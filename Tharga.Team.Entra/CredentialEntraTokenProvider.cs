using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
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

    public CredentialEntraTokenProvider(IOptions<EntraDirectoryOptions> options, ILogger<CredentialEntraTokenProvider> logger = null)
    {
        _options = options.Value;
        WarnIfPartiallyConfigured(logger);
    }

    /// <summary>
    /// Distinguishes "not wanted" from "got it wrong", because only one of them is a mistake.
    /// </summary>
    /// <remarks>
    /// No credential field set at all reads as a deliberate opt-out — a host that registers the
    /// directory in every environment but supplies secrets in only some of them, which is the common
    /// shape. That stays silent.
    /// <para>
    /// <i>Some</i> fields set and others empty cannot be deliberate: nobody half-fills a credential on
    /// purpose. That is worth a warning naming exactly which values are missing, because the symptom —
    /// directory features quietly absent from the admin page — otherwise gives no clue where to look.
    /// </para>
    /// <para>
    /// Logged from the constructor rather than from <see cref="IsConfigured"/>: the provider is a
    /// singleton, so this warns once per process instead of on every page render, and a property getter
    /// with a logging side effect is a trap for the next reader.
    /// </para>
    /// </remarks>
    private void WarnIfPartiallyConfigured(ILogger logger)
    {
        if (IsConfigured) return;
        if (_options.Credential != null) return;

        var missing = MissingCredentialFields(_options);
        if (missing.Length == 0 || missing.Length == CredentialFieldCount) return;

        logger?.LogWarning(
            "The Entra user directory is registered but only partly configured: {MissingFields} " +
            "{MissingCount} missing. Directory features (verify, the directory column, the " +
            "directory-only tab, delete-from-directory) stay hidden until this is complete. Set the " +
            "missing values, supply a Credential, or remove AddThargaEntraUserDirectory if the " +
            "directory is not wanted.",
            string.Join(", ", missing), missing.Length);
    }

    private const int CredentialFieldCount = 3;

    private static string[] MissingCredentialFields(EntraDirectoryOptions options)
    {
        var missing = new List<string>(CredentialFieldCount);
        if (string.IsNullOrEmpty(options.TenantId)) missing.Add(nameof(EntraDirectoryOptions.TenantId));
        if (string.IsNullOrEmpty(options.ClientId)) missing.Add(nameof(EntraDirectoryOptions.ClientId));
        if (string.IsNullOrEmpty(options.ClientSecret)) missing.Add(nameof(EntraDirectoryOptions.ClientSecret));
        return [.. missing];
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
