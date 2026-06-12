namespace Tharga.Team;

/// <summary>
/// Controls which owner-scoped ("private") API keys a listing includes, on top of the normal
/// team-wide keys. The actual entitlement is always intersected with the caller's identity, so these
/// values can never reveal a key the caller isn't allowed to see.
/// </summary>
public enum PrivateKeyScope
{
    /// <summary>Exclude all private keys (default — pure team-wide view).</summary>
    None,

    /// <summary>Include the caller's own private keys.</summary>
    Mine,

    /// <summary>Include every private key the caller is entitled to see (own, plus Developer-role and — when the host enables privileged access — Administrator/Owner).</summary>
    All,
}
