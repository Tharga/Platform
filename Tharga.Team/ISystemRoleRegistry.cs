namespace Tharga.Team;

/// <summary>
/// Maps app/global roles (e.g. "Developer", "Administrator") to system-level scopes, so privileged users gain
/// those scopes as claims — the user-side counterpart to a system API key's explicit scope list.
/// </summary>
public interface ISystemRoleRegistry
{
    /// <summary>Returns the distinct system scopes granted by the given role names.</summary>
    IReadOnlyList<string> GetScopesForRoles(IEnumerable<string> roleNames);
}
