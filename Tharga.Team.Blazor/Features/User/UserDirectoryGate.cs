namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Which user-identity source a surface should read from. The full directory requires the
/// <c>users:manage</c> system scope — team access level never grants it — so a caller without that scope
/// reads the caller-scoped co-member projection instead of failing to render.
/// </summary>
public enum UserDirectorySource
{
    /// <summary>Every user in the store — <see cref="IUserService.GetAsync"/>.</summary>
    FullDirectory,

    /// <summary>Users sharing a team with the caller — <see cref="IUserService.GetTeamMemberUsersAsync"/>.</summary>
    TeamMembers
}

/// <summary>
/// Gating decision for loading user records into a team surface. Separated from the component so the
/// choice is testable without rendering, matching <see cref="UserAdminGate"/>.
/// </summary>
public static class UserDirectoryGate
{
    public static UserDirectorySource Resolve(bool hasUsersManageScope)
        => hasUsersManageScope ? UserDirectorySource.FullDirectory : UserDirectorySource.TeamMembers;
}
