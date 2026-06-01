using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class SystemRoleRegistryTests
{
    [Fact]
    public void GetScopesForRoles_Maps_Dedups_And_IsCaseInsensitive()
    {
        var registry = new SystemRoleRegistry();
        registry.Map("Developer", "system:teams:read", "mcp:discover");
        registry.Map("Administrator", "system:teams:read", "system:metrics:read");

        var scopes = registry.GetScopesForRoles(new[] { "developer", "Administrator" });

        Assert.Equal(3, scopes.Count); // system:teams:read deduped across the two roles
        Assert.Contains("system:teams:read", scopes);
        Assert.Contains("mcp:discover", scopes);
        Assert.Contains("system:metrics:read", scopes);
    }

    [Fact]
    public void GetScopesForRoles_UnknownRole_ReturnsEmpty()
    {
        var registry = new SystemRoleRegistry();
        registry.Map("Developer", "system:teams:read");

        Assert.Empty(registry.GetScopesForRoles(new[] { "Nobody" }));
    }

    [Fact]
    public void Map_DuplicateRole_Throws()
    {
        var registry = new SystemRoleRegistry();
        registry.Map("Developer", "a");

        Assert.Throws<InvalidOperationException>(() => registry.Map("Developer", "b"));
    }
}
