namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Gating decisions for the user administration surface. Viewing the admin lists and acting on users
/// (verify, delete) require the <c>users:manage</c> system scope — the service layer enforces the same
/// rule, so this gate is about rendering a friendly message instead of an exception. Directory features
/// additionally require a registered <see cref="IUserDirectoryService"/> — without one they are hidden
/// entirely, not disabled.
/// </summary>
public static class UserAdminGate
{
    public static bool CanAdministerUsers(bool hasUsersManageScope)
        => hasUsersManageScope;

    public static bool ShowDirectoryFeatures(bool hasUsersManageScope, bool directoryRegistered)
        => hasUsersManageScope && directoryRegistered;

    /// <summary>
    /// Whether the Teams tab offers deleting a team. Requires the <see cref="SystemTeamScopes.Delete"/>
    /// system scope, which authorizes deleting any team irrespective of membership.
    /// </summary>
    /// <remarks>
    /// Deliberately independent of consent and of the caller's access level on the team. Consent governs
    /// what a team exposes inbound; it does not decide who may destroy it. The scope must be a
    /// <i>system</i> grant — resolve it with <c>TeamScopeGate.HasSystemScope</c>, never a bare
    /// <c>HasClaim</c>, so an in-team grant of the same name cannot satisfy it.
    /// </remarks>
    public static bool CanDeleteTeams(bool hasTeamsDeleteScope)
        => hasTeamsDeleteScope;
}
