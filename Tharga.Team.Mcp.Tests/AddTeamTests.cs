using Microsoft.Extensions.DependencyInjection;
using Tharga.Mcp;
using Tharga.Team.Mcp;
using Tharga.Team;

namespace Tharga.Team.Mcp.Tests;

public class AddTeamTests
{
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
