using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Simulation;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The security core: a simulation can only ever take access away.
/// </summary>
public class AccessSimulationFilterTests
{
    private static ClaimsPrincipal Principal(
        string[] scopes = null,
        string[] systemScopes = null,
        string[] appRoles = null,
        AccessLevel? accessLevel = null,
        bool member = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "alice-subject"),
            new(ClaimTypes.Name, "Alice"),
            new(TeamClaimTypes.TeamKey, "team-1"),
            new(TeamClaimTypes.MemberKey, "member-1")
        };

        if (member) claims.Add(new Claim(ClaimTypes.Role, Roles.TeamMember));
        if (accessLevel != null)
        {
            claims.Add(new Claim(TeamClaimTypes.AccessLevel, accessLevel.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, $"Team{accessLevel.Value}"));
        }

        foreach (var s in scopes ?? []) claims.Add(new Claim(TeamClaimTypes.Scope, s));
        foreach (var s in systemScopes ?? []) claims.Add(new Claim(TeamClaimTypes.SystemScope, s));
        foreach (var r in appRoles ?? []) claims.Add(new Claim(ClaimTypes.Role, r));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static string[] Scopes(ClaimsPrincipal p) => p.FindAll(TeamClaimTypes.Scope).Select(c => c.Value).ToArray();
    private static string[] SystemScopes(ClaimsPrincipal p) => p.FindAll(TeamClaimTypes.SystemScope).Select(c => c.Value).ToArray();
    private static string[] RoleClaims(ClaimsPrincipal p) => p.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

    private static AccessSimulation Simulation(params string[] scopes)
        => new() { Kind = AccessSimulationKind.Scopes, Label = "test", Scopes = scopes };

    // --- the central claim ---

    [Fact]
    public void ScopesOutsideTheTargetAreRemoved()
    {
        var principal = Principal(scopes: ["orders:read", "orders:write", "billing:manage"]);

        AccessSimulationFilter.Apply(principal, Simulation("orders:read"));

        Assert.Equal(["orders:read"], Scopes(principal));
    }

    /// <summary>
    /// The whole security argument. A simulation naming scopes the caller does not hold gains them
    /// nothing — which is why the cookie carrying it does not need to be trusted.
    /// </summary>
    [Fact]
    public void AForgedSimulationNamingScopesTheCallerLacks_GrantsNothing()
    {
        var principal = Principal(scopes: ["orders:read"]);

        AccessSimulationFilter.Apply(principal, Simulation("orders:read", "billing:manage", "firewall:open"));

        Assert.Equal(["orders:read"], Scopes(principal));
    }

    /// <summary>
    /// A simulation created before the caller's access changed cannot resurrect what they have since
    /// lost — the kept set is filtered against what is on the principal now, not against what was there
    /// when the simulation was made.
    /// </summary>
    [Fact]
    public void AStaleSimulation_CannotElevate()
    {
        var principal = Principal(scopes: ["orders:read"]);

        AccessSimulationFilter.Apply(principal, Simulation("orders:read", "orders:refund"));

        Assert.DoesNotContain("orders:refund", Scopes(principal));
    }

    /// <summary>
    /// Applying a role replaces the effective set rather than adding to it — the user's own framing, and
    /// the case that would look like a bug if it were additive.
    /// </summary>
    [Fact]
    public void ApplyingARole_ReplacesRatherThanAdds()
    {
        var principal = Principal(scopes: ["orders:read", "orders:write", "billing:manage"]);

        // "Support" grants only these two.
        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.Role,
            Label = "Support",
            Scopes = ["orders:read", "valuegroup:read"]
        });

        Assert.Equal(["orders:read"], Scopes(principal));
    }

    [Fact]
    public void AnEmptyTarget_RemovesEveryScope()
    {
        var principal = Principal(scopes: ["orders:read", "orders:write"]);

        AccessSimulationFilter.Apply(principal, Simulation());

        Assert.Empty(Scopes(principal));
    }

    [Fact]
    public void ANullSimulation_ChangesNothing()
    {
        var principal = Principal(scopes: ["orders:read"], accessLevel: AccessLevel.Owner);

        AccessSimulationFilter.Apply(principal, null);

        Assert.Equal(["orders:read"], Scopes(principal));
        Assert.Equal("Owner", principal.FindFirst(TeamClaimTypes.AccessLevel)?.Value);
    }

    // --- system scopes and app roles ---

    /// <summary>
    /// Another user's system access cannot be computed, so the caller's own is dropped rather than shown
    /// as if it were theirs.
    /// </summary>
    [Fact]
    public void SimulatingAUser_DropsSystemScopesAndAppRoles()
    {
        var principal = Principal(
            scopes: ["orders:read"],
            systemScopes: ["audit:read", "teams:read"],
            appRoles: ["Developer"]);

        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.User,
            Label = "Bob",
            Scopes = ["orders:read"],
            DropSystemScopes = true,
            DropAppRoles = true
        });

        Assert.Empty(SystemScopes(principal));
        Assert.DoesNotContain("Developer", RoleClaims(principal));
    }

    /// <summary>Team-derived roles are not app roles and survive — the caller is still a member.</summary>
    [Fact]
    public void DroppingAppRoles_KeepsTheTeamRoles()
    {
        var principal = Principal(appRoles: ["Developer"], accessLevel: AccessLevel.Administrator);

        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.User, Label = "Bob", DropAppRoles = true
        });

        Assert.Contains(Roles.TeamMember, RoleClaims(principal));
        Assert.Contains("TeamAdministrator", RoleClaims(principal));
    }

    [Fact]
    public void SimulatingARole_LeavesSystemScopesAlone()
    {
        var principal = Principal(scopes: ["orders:read"], systemScopes: ["audit:read"]);

        AccessSimulationFilter.Apply(principal, Simulation("orders:read"));

        Assert.Equal(["audit:read"], SystemScopes(principal));
    }

    // --- access level: the clamped replacement ---

    [Fact]
    public void ALowerAccessLevel_IsApplied()
    {
        var principal = Principal(accessLevel: AccessLevel.Owner);

        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.AccessLevel, Label = "Viewer", AccessLevel = AccessLevel.Viewer
        });

        Assert.Equal("Viewer", principal.FindFirst(TeamClaimTypes.AccessLevel)?.Value);
        Assert.Contains("TeamViewer", RoleClaims(principal));
        Assert.DoesNotContain("TeamOwner", RoleClaims(principal));
    }

    /// <summary>
    /// The escalation attempt, and the reason the ordering has its own type: the enum runs
    /// <c>Owner=0 … Viewer=3</c>, so a naive <c>Math.Min</c> would have let this through.
    /// </summary>
    [Fact]
    public void AHigherAccessLevel_IsRefused()
    {
        var principal = Principal(accessLevel: AccessLevel.Viewer);

        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.AccessLevel, Label = "Owner", AccessLevel = AccessLevel.Owner
        });

        Assert.Equal("Viewer", principal.FindFirst(TeamClaimTypes.AccessLevel)?.Value);
        Assert.DoesNotContain("TeamOwner", RoleClaims(principal));
    }

    /// <summary>The claim and its role move together, so nothing can read one and disagree with the other.</summary>
    [Fact]
    public void TheAccessLevelClaimAndItsRole_NeverDisagree()
    {
        var principal = Principal(accessLevel: AccessLevel.Owner);

        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.AccessLevel, Label = "User", AccessLevel = AccessLevel.User
        });

        var level = principal.FindFirst(TeamClaimTypes.AccessLevel)?.Value;
        var levelRoles = RoleClaims(principal).Where(r => r.StartsWith("Team") && r != Roles.TeamMember).ToArray();

        Assert.Equal("User", level);
        Assert.Equal(["TeamUser"], levelRoles);
    }

    [Fact]
    public void NoRequestedAccessLevel_LeavesItAlone()
    {
        var principal = Principal(accessLevel: AccessLevel.Administrator);

        AccessSimulationFilter.Apply(principal, Simulation());

        Assert.Equal("Administrator", principal.FindFirst(TeamClaimTypes.AccessLevel)?.Value);
    }

    // --- what must never be touched ---

    /// <summary>
    /// The audit actor stays the real caller by construction. If identity claims were filterable, an
    /// action taken under simulation could be attributed to nobody.
    /// </summary>
    [Fact]
    public void IdentityClaimsAreNeverRemoved()
    {
        var principal = Principal(scopes: ["orders:read"], accessLevel: AccessLevel.Owner);

        AccessSimulationFilter.Apply(principal, new AccessSimulation
        {
            Kind = AccessSimulationKind.User,
            Label = "Bob",
            AccessLevel = AccessLevel.Viewer,
            DropSystemScopes = true,
            DropAppRoles = true
        });

        Assert.Equal("alice-subject", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Alice", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("team-1", principal.FindFirst(TeamClaimTypes.TeamKey)?.Value);
        Assert.Equal("member-1", principal.FindFirst(TeamClaimTypes.MemberKey)?.Value);
    }

    /// <summary>
    /// Authorization reads the union across identities, so filtering only the primary one would leave a
    /// scope honoured that the simulation reported removing.
    /// </summary>
    [Fact]
    public void ScopesOnASecondIdentityAreAlsoRemoved()
    {
        var principal = Principal(scopes: ["orders:read"]);
        principal.AddIdentity(new ClaimsIdentity(
            [new Claim(TeamClaimTypes.Scope, "billing:manage")], "Secondary"));

        Assert.Contains("billing:manage", Scopes(principal));

        AccessSimulationFilter.Apply(principal, Simulation("orders:read"));

        Assert.Equal(["orders:read"], Scopes(principal));
    }

    // --- no elevation, stated as a property over many shapes ---

    [Theory]
    [InlineData(new[] { "a" }, new[] { "a", "b", "c" })]
    [InlineData(new[] { "a", "b" }, new[] { "c" })]
    [InlineData(new string[0], new[] { "a" })]
    [InlineData(new[] { "a", "b", "c" }, new[] { "a", "b", "c" })]
    public void TheEffectiveSetIsAlwaysASubsetOfTheRealSet(string[] held, string[] simulated)
    {
        var principal = Principal(scopes: held);

        AccessSimulationFilter.Apply(principal, Simulation(simulated));

        Assert.Subset(new HashSet<string>(held), new HashSet<string>(Scopes(principal)));
    }
}
