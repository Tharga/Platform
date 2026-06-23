namespace Tharga.Team.Service;

/// <summary>
/// Service-layer view of the self-service team-lifecycle policy, used by the authorization decorator.
/// Populated at registration time from the Blazor option of the same name (which lives in a higher layer).
/// </summary>
public sealed class TeamLifecycleOptions
{
    /// <summary>
    /// When false, the self-service <c>team:manage</c> paths for creating and (in-team) deleting a team are
    /// denied at the service layer. Does not affect the <c>teams:delete</c> system scope, which can always delete.
    /// </summary>
    public bool AllowTeamCreation { get; init; } = true;
}
