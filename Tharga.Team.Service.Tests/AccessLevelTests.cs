using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class AccessLevelTests
{
    [Fact]
    public void Custom_Is_The_Lowest_Privilege_Tier()
    {
        // The whole Custom design relies on it being the highest numeric value (lowest privilege):
        // it must fail every `accessLevel > minimumLevel` gate and resolve to no base scopes.
        var max = Enum.GetValues<AccessLevel>().Max();

        Assert.Equal(AccessLevel.Custom, max);
        Assert.True(AccessLevel.Custom > AccessLevel.Viewer);
        Assert.True(AccessLevel.Custom > AccessLevel.Administrator);
    }
}
