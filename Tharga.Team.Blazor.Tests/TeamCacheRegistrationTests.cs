using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// <see cref="ITeamCache"/> registration: the built-in is supplied so nobody has to configure anything, and a
/// host that registers its own wins — which is the whole point of the seam, because the built-in cannot serve
/// a multi-instance deployment.
/// </summary>
public class TeamCacheRegistrationTests
{
    private const string ValidAzureAdConfig = """
        { "AzureAd": { "Authority": "https://test.ciamlogin.com/test", "ClientId": "c", "TenantId": "t", "CallbackPath": "/signin-oidc" } }
        """;

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ValidAzureAdConfig));
        builder.Configuration.AddJsonStream(stream);
        return builder;
    }

    private sealed class SharedCache : ITeamCache
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

    /// <summary>
    /// A singleton, not scoped. The services that read it are scoped, so a scoped cache would live for one
    /// request and cache nothing across the requests it exists to serve.
    /// </summary>
    [Fact]
    public void TheBuiltIn_IsRegisteredAsASingleton()
    {
        var builder = CreateBuilder();
        builder.AddThargaTeam();

        var descriptor = Assert.Single(builder.Services, d => d.ServiceType == typeof(ITeamCache));

        Assert.Equal(typeof(InMemoryTeamCache), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void TheBuiltIn_Resolves()
    {
        var builder = CreateBuilder();
        builder.AddThargaTeam();

        using var provider = builder.Services.BuildServiceProvider();

        Assert.IsType<InMemoryTeamCache>(provider.GetRequiredService<ITeamCache>());
    }

    /// <summary>
    /// Registered with <c>TryAdd</c>, so a host running more than one instance replaces it with a shared
    /// implementation. Without this the seam exists but cannot be used, which is worse than no seam.
    /// </summary>
    [Fact]
    public void AHostRegistration_Wins()
    {
        var builder = CreateBuilder();
        builder.Services.AddSingleton<ITeamCache, SharedCache>();
        builder.AddThargaTeam();

        using var provider = builder.Services.BuildServiceProvider();

        Assert.IsType<SharedCache>(provider.GetRequiredService<ITeamCache>());
    }

    /// <summary>
    /// The granular path registers it too. Icons and the email sender have both been reported missing from
    /// this path (Tharga/Team#157, #176); a cache that only the facade registers would be the same defect.
    /// </summary>
    [Fact]
    public void TheGranularPath_RegistersItToo()
    {
        var services = new ServiceCollection();
        services.AddThargaTeamBlazor();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<InMemoryTeamCache>(provider.GetRequiredService<ITeamCache>());
    }
}
