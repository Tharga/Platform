namespace Tharga.Team.Entra;

/// <summary>
/// Supplies bearer tokens for Microsoft Graph calls. The default implementation
/// (<see cref="CredentialEntraTokenProvider"/>) authenticates with the configured credential; replace
/// the registration to source tokens differently.
/// </summary>
public interface IEntraTokenProvider
{
    /// <summary>
    /// Whether this provider can acquire a token at all. <c>false</c> means credentials are missing, so
    /// <see cref="GetTokenAsync"/> would fail however it were called — which is what makes it answerable
    /// before the first call rather than during it.
    /// </summary>
    /// <remarks>Defaults to <c>true</c> so a custom provider needs no change.</remarks>
    bool IsConfigured => true;

    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
