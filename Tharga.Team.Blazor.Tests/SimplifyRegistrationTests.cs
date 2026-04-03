using Microsoft.Extensions.DependencyInjection;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.Service;

namespace Tharga.Team.Blazor.Tests;

public class SimplifyRegistrationTests
{
    [Fact]
    public void RegisterTeamService_ThreeParams_RegistersITeamManagementService()
    {
        var services = new ServiceCollection();

        services.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService, StubMember>();
        });

        Assert.Contains(services, d => d.ServiceType == typeof(ITeamManagementService));
    }

    [Fact]
    public void RegisterTeamService_TwoParams_DoesNotRegisterITeamManagementService()
    {
        var services = new ServiceCollection();

        services.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService>();
        });

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(ITeamManagementService));
    }

    [Fact]
    public void RegisterTeamService_AutoRegistersDefaultScopes()
    {
        var services = new ServiceCollection();

        services.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService>();
        });

        Assert.Contains(services, d => d.ServiceType == typeof(IScopeRegistry));
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IScopeRegistry>();
        Assert.Contains(registry.All, s => s.Name == TeamScopes.Read);
        Assert.Contains(registry.All, s => s.Name == TeamScopes.Manage);
        Assert.Contains(registry.All, s => s.Name == TeamScopes.MemberInvite);
        Assert.Contains(registry.All, s => s.Name == TeamScopes.MemberRemove);
        Assert.Contains(registry.All, s => s.Name == TeamScopes.MemberRole);
        Assert.Contains(registry.All, s => s.Name == ApiKeyScopes.Manage);
    }

    [Fact]
    public void RegisterTeamService_DoesNotOverrideExistingScopeRegistry()
    {
        var services = new ServiceCollection();
        services.AddThargaScopes(scopes =>
        {
            scopes.Register("custom:scope", AccessLevel.Viewer);
        });

        services.AddThargaTeamBlazor(o =>
        {
            o.RegisterTeamService<StubTeamService, StubUserService>();
        });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IScopeRegistry>();
        Assert.Contains(registry.All, s => s.Name == "custom:scope");
        // Should not have added defaults since IScopeRegistry was already registered
        Assert.DoesNotContain(registry.All, s => s.Name == TeamScopes.Read);
    }

    [Fact]
    public void WithoutTeamService_DoesNotRegisterScopes()
    {
        var services = new ServiceCollection();

        services.AddThargaTeamBlazor();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IScopeRegistry));
    }
}

internal class StubMember : ITeamMember
{
    public string Key { get; init; }
    public string Name { get; init; }
    public Invitation Invitation { get; init; }
    public DateTime? LastSeen { get; init; }
    public MembershipState? State { get; init; }
    public AccessLevel AccessLevel { get; init; }
    public string[] TenantRoles { get; init; }
    public string[] ScopeOverrides { get; init; }
}

internal class StubTeamService : TeamServiceBase
{
    public StubTeamService() : base(null) { }
    protected override IAsyncEnumerable<ITeam> GetTeamsAsync(IUser user) => throw new NotImplementedException();
    protected override Task<ITeam> GetTeamAsync(string teamKey) => throw new NotImplementedException();
    protected override Task<ITeam> CreateTeamAsync(string teamKey, string name, IUser user, string displayName) => throw new NotImplementedException();
    protected override Task SetTeamNameAsync(string teamKey, string name) => throw new NotImplementedException();
    protected override Task DeleteTeamAsync(string teamKey) => throw new NotImplementedException();
    protected override Task AddTeamMemberAsync(string teamKey, InviteUserModel model) => throw new NotImplementedException();
    protected override Task RemoveTeamMemberAsync(string teamKey, string userKey) => throw new NotImplementedException();
    protected override Task<ITeam> SetTeamMemberInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept) => throw new NotImplementedException();
    protected override Task SetTeamMemberLastSeenAsync(string teamKey, string userKey) => throw new NotImplementedException();
    protected override Task<ITeamMember> GetTeamMembersAsync(string teamKey, string userKey) => throw new NotImplementedException();
    protected override Task SetTeamMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel) => throw new NotImplementedException();
    protected override Task SetTeamMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles) => throw new NotImplementedException();
    protected override Task SetTeamMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides) => throw new NotImplementedException();
    protected override Task SetTeamConsentInternalAsync(string teamKey, string[] consentedRoles) => throw new NotImplementedException();
    protected override IAsyncEnumerable<ITeam> GetConsentedTeamsInternalAsync(string[] userRoles) => throw new NotImplementedException();
}

internal class StubUserService : UserServiceBase
{
    public StubUserService() : base(null) { }
    protected override Task<IUser> GetUserAsync(System.Security.Claims.ClaimsPrincipal claimsPrincipal) => throw new NotImplementedException();
    protected override IAsyncEnumerable<IUser> GetAllAsync() => throw new NotImplementedException();
}
