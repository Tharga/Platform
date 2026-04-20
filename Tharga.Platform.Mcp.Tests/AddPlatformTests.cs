using Microsoft.Extensions.DependencyInjection;
using Tharga.Mcp;
using Tharga.Platform.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp.Tests;

public class AddPlatformTests
{
    [Fact]
    public void ReplacesDefaultContextAccessorWithHttpContextBacked()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddPlatform();
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
            mcp.AddPlatform(o => o.ExposeSystemResources = false);
        });

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(PlatformSystemResourceProvider));
    }

    [Fact]
    public void ExposeSystemResources_True_RegistersSystemProvider()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddPlatform(o => o.ExposeSystemResources = true);
        });

        Assert.Contains(services, d => d.ServiceType == typeof(PlatformSystemResourceProvider));
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
            mcp.AddPlatform();
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
            mcp.AddPlatform();
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
            mcp.AddPlatform(o => o.DeveloperRole = "SuperAdmin");
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<McpPlatformOptions>>();

        Assert.Equal("SuperAdmin", options.Value.DeveloperRole);
    }

    [Fact]
#pragma warning disable CS0618 // Type or member is obsolete
    public void ObsoleteAddMcpPlatform_ForwardsToAddPlatform()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddMcpPlatform();
        });

        var provider = services.BuildServiceProvider();
        Assert.IsType<HttpContextMcpContextAccessor>(provider.GetRequiredService<IMcpContextAccessor>());
    }
#pragma warning restore CS0618
}
