using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Team-aware role resolution (Tharga/Platform#117): <see cref="TenantRoleService"/> merges code roles
/// with a team's custom roles and unions access-level ∪ code-role ∪ custom-role ∪ override scopes.
/// </summary>
public class TenantRoleServiceTests
{
    private const string TeamKey = "T1";

    private static (TenantRoleService sut, ITeamService team) Build(
        IReadOnlyList<TenantRoleDefinition> teamCustomRoles,
        Action<ScopeRegistry> configureScopes = null,
        Action<TenantRoleRegistry> configureCodeRoles = null)
    {
        var scopes = new ScopeRegistry();
        configureScopes?.Invoke(scopes);

        var codeRoles = new TenantRoleRegistry();
        configureCodeRoles?.Invoke(codeRoles);
        scopes.SetRoleRegistry(codeRoles);

        var team = Substitute.For<ITeamService>();
        team.GetTeamCustomRolesAsync(TeamKey).Returns(teamCustomRoles ?? []);

        return (new TenantRoleService(team, scopes, codeRoles), team);
    }

    [Fact]
    public async Task GetRolesAsync_merges_code_and_custom_roles()
    {
        var (sut, _) = Build(
            teamCustomRoles: [new TenantRoleDefinition("Registrar", ["case:read"])],
            configureCodeRoles: r => r.Register("Developer", "sys:admin"));

        var roles = await sut.GetRolesAsync(TeamKey);

        Assert.Contains(roles, r => r.Name == "Developer");
        Assert.Contains(roles, r => r.Name == "Registrar");
    }

    [Fact]
    public async Task GetRolesAsync_code_role_wins_on_name_clash()
    {
        var (sut, _) = Build(
            teamCustomRoles: [new TenantRoleDefinition("Developer", ["case:read"])],
            configureCodeRoles: r => r.Register("Developer", "sys:admin"));

        var roles = await sut.GetRolesAsync(TeamKey);

        var developer = Assert.Single(roles, r => r.Name == "Developer");
        Assert.Equal(["sys:admin"], developer.Scopes);
    }

    [Fact]
    public async Task GetEffectiveScopesAsync_includes_custom_role_scopes_for_held_roles()
    {
        var (sut, _) = Build(
            teamCustomRoles: [new TenantRoleDefinition("Registrar", ["case:read", "case:write"])],
            configureScopes: s =>
            {
                s.Register("case:read", AccessLevel.Custom);
                s.Register("case:write", AccessLevel.Custom);
            });

        var scopes = await sut.GetEffectiveScopesAsync(TeamKey, AccessLevel.Custom, ["Registrar"], null);

        Assert.Contains("case:read", scopes);
        Assert.Contains("case:write", scopes);
    }

    [Fact]
    public async Task GetEffectiveScopesAsync_excludes_custom_roles_the_member_does_not_hold()
    {
        var (sut, _) = Build(
            teamCustomRoles:
            [
                new TenantRoleDefinition("Registrar", ["case:write"]),
                new TenantRoleDefinition("Reader", ["case:read"])
            ],
            configureScopes: s =>
            {
                s.Register("case:read", AccessLevel.Custom);
                s.Register("case:write", AccessLevel.Custom);
            });

        var scopes = await sut.GetEffectiveScopesAsync(TeamKey, AccessLevel.Custom, ["Reader"], null);

        Assert.Contains("case:read", scopes);
        Assert.DoesNotContain("case:write", scopes);
    }

    [Fact]
    public async Task GetEffectiveScopesAsync_unions_access_level_code_role_and_override_scopes()
    {
        var (sut, _) = Build(
            teamCustomRoles: [new TenantRoleDefinition("Registrar", ["case:write"])],
            configureScopes: s =>
            {
                s.Register("team:read", AccessLevel.Viewer);
                s.Register("case:write", AccessLevel.Custom);
                s.Register("feature:x", AccessLevel.Custom);
            },
            configureCodeRoles: r => r.Register("Developer", "team:read"));

        var scopes = await sut.GetEffectiveScopesAsync(
            TeamKey, AccessLevel.Custom, ["Developer", "Registrar"], ["feature:x"]);

        Assert.Contains("team:read", scopes);   // code role
        Assert.Contains("case:write", scopes);  // custom role
        Assert.Contains("feature:x", scopes);   // override
    }

    [Fact]
    public async Task GetEffectiveScopesAsync_no_custom_roles_matches_base_resolution()
    {
        var (sut, _) = Build(
            teamCustomRoles: [],
            configureScopes: s => s.Register("team:read", AccessLevel.Viewer));

        var scopes = await sut.GetEffectiveScopesAsync(TeamKey, AccessLevel.Viewer, [], null);

        Assert.Equal(["team:read"], scopes);
    }

    // End-to-end through the REAL composition (no mocked store or resolver): define a custom role on a team
    // via the service write path, then confirm a member holding that role resolves to its scopes.
    [Fact]
    public async Task EndToEnd_DefineCustomRole_thenResolveScopesForAssignedMember()
    {
        var userService = Substitute.For<IUserService>();
        var teamService = new TestTeamService(userService);
        teamService.AddTeam(TeamKey, "Team One",
            new TestMember { Key = "u1", AccessLevel = AccessLevel.Custom, TenantRoles = ["Registrar"], State = MembershipState.Member });

        await teamService.SetTeamCustomRolesAsync(TeamKey,
            [new TenantRoleDefinition("Registrar", ["case:read", "case:write"])]);

        var scopes = new ScopeRegistry();
        scopes.Register("case:read", AccessLevel.Custom);
        scopes.Register("case:write", AccessLevel.Custom);

        var resolver = new TenantRoleService(teamService, scopes);

        var effective = await resolver.GetEffectiveScopesAsync(TeamKey, AccessLevel.Custom, ["Registrar"], null);

        Assert.Contains("case:read", effective);
        Assert.Contains("case:write", effective);
    }
}
