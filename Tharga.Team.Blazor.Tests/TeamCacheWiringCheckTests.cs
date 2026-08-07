using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The startup check that fails a host which registered a custom <see cref="ITeamCache"/> its own services
/// never received. Detection logic is covered by <c>TeamCacheWiringTests</c>; this covers the wiring — that it
/// is registered, and that it stays silent for a default host.
/// </summary>
/// <remarks>
/// <b>The silent-for-default case is the one that must not regress.</b> This check throws, so a false positive
/// would stop every existing consumer from booting after a routine upgrade.
/// </remarks>
public class TeamCacheWiringCheckTests
{
    private static ServiceCollection BaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(
            sp => (ServerAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
        return services;
    }

    private static IHostedService[] HostedServices(IServiceProvider provider)
        => provider.GetServices<IHostedService>().ToArray();

    [Fact]
    public void TheCheck_IsRegistered()
    {
        var services = BaseServices();
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<StubTeamService, StubUserService>());

        using var provider = services.BuildServiceProvider();

        Assert.Contains(HostedServices(provider), s => s.GetType().Name == "TeamCacheWiringCheck");
    }

    /// <summary>
    /// A host that configured nothing must start. The container's built-in cache and the bases' fallback are
    /// two different <see cref="InMemoryTeamCache"/> instances, so an identity comparison alone would fire
    /// here — and this check throws.
    /// </summary>
    [Fact]
    public async Task ADefaultHost_Starts()
    {
        var services = BaseServices();
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<StubTeamService, StubUserService>());

        using var provider = services.BuildServiceProvider();
        var check = HostedServices(provider).Single(s => s.GetType().Name == "TeamCacheWiringCheck");

        await check.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// The stubs take no <see cref="ITeamCache"/> on their constructors, which is exactly the host mistake this
    /// exists to catch — so registering a custom cache alongside them must fail startup, naming the types.
    /// </summary>
    [Fact]
    public async Task AHostThatRegisteredACustomCacheWithoutForwardingIt_FailsStartup()
    {
        var services = BaseServices();
        services.AddSingleton<ITeamCache, NotForwardedCache>();
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<StubTeamService, StubUserService>());

        using var provider = services.BuildServiceProvider();
        var check = HostedServices(provider).Single(s => s.GetType().Name == "TeamCacheWiringCheck");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => check.StartAsync(CancellationToken.None));

        Assert.Contains(nameof(StubTeamService), ex.Message);
        Assert.Contains("constructor", ex.Message);
    }

    private sealed class NotForwardedCache : ITeamCache
    {
        public Task<CachedValue<IUser>> GetUserAsync(string identity) => Task.FromResult(CachedValue<IUser>.Miss);
        public Task SetUserAsync(string identity, IUser user) => Task.CompletedTask;
        public Task RemoveUserAsync(string identity) => Task.CompletedTask;
        public Task RemoveUserByKeyAsync(string userKey) => Task.CompletedTask;
        public Task<CachedValue<ITeamMember>> GetMemberAsync(string teamKey, string userKey) => Task.FromResult(CachedValue<ITeamMember>.Miss);
        public Task SetMemberAsync(string teamKey, string userKey, ITeamMember member) => Task.CompletedTask;
        public Task RemoveMemberAsync(string teamKey, string userKey) => Task.CompletedTask;
        public Task RemoveMembersForUserAsync(string userKey) => Task.CompletedTask;
        public Task<CachedValue<IReadOnlyList<TenantRoleDefinition>>> GetCustomRolesAsync(string teamKey) => Task.FromResult(CachedValue<IReadOnlyList<TenantRoleDefinition>>.Miss);
        public Task SetCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles) => Task.CompletedTask;
        public Task RemoveCustomRolesAsync(string teamKey) => Task.CompletedTask;
    }
}
