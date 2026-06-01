using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Verifies that tenant-role scopes actually resolve through the resolved <see cref="IScopeRegistry"/> —
/// i.e. that <c>AddThargaTenantRoles</c> links the role registry to the LIVE scope-registry instance.
/// </summary>
public class TenantRoleScopeLinkageTests
{
    [Fact]
    public void RoleScopes_ResolveThroughResolvedScopeRegistry_WhenScopesRegisteredFirst()
    {
        var services = new ServiceCollection();
        services.AddThargaScopes(s => s.Register("a:read", AccessLevel.Viewer));
        services.AddThargaTenantRoles(r => r.Register("Editor", new[] { "a:read", "x:write" }));

        var registry = services.BuildServiceProvider().GetRequiredService<IScopeRegistry>();
        var effective = registry.GetEffectiveScopes(AccessLevel.Custom, new[] { "Editor" }, null);

        Assert.Contains("x:write", effective); // role scope must flow through
        Assert.Contains("a:read", effective);
    }

    [Fact]
    public void RoleScopes_ResolveThroughResolvedScopeRegistry_WhenRolesRegisteredFirst()
    {
        var services = new ServiceCollection();
        services.AddThargaTenantRoles(r => r.Register("Editor", new[] { "x:write" }));
        services.AddThargaScopes(s => s.Register("a:read", AccessLevel.Viewer));

        var registry = services.BuildServiceProvider().GetRequiredService<IScopeRegistry>();
        var effective = registry.GetEffectiveScopes(AccessLevel.Custom, new[] { "Editor" }, null);

        Assert.Contains("x:write", effective);
    }
}
