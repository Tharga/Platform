using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Authorization + validation for <see cref="AuthorizationTeamServiceDecorator.SetTeamCustomRolesAsync"/>
/// (Tharga/Platform#117): requires <c>team:manage</c> on the target team, and guards against privilege
/// escalation — every role scope must be app-registered, names non-empty/unique, no code-role collision.
/// </summary>
public class AuthorizationTeamServiceDecoratorCustomRolesTests
{
    private static AuthorizationTeamServiceDecorator Build(
        ClaimsPrincipal principal, ITeamService inner, IScopeRegistry scopes = null, ITenantRoleRegistry roles = null)
    {
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));
        return new AuthorizationTeamServiceDecorator(
            inner, new TeamAuthorizer(accessor), new TeamLifecycleOptions { AllowTeamCreation = true }, scopes, roles);
    }

    private static ClaimsPrincipal Principal(string teamKey, params string[] scopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var s in scopes) claims.Add(new Claim(TeamClaimTypes.Scope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ScopeRegistry Scopes(params string[] names)
    {
        var registry = new ScopeRegistry();
        foreach (var n in names) registry.Register(n, AccessLevel.Custom);
        return registry;
    }

    private static readonly IReadOnlyList<TenantRoleDefinition> ValidRoles =
        [new TenantRoleDefinition("Registrar", ["case:read", "case:write"])];

    [Fact]
    public async Task TeamManage_ValidScopes_Delegates()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.Manage), inner, Scopes("case:read", "case:write"));

        await sut.SetTeamCustomRolesAsync("T1", ValidRoles);

        await inner.Received(1).SetTeamCustomRolesAsync("T1", ValidRoles);
    }

    [Fact]
    public async Task MemberManageOnly_Throws()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.MemberManage), inner, Scopes("case:read", "case:write"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetTeamCustomRolesAsync("T1", ValidRoles));
        await inner.DidNotReceive().SetTeamCustomRolesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<TenantRoleDefinition>>());
    }

    [Fact]
    public async Task DifferentTeam_Throws()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.Manage), inner, Scopes("case:read", "case:write"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetTeamCustomRolesAsync("T2", ValidRoles));
        await inner.DidNotReceive().SetTeamCustomRolesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<TenantRoleDefinition>>());
    }

    [Fact]
    public async Task UnregisteredScope_Throws_PrivilegeEscalationGuard()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.Manage), inner, Scopes("case:read"));

        IReadOnlyList<TenantRoleDefinition> roles = [new TenantRoleDefinition("Registrar", ["case:read", "case:delete"])];

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetTeamCustomRolesAsync("T1", roles));
        await inner.DidNotReceive().SetTeamCustomRolesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<TenantRoleDefinition>>());
    }

    [Fact]
    public async Task CollidesWithCodeRole_Throws()
    {
        var inner = Substitute.For<ITeamService>();
        var codeRoles = new TenantRoleRegistry();
        codeRoles.Register("Reader", ["case:read"]);
        var sut = Build(Principal("T1", TeamScopes.Manage), inner, Scopes("case:read"), codeRoles);

        IReadOnlyList<TenantRoleDefinition> roles = [new TenantRoleDefinition("Reader", ["case:read"])];

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetTeamCustomRolesAsync("T1", roles));
        await inner.DidNotReceive().SetTeamCustomRolesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<TenantRoleDefinition>>());
    }

    [Fact]
    public async Task DuplicateNames_Throws()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.Manage), inner, Scopes("case:read"));

        IReadOnlyList<TenantRoleDefinition> roles =
            [new TenantRoleDefinition("Dup", ["case:read"]), new TenantRoleDefinition("Dup", ["case:read"])];

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetTeamCustomRolesAsync("T1", roles));
    }

    [Fact]
    public async Task EmptyName_Throws()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.Manage), inner, Scopes("case:read"));

        IReadOnlyList<TenantRoleDefinition> roles = [new TenantRoleDefinition("  ", ["case:read"])];

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetTeamCustomRolesAsync("T1", roles));
    }

    [Fact]
    public async Task NoScopeRegistry_RejectsAnyScope()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Principal("T1", TeamScopes.Manage), inner);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetTeamCustomRolesAsync("T1", ValidRoles));
    }

    [Fact]
    public async Task GetTeamCustomRoles_PassesThrough()
    {
        var inner = Substitute.For<ITeamService>();
        inner.GetTeamCustomRolesAsync("T1").Returns(ValidRoles);
        var sut = Build(Principal("T1"), inner);

        var result = await sut.GetTeamCustomRolesAsync("T1");

        Assert.Same(ValidRoles, result);
    }
}
