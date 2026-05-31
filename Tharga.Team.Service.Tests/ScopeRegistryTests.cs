using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class ScopeRegistryTests
{
    [Fact]
    public void Owner_Gets_All_Scopes()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:delete", AccessLevel.Administrator);

        var scopes = registry.GetScopesForAccessLevel(AccessLevel.Owner);

        Assert.Equal(2, scopes.Count);
        Assert.Contains("doc:read", scopes);
        Assert.Contains("doc:delete", scopes);
    }

    [Fact]
    public void Administrator_Gets_All_Scopes()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:delete", AccessLevel.Administrator);

        var scopes = registry.GetScopesForAccessLevel(AccessLevel.Administrator);

        Assert.Equal(2, scopes.Count);
    }

    [Fact]
    public void User_Gets_User_And_Viewer_Scopes()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:download", AccessLevel.User);
        registry.Register("doc:delete", AccessLevel.Administrator);

        var scopes = registry.GetScopesForAccessLevel(AccessLevel.User);

        Assert.Equal(2, scopes.Count);
        Assert.Contains("doc:read", scopes);
        Assert.Contains("doc:download", scopes);
        Assert.DoesNotContain("doc:delete", scopes);
    }

    [Fact]
    public void Viewer_Gets_Only_Viewer_Scopes()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:download", AccessLevel.User);
        registry.Register("doc:delete", AccessLevel.Administrator);

        var scopes = registry.GetScopesForAccessLevel(AccessLevel.Viewer);

        Assert.Single(scopes);
        Assert.Contains("doc:read", scopes);
    }

    [Fact]
    public void Duplicate_Registration_Throws()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);

        Assert.Throws<InvalidOperationException>(() => registry.Register("doc:read", AccessLevel.User));
    }

    [Fact]
    public void Custom_Gets_No_Base_Scopes()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:download", AccessLevel.User);
        registry.Register("doc:delete", AccessLevel.Administrator);

        var scopes = registry.GetScopesForAccessLevel(AccessLevel.Custom);

        Assert.Empty(scopes);
    }

    [Fact]
    public void Custom_Ignores_Scope_Registered_At_Custom()
    {
        // Defensive: even if a scope is registered at Custom level, Custom resolves to no base scopes.
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:custom", AccessLevel.Custom);

        var scopes = registry.GetScopesForAccessLevel(AccessLevel.Custom);

        Assert.Empty(scopes);
    }

    [Fact]
    public void Custom_Effective_Scopes_Are_Roles_Union_Overrides_Only()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:delete", AccessLevel.Administrator);

        var roleRegistry = Substitute.For<ITenantRoleRegistry>();
        roleRegistry.GetScopesForRoles(Arg.Any<IEnumerable<string>>()).Returns(new[] { "role:scope" });
        registry.SetRoleRegistry(roleRegistry);

        var scopes = registry.GetEffectiveScopes(AccessLevel.Custom, new[] { "editor" }, new[] { "override:scope" });

        Assert.Equal(2, scopes.Count);
        Assert.Contains("role:scope", scopes);
        Assert.Contains("override:scope", scopes);
        Assert.DoesNotContain("doc:read", scopes);
        Assert.DoesNotContain("doc:delete", scopes);
    }
}
