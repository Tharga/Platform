using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.Service;

namespace Tharga.Team.Blazor.Tests;

public class BlazorTeamPrincipalAccessorTests
{
    private static ClaimsPrincipal Principal(string name)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "Test"));

    private sealed class StubAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    [Fact]
    public async Task Uses_HttpContext_User_When_Present()
    {
        var httpUser = Principal("http");
        var accessor = Mock.Of<IHttpContextAccessor>(a => a.HttpContext == new DefaultHttpContext { User = httpUser });
        var sut = new BlazorTeamPrincipalAccessor(accessor, new StubAuthStateProvider(Principal("circuit")));

        Assert.Same(httpUser, await sut.GetCurrentAsync());
    }

    [Fact]
    public async Task Falls_Back_To_AuthenticationStateProvider_In_Circuit()
    {
        var circuitUser = Principal("circuit");
        var accessor = Mock.Of<IHttpContextAccessor>(); // interactive circuit — HttpContext defaults to null
        var sut = new BlazorTeamPrincipalAccessor(accessor, new StubAuthStateProvider(circuitUser));

        Assert.Same(circuitUser, await sut.GetCurrentAsync());
    }

    /// <summary>
    /// The third world the accessor actually runs in: neither an HTTP request nor a circuit — an MCP
    /// request handler, a hosted service, a message handler. The circuit fallback calls an API that only
    /// works inside a Razor component's DI scope, so today every MCP resource read fails with
    /// <c>"Do not call GetAuthenticationStateAsync outside of the DI scope for a Razor component."</c>
    /// </summary>
    [Fact]
    public async Task No_HttpContext_And_No_Circuit_Does_Not_Throw()
    {
        var accessor = Mock.Of<IHttpContextAccessor>(); // null HttpContext
        var sut = new BlazorTeamPrincipalAccessor(accessor, new ServerAuthenticationStateProvider());

        var principal = await sut.GetCurrentAsync();

        Assert.True(principal is null || principal.Identity?.IsAuthenticated != true,
            "with neither an HTTP request nor a circuit there is no caller, so authorization must refuse rather than permit");
    }

    /// <summary>
    /// The guard against the fix becoming a silent swallow. A provider that fails for a real reason must
    /// still surface — otherwise a broken circuit is indistinguishable from "no caller", and authorization
    /// quietly refuses instead of the bug being seen.
    /// </summary>
    [Fact]
    public async Task A_Provider_Failing_For_A_Real_Reason_Still_Throws()
    {
        var accessor = Mock.Of<IHttpContextAccessor>();
        var sut = new BlazorTeamPrincipalAccessor(accessor, new ThrowingAuthStateProvider());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.GetCurrentAsync());
        Assert.Contains("worth seeing", ex.Message);
    }

    /// <summary>
    /// The narrower half of the same guard: a provider that <i>is</i> a circuit provider, failing for a
    /// reason other than "not in a circuit", must still surface. Only the specific outside-DI-scope
    /// condition is treated as "no caller".
    /// </summary>
    [Fact]
    public async Task A_Circuit_Provider_Failing_Differently_Still_Throws()
    {
        var accessor = Mock.Of<IHttpContextAccessor>();
        var sut = new BlazorTeamPrincipalAccessor(accessor, new BrokenServerProvider());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.GetCurrentAsync());
        Assert.Contains("database", ex.Message);
    }

    /// <summary>Derives from the framework's circuit provider, so a type check alone would not save us.</summary>
    private sealed class BrokenServerProvider : ServerAuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => throw new InvalidOperationException("The database is unreachable.");
    }

    private sealed class ThrowingAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => throw new InvalidOperationException("The circuit is broken for a reason worth seeing.");
    }

    [Fact]
    public void AddThargaTeamBlazor_Registers_BlazorTeamPrincipalAccessor()
    {
        var services = new ServiceCollection();
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<FakeTeamService, FakeUserService>());

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ITeamPrincipalAccessor));
        Assert.Equal(typeof(BlazorTeamPrincipalAccessor), descriptor.ImplementationType);
    }

    private sealed class FakeUserService(AuthenticationStateProvider asp) : UserServiceBase(asp)
    {
        protected override Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal) => Task.FromResult<IUser>(null);
        protected override async IAsyncEnumerable<IUser> GetAllAsync() { yield break; }
    }

    private sealed class FakeTeamService(IUserService userService) : TeamServiceBase(userService)
    {
        protected override Task SetTeamConsentInternalAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel) => Task.CompletedTask;
        protected override async IAsyncEnumerable<ITeam> GetConsentedTeamsInternalAsync(string[] userRoles) { yield break; }
        protected override Task SetTeamCustomRolesInternalAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles) => Task.CompletedTask;
        protected override IAsyncEnumerable<ITeam> GetTeamsAsync(IUser user) => throw new NotImplementedException();
        protected override Task<ITeam> GetTeamAsync(string teamKey) => throw new NotImplementedException();
        protected override Task<ITeam> CreateTeamAsync(string teamKey, string name, IUser user, string displayName = null) => throw new NotImplementedException();
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
        protected override Task SetTeamMemberNameAsync(string teamKey, string userKey, string name) => throw new NotImplementedException();
    }
}
