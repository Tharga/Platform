namespace Tharga.Team.Entra;

/// <summary>
/// Supplies bearer tokens for Microsoft Graph calls. The default implementation
/// (<see cref="CredentialEntraTokenProvider"/>) authenticates with the configured credential; replace
/// the registration to source tokens differently.
/// </summary>
public interface IEntraTokenProvider
{
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
