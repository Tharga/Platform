using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Tests for <see cref="SystemRoleRegistry.Grant"/> — the merge-capable counterpart to
/// <see cref="SystemRoleRegistry.Map"/>, used when a toolkit-side grant (e.g.
/// <c>Consent.GrantTeamsRead</c>) has to compose on top of consumer configuration that may already
/// have mapped the same role.
/// </summary>
public class SystemRoleRegistryGrantTests
{
    [Fact]
    public void Grant_OnUnmappedRole_CreatesTheMapping()
    {
        var registry = new SystemRoleRegistry();

        registry.Grant("Developer", SystemTeamScopes.Read);

        Assert.Equal([SystemTeamScopes.Read], registry.GetScopesForRoles(["Developer"]));
    }

    [Fact]
    public void Grant_OnAlreadyMappedRole_MergesInsteadOfThrowing()
    {
        var registry = new SystemRoleRegistry();
        registry.Map("Developer", "audit:read");

        registry.Grant("Developer", SystemTeamScopes.Read);

        var scopes = registry.GetScopesForRoles(["Developer"]);
        Assert.Contains("audit:read", scopes);
        Assert.Contains(SystemTeamScopes.Read, scopes);
    }

    [Fact]
    public void Grant_IsIdempotent_AndDoesNotDuplicate()
    {
        var registry = new SystemRoleRegistry();

        registry.Grant("Developer", SystemTeamScopes.Read);
        registry.Grant("Developer", SystemTeamScopes.Read);

        Assert.Single(registry.GetScopesForRoles(["Developer"]));
    }

    [Fact]
    public void Grant_MatchesRoleNamesCaseInsensitively_LikeMap()
    {
        var registry = new SystemRoleRegistry();
        registry.Map("Developer", "audit:read");

        registry.Grant("developer", SystemTeamScopes.Read);

        var scopes = registry.GetScopesForRoles(["Developer"]);
        Assert.Equal(2, scopes.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Grant_IgnoresMissingRoleName(string roleName)
    {
        var registry = new SystemRoleRegistry();

        registry.Grant(roleName, SystemTeamScopes.Read);

        Assert.Empty(registry.All);
    }

    [Fact]
    public void Grant_IgnoresEmptyScopeList()
    {
        var registry = new SystemRoleRegistry();

        registry.Grant("Developer");

        Assert.Empty(registry.All);
    }

    [Fact]
    public void Map_StillThrowsOnDuplicate_SoConsumerMistakesAreStillReported()
    {
        var registry = new SystemRoleRegistry();
        registry.Map("Developer", "audit:read");

        Assert.Throws<InvalidOperationException>(() => registry.Map("Developer", SystemTeamScopes.Read));
    }
}
