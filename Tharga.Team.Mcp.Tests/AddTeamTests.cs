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

    /// <summary>
    /// The credential an MCP caller actually presents. Without this the endpoint's policy names no scheme,
    /// so it authenticates against the application's default one — OIDC in a Blazor host — and an agent
    /// with a valid API key is answered with a 302 to a login page (Tharga/Mcp#18).
    /// </summary>
    [Fact]
    public void AddTeam_ContributesTheApiKeyAuthenticationScheme()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp => mcp.AddTeam());

        var options = services.BuildServiceProvider().GetRequiredService<ThargaMcpOptions>();
        Assert.Contains(ApiKeyConstants.SchemeName, options.AuthenticationSchemes);
    }

    /// <summary>A host may add its own alongside; contributing must not replace what is already there.</summary>
    [Fact]
    public void AddTeam_LeavesAHostContributedSchemeInPlace()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.Options.AuthenticationSchemes.Add("Cookies");
            mcp.AddTeam();
        });

        var options = services.BuildServiceProvider().GetRequiredService<ThargaMcpOptions>();
        Assert.Equal(["Cookies", ApiKeyConstants.SchemeName], options.AuthenticationSchemes);
    }

    /// <summary>Registering twice must not duplicate the scheme in the policy.</summary>
    [Fact]
    public void AddTeam_DoesNotContributeTheSchemeTwice()
    {
        var services = new ServiceCollection();

        services.AddThargaMcp(mcp =>
        {
            mcp.AddTeam();
            mcp.AddTeam();
        });

        var options = services.BuildServiceProvider().GetRequiredService<ThargaMcpOptions>();
        Assert.Single(options.AuthenticationSchemes, x => x == ApiKeyConstants.SchemeName);
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

    /// <summary>
    /// The MCP registrations survive container validation — no singleton captures a scoped service.
    /// </summary>
    /// <remarks>
    /// <b>The guard that was missing, and it cost a broken release.</b> 3.10.1 gave the singleton
    /// <c>HttpContextMcpContextAccessor</c> constructor dependencies on scoped services
    /// (<c>IUserService</c>, <c>ITeamService</c>). Every unit test passed — none of them built a
    /// validated container — while the application could not start at all:
    /// <c>Cannot consume scoped service 'IUserService' from singleton 'IMcpContextAccessor'</c>.
    /// <para>
    /// This is exactly the failure mode a consuming host reported about a different defect the same day:
    /// <c>ValidateOnBuild</c> is on by default only in Development, so a suite that never validates stays
    /// green while startup is broken. Asserting it here is the cheap version of running the app.
    /// </para>
    /// <para>
    /// Scoped services belong on <c>HttpContext.RequestServices</c> for a singleton like this one — the
    /// request's own scope — not in its constructor.
    /// </para>
    /// </remarks>
    [Fact]
    public void AddTeam_SurvivesContainerValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        // Registered as scoped, exactly as a host registers them -- and this is load-bearing. Without
        // them the container reports "unable to resolve" instead of "cannot consume scoped", and the
        // guard silently passes against the very bug it exists to catch. Verified by reintroducing that
        // bug: the guard is green without these lines and red with them.
        services.AddScoped(_ => Substitute.For<IUserService>());
        services.AddScoped(_ => Substitute.For<ITeamService>());
        services.AddScoped(_ => Substitute.For<ITeamManagementService>());

        services.AddThargaMcp(mcp => mcp.AddTeam());

        var exception = Record.Exception(() => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        }));

        // Asserts the absence of one failure class, not of all failures. This bare collection has no
        // host services in it, so validation legitimately complains about IHostApplicationLifetime and
        // the like; making it complete would be re-registering the whole application to test one rule,
        // and the test would then break for reasons that have nothing to do with lifetimes.
        Assert.DoesNotContain("Cannot consume scoped service", exception?.ToString() ?? string.Empty);
    }
}
