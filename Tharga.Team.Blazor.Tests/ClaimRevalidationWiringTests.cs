using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Registration guards for the #127 revalidator. These catch the two failure modes that would only show
/// up at runtime: a DI cycle (auth-state provider -> ITeamService -> principal accessor -> auth-state
/// provider), and the circuit seeding a different instance than the one the UI/service layer reads
/// (which would present as "logged out").
/// </summary>
public class ClaimRevalidationWiringTests
{
    /// <summary>Mirrors the framework's Blazor Server auth-state registration, present before AddThargaTeamBlazor runs.</summary>
    private static ServiceCollection BaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(
            sp => (ServerAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
        return services;
    }

    [Fact]
    public void Enabled_ReplacesServerProvider_AndSeedingReachesTheSameInstanceTheUiReads()
    {
        var services = BaseServices();
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<StubTeamService, StubUserService>());

        using var scope = services.BuildServiceProvider().CreateScope();
        var authProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        var hostEnv = scope.ServiceProvider.GetRequiredService<IHostEnvironmentAuthenticationStateProvider>();

        Assert.IsType<TeamRevalidatingAuthenticationStateProvider>(authProvider);
        Assert.Same(authProvider, hostEnv); // the circuit seeds via IHostEnv — it must be the instance the UI reads
    }

    [Fact]
    public void Enabled_NoCircularDependency_BothSidesResolve()
    {
        var services = BaseServices();
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<StubTeamService, StubUserService>());

        using var scope = services.BuildServiceProvider(validateScopes: true).CreateScope();
        var sp = scope.ServiceProvider;

        // The auth-state provider and ITeamService each sit on one side of the would-be cycle; both must resolve.
        Assert.NotNull(sp.GetRequiredService<AuthenticationStateProvider>());
        Assert.NotNull(sp.GetRequiredService<ITeamService>());
        Assert.NotNull(sp.GetRequiredService<TeamClaimRevalidator>());
    }

    /// <summary>
    /// The definitive integration guard: register the auth-state provider exactly as a Blazor Server app
    /// does (<c>AddRazorComponents().AddInteractiveServerComponents()</c>) and confirm our detection keys on
    /// how the framework actually registers it (by implementation type) and replaces it. If a future
    /// framework version switched to a factory registration, our detection would silently no-op — this test
    /// would fail first.
    /// </summary>
    [Fact]
    public void Enabled_WrapsTheRealFrameworkServerProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRazorComponents().AddInteractiveServerComponents();

        var frameworkDescriptor = services.Last(d => d.ServiceType == typeof(AuthenticationStateProvider));
        Assert.NotNull(frameworkDescriptor.ImplementationType); // framework registers by type — our detection depends on this
        Assert.True(typeof(ServerAuthenticationStateProvider).IsAssignableFrom(frameworkDescriptor.ImplementationType));

        services.AddThargaTeamBlazor(o => o.RegisterTeamService<StubTeamService, StubUserService>());

        var afterDescriptor = services.Last(d => d.ServiceType == typeof(AuthenticationStateProvider));
        Assert.Null(afterDescriptor.ImplementationType); // replaced with our factory -> the revalidator
    }

    [Fact]
    public void Disabled_LeavesTheFrameworkServerProviderInPlace()
    {
        var services = BaseServices();
        services.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService>();
            o.ClaimRevalidation.Enabled = false;
        });

        using var scope = services.BuildServiceProvider().CreateScope();
        var authProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();

        Assert.IsType<ServerAuthenticationStateProvider>(authProvider); // exact type — not replaced
    }
}
