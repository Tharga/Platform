using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.Service;

namespace Tharga.Team.Blazor.Tests;

public class OptionalRegistryTests
{
    [Fact]
    public void ScopeRegistry_IsNull_WhenNotRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<IScopeRegistry>();

        Assert.Null(registry);
    }

    [Fact]
    public void TenantRoleRegistry_IsNull_WhenNotRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<ITenantRoleRegistry>();

        Assert.Null(registry);
    }

    [Fact]
    public void ScopeRegistry_IsResolved_WhenRegistered()
    {
        var services = new ServiceCollection();
        services.AddThargaScopes(scopes =>
        {
            scopes.Register("test:read", AccessLevel.Viewer);
        });
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<IScopeRegistry>();

        Assert.NotNull(registry);
        Assert.Single(registry.All);
    }

    [Fact]
    public void TenantRoleRegistry_IsResolved_WhenRegistered()
    {
        var services = new ServiceCollection();
        services.AddThargaScopes(_ => { });
        services.AddThargaTenantRoles(roles =>
        {
            roles.Register("Editor", new[] { "test:read" });
        });
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<ITenantRoleRegistry>();

        Assert.NotNull(registry);
        Assert.Single(registry.All);
    }

    [Fact]
    public void AddThargaTeamBlazor_DoesNotRequireScopeRegistry()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddThargaTeamBlazor());

        Assert.Null(exception);
    }

    [Fact]
    public void AddThargaTeamBlazor_DoesNotRequireTenantRoleRegistry()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddThargaTeamBlazor(o =>
        {
            o.ShowMemberRoles = true;
            o.ShowScopeOverrides = true;
        }));

        Assert.Null(exception);
    }
}
