using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Tests for umbrella / implied scopes (Tharga/Platform#102): a granted scope that declares
/// <see cref="ScopeDefinition.Implies"/> resolves to the implied scopes too, transitively and cycle-safe.
/// </summary>
public class ImpliedScopesTests
{
    [Fact]
    public void Umbrella_Scope_Expands_To_Implied_When_Granted_Via_Override()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Administrator);
        registry.Register("doc:write", AccessLevel.Administrator);
        registry.Register("doc:manage", AccessLevel.Administrator, implies: new[] { "doc:read", "doc:write" });

        // Custom grants no base scopes; the umbrella arrives only via the override.
        var scopes = registry.GetEffectiveScopes(AccessLevel.Custom, null, new[] { "doc:manage" });

        Assert.Contains("doc:manage", scopes);
        Assert.Contains("doc:read", scopes);
        Assert.Contains("doc:write", scopes);
    }

    [Fact]
    public void Implied_Scopes_Not_Added_When_Umbrella_Not_Granted()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Viewer);
        registry.Register("doc:manage", AccessLevel.Administrator, implies: new[] { "doc:read" });

        var scopes = registry.GetEffectiveScopes(AccessLevel.Viewer, null, null);

        Assert.Contains("doc:read", scopes);
        Assert.DoesNotContain("doc:manage", scopes);
    }

    [Fact]
    public void Implied_Expansion_Is_Transitive()
    {
        var registry = new ScopeRegistry();
        registry.Register("c", AccessLevel.Administrator);
        registry.Register("b", AccessLevel.Administrator, implies: new[] { "c" });
        registry.Register("a", AccessLevel.Administrator, implies: new[] { "b" });

        var scopes = registry.GetEffectiveScopes(AccessLevel.Custom, null, new[] { "a" });

        Assert.Contains("a", scopes);
        Assert.Contains("b", scopes);
        Assert.Contains("c", scopes);
    }

    [Fact]
    public void Implied_Expansion_Is_Cycle_Safe()
    {
        var registry = new ScopeRegistry();
        registry.Register("a", AccessLevel.Administrator, implies: new[] { "b" });
        registry.Register("b", AccessLevel.Administrator, implies: new[] { "a" });

        var scopes = registry.GetEffectiveScopes(AccessLevel.Custom, null, new[] { "a" });

        Assert.Equal(2, scopes.Count);
        Assert.Contains("a", scopes);
        Assert.Contains("b", scopes);
    }

    [Fact]
    public void Administrator_Gets_Umbrella_And_Implied()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:read", AccessLevel.Administrator);
        registry.Register("doc:write", AccessLevel.Administrator);
        registry.Register("doc:manage", AccessLevel.Administrator, implies: new[] { "doc:read", "doc:write" });

        var scopes = registry.GetEffectiveScopes(AccessLevel.Administrator, null, null);

        Assert.Contains("doc:manage", scopes);
        Assert.Contains("doc:read", scopes);
        Assert.Contains("doc:write", scopes);
    }

    [Fact]
    public void ScopeDefinition_Stores_Implies()
    {
        var registry = new ScopeRegistry();
        registry.Register("doc:manage", AccessLevel.Administrator, "Manage docs.", implies: new[] { "doc:read" });

        var def = registry.All.Single(s => s.Name == "doc:manage");
        Assert.Equal(new[] { "doc:read" }, def.Implies);
    }
}
