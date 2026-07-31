using Microsoft.Extensions.DependencyInjection;
using Tharga.Mcp;
using Tharga.Team.Mcp;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Mcp.Tests;

public class AddTeamTests
{
    /// <summary>
    /// A host that already registered <c>mcp:discover</c> must still start. Registering it as a system
    /// scope by hand was the documented workaround while the checker read system claims only, so the
    /// consumers this feature is for are the ones most likely to have it — and both registries throw on a
    /// duplicate, which would turn the fix into a startup crash on upgrade.
    /// </summary>
    [Fact]
    public void AddTeam_DoesNotThrow_WhenTheHostAlreadyRegisteredDiscoverAsASystemScope()
    {
        var services = new ServiceCollection();
        services.AddThargaSystemScopes(scopes => scopes.Register(McpScopes.Discover, "Host registration."));

        var act = () => services.AddThargaMcp(mcp => mcp.AddTeam());

        Assert.Null(Record.Exception(act));
    }

    [Fact]
    public void AddTeam_DoesNotThrow_WhenTheHostAlreadyRegisteredDiscoverAsATeamScope()
    {
        var services = new ServiceCollection();
        services.AddThargaScopes(scopes => scopes.Register(McpScopes.Discover, AccessLevel.Viewer, "Host registration."));

        var act = () => services.AddThargaMcp(mcp => mcp.AddTeam());

        Assert.Null(Record.Exception(act));
    }

    /// <summary>The host's own description wins — the toolkit skips rather than replaces.</summary>
    [Fact]
    public void AddTeam_LeavesAnExistingRegistrationIntact()
    {
        var services = new ServiceCollection();
        services.AddThargaSystemScopes(scopes => scopes.Register(McpScopes.Discover, "Host registration."));

        services.AddThargaMcp(mcp => mcp.AddTeam());

        var registry = services.BuildServiceProvider().GetRequiredService<ISystemScopeRegistry>();
        var registered = registry.All.Where(x => x.Name == McpScopes.Discover).ToArray();
        Assert.Single(registered);
        Assert.Equal("Host registration.", registered[0].Description);
    }

    [Fact]
    public void ReplacesDefaultContextAccessorWithHttpContextBacked()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam();
        });

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IMcpContextAccessor>();

        Assert.IsType<HttpContextMcpContextAccessor>(accessor);
    }

    [Fact]
    public void ExposeSystemResources_False_DoesNotRegisterSystemProvider()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam(o => o.ExposeSystemResources = false);
        });

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(TeamSystemResourceProvider));
    }

    [Fact]
    public void ExposeSystemResources_True_RegistersSystemProvider()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam(o => o.ExposeSystemResources = true);
        });

        Assert.Contains(services, d => d.ServiceType == typeof(TeamSystemResourceProvider));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IMcpResourceProvider) &&
            d.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void RegistersScopeChecker()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam();
        });

        var provider = services.BuildServiceProvider();
        var checker = provider.GetRequiredService<IMcpScopeChecker>();

        Assert.IsType<McpScopeChecker>(checker);
    }

    [Fact]
    public void RegistersBuiltInMcpScopes()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam();
        });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IScopeRegistry>();

        Assert.Contains(registry.All, s => s.Name == McpScopes.Discover);
    }

    [Fact]
    public void CustomOptions_DeveloperRoleIsApplied()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam(o => o.DeveloperRole = "SuperAdmin");
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<McpTeamOptions>>();

        Assert.Equal("SuperAdmin", options.Value.DeveloperRole);
    }
}
