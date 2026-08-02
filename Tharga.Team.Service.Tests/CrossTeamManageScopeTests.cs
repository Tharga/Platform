using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// <see cref="SystemTeamScopes.Manage"/> — the oversight equivalent of in-team <c>team:manage</c>, for
/// the two operations that are presentational.
/// </summary>
/// <remarks>
/// <b>The boundary is the point of these tests, not the happy path.</b> In-team <c>team:manage</c> covers
/// rename, icon, consent and custom roles alike. The system grant covers only the first two. Consent is a
/// team's own statement about what it exposes inbound and custom roles decide what a member may do — both
/// authorization; rename and icon change how a team looks.
/// <para>
/// Nothing in the type system expresses "these two members of that scope but not those two", so the
/// erosion this guards against would be a one-line change that reads like consistency. Hence a test.
/// </para>
/// </remarks>
public class CrossTeamManageScopeTests
{
    private static AuthorizationTeamServiceDecorator Build(ClaimsPrincipal principal, ITeamService inner)
    {
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));
        return new AuthorizationTeamServiceDecorator(
            inner, new TeamAuthorizer(accessor), new TeamLifecycleOptions { AllowTeamCreation = true });
    }

    /// <summary>An oversight caller: the system grant, and no membership of the team being acted on.</summary>
    private static ClaimsPrincipal Oversight()
        => new(new ClaimsIdentity([new Claim(TeamClaimTypes.SystemScope, SystemTeamScopes.Manage)], "Test"));

    private static ClaimsPrincipal Member(string teamKey)
        => new(new ClaimsIdentity(
            [new Claim(TeamClaimTypes.TeamKey, teamKey), new Claim(TeamClaimTypes.Scope, TeamScopes.Manage)], "Test"));

    private static ClaimsPrincipal Nobody() => new(new ClaimsIdentity([], "Test"));

    [Fact]
    public async Task Oversight_CanRenameATeamItDoesNotBelongTo()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Oversight(), inner);

        await sut.RenameTeamAsync<ITeamMember>("other-team", "New name");

        await inner.Received(1).RenameTeamAsync<ITeamMember>("other-team", "New name");
    }

    [Fact]
    public async Task Oversight_CanSetAndClearTheIcon()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Oversight(), inner);

        await sut.SetTeamIconAsync("other-team", [1], "image/png");
        await sut.ClearTeamIconAsync("other-team");

        await inner.Received(1).SetTeamIconAsync("other-team", Arg.Any<byte[]>(), "image/png");
        await inner.Received(1).ClearTeamIconAsync("other-team");
    }

    /// <summary>
    /// The boundary. Consent is authorization, not presentation — an operator overriding what a team
    /// exposes inbound is a far larger claim than fixing a typo in its name.
    /// </summary>
    [Fact]
    public async Task Oversight_CannotChangeConsent()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Oversight(), inner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.SetTeamConsentAsync("other-team", ["Developer"], AccessLevel.Administrator));

        await inner.DidNotReceiveWithAnyArgs().SetTeamConsentAsync(default, default, default);
    }

    /// <summary>Custom roles decide what a member may do, so they stay on the in-team scope too.</summary>
    [Fact]
    public async Task Oversight_CannotChangeCustomRoles()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Oversight(), inner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.SetTeamCustomRolesAsync("other-team", [new TenantRoleDefinition("R", [])]));

        await inner.DidNotReceiveWithAnyArgs().SetTeamCustomRolesAsync(default, default);
    }

    /// <summary>The in-team route is unchanged — this scope is additive, not a replacement.</summary>
    [Fact]
    public async Task InTeamManage_StillRenamesItsOwnTeam()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Member("T1"), inner);

        await sut.RenameTeamAsync<ITeamMember>("T1", "New name");

        await inner.Received(1).RenameTeamAsync<ITeamMember>("T1", "New name");
    }

    /// <summary>In-team scope is still `TeamKey`-bound: holding it for one team authorizes only that one.</summary>
    [Fact]
    public async Task InTeamManage_StillCannotReachAnotherTeam()
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Member("T1"), inner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.RenameTeamAsync<ITeamMember>("T2", "New name"));
    }

    [Theory]
    [InlineData("rename")]
    [InlineData("set-icon")]
    [InlineData("clear-icon")]
    public async Task NeitherScope_IsRefused(string operation)
    {
        var inner = Substitute.For<ITeamService>();
        var sut = Build(Nobody(), inner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            switch (operation)
            {
                case "rename": await sut.RenameTeamAsync<ITeamMember>("T1", "n"); break;
                case "set-icon": await sut.SetTeamIconAsync("T1", [1], "image/png"); break;
                default: await sut.ClearTeamIconAsync("T1"); break;
            }
        });
    }

    /// <summary>
    /// A <i>team</i> grant of the same name must not satisfy the system check — the claim types carry
    /// their provenance precisely so an in-team scope cannot be spent cross-team.
    /// </summary>
    [Fact]
    public async Task ATeamGrantNamedTeamsManage_DoesNotAuthorizeAnotherTeam()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(TeamClaimTypes.TeamKey, "T1"), new Claim(TeamClaimTypes.Scope, SystemTeamScopes.Manage)], "Test"));
        var inner = Substitute.For<ITeamService>();
        var sut = Build(principal, inner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.RenameTeamAsync<ITeamMember>("T2", "New name"));
    }
}
