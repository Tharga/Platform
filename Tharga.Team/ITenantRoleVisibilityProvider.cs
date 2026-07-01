namespace Tharga.Team;

/// <summary>
/// Decides whether a given tenant role should be offered in the role editor for a given team.
/// Consulted by the team role editor before building its per-team role list, letting a consumer
/// hide feature-gated roles from teams where the feature is disabled.
/// </summary>
/// <remarks>
/// Visibility is a UI-editor concern only. Hiding a role does not prune existing assignments and
/// does not affect scope resolution — a member already assigned a hidden role keeps it and still
/// receives its scopes at runtime. The default implementation shows every role.
/// </remarks>
public interface ITenantRoleVisibilityProvider
{
    /// <summary>
    /// Returns whether <paramref name="roleName"/> should be selectable in the role editor for the
    /// team identified by <paramref name="teamKey"/>.
    /// </summary>
    Task<bool> IsRoleVisibleAsync(string teamKey, string roleName, CancellationToken cancellationToken = default);
}
