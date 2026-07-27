using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The team surface must render for callers without <c>users:manage</c> — team access level never grants
/// that system scope, so every ordinary member and every team owner reads the co-member projection
/// instead (Tharga/Team#139).
/// </summary>
public class UserDirectoryGateTests
{
    [Fact]
    public void Resolve_WithUsersManage_ReadsFullDirectory()
    {
        Assert.Equal(UserDirectorySource.FullDirectory, UserDirectoryGate.Resolve(true));
    }

    [Fact]
    public void Resolve_WithoutUsersManage_ReadsTeamMembers()
    {
        Assert.Equal(UserDirectorySource.TeamMembers, UserDirectoryGate.Resolve(false));
    }
}
