namespace Tharga.Team;

/// <summary>
/// Team lifecycle operations that precede a team existing, and so cannot be authorized against one.
/// </summary>
/// <remarks>
/// Separate from <see cref="ITeamManagementService"/> because creation is the one operation with no team
/// to name: its rule is "an authenticated caller, where the host allows team creation", enforced by
/// <c>AuthorizationTeamServiceDecorator.RequireCreateAsync</c>. Leaving it on the team-management
/// interface would make that interface half team-bound and half not, which is precisely the shape that
/// forces authorization back into per-method annotations.
/// </remarks>
public interface ITeamLifecycleService
{
    /// <summary>
    /// Creates a team owned by the caller. Requires authentication and the host's <c>AllowTeamCreation</c>
    /// — no scope, since the caller cannot hold one for a team that does not yet exist.
    /// </summary>
    Task<ITeam> CreateTeamAsync(string name = null);
}
