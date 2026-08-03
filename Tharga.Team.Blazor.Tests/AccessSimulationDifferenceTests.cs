using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Simulation;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// What a simulation cannot show, reported before the caller draws a conclusion from it.
/// </summary>
/// <remarks>
/// The failure this prevents is subtle and points the wrong way. An administrator simulating a user who
/// holds a scope they do not see the intersection — <i>less</i> than that user really sees — and
/// concludes "they cannot do X" about something they can. For a feature whose job is setting access
/// correctly, that error leads to **granting more than necessary**.
/// </remarks>
public class AccessSimulationDifferenceTests
{
    private static ClaimsPrincipal Holding(params string[] scopes)
        => new(new ClaimsIdentity(scopes.Select(s => new Claim(TeamClaimTypes.Scope, s)), "Test"));

    private static AccessSimulation Target(AccessSimulationKind kind, params string[] scopes)
        => new() { Kind = kind, Label = "target", Scopes = scopes };

    [Fact]
    public void ScopesTheTargetHasAndTheCallerLacks_AreReported()
    {
        var gap = AccessSimulationDifference.Compare(
            Holding("orders:read"),
            Target(AccessSimulationKind.Role, "orders:read", "billing:manage"));

        Assert.Equal(["billing:manage"], gap.UnreachableScopes);
    }

    [Fact]
    public void WhenTheCallerHoldsEverythingTheTargetDoes_ThereIsNoScopeGap()
    {
        var gap = AccessSimulationDifference.Compare(
            Holding("orders:read", "orders:write", "billing:manage"),
            Target(AccessSimulationKind.Role, "orders:read", "billing:manage"));

        Assert.Empty(gap.UnreachableScopes);
    }

    /// <summary>
    /// The case the Owner/Administrator restriction is meant to make rare rather than impossible: a
    /// member's <c>ScopeOverrides</c> are unioned in without being validated against the registry, so a
    /// member can hold a scope no access level grants — including an administrator's.
    /// </summary>
    [Fact]
    public void AnUnregisteredScopeOverride_IsStillAGapAgainstAnAdministrator()
    {
        // An administrator holds every *registered* scope. This one was never registered.
        var administrator = Holding("orders:read", "orders:write", "billing:manage");

        var gap = AccessSimulationDifference.Compare(
            administrator,
            Target(AccessSimulationKind.User, "orders:read", "legacy:import"));

        Assert.Equal(["legacy:import"], gap.UnreachableScopes);
        Assert.False(gap.IsFaithful);
    }

    /// <summary>
    /// Simulating a user always carries this, because the target's system scopes come from app roles the
    /// toolkit does not store — they are unknown rather than empty.
    /// </summary>
    [Fact]
    public void SimulatingAUser_AlwaysReportsSystemAccessAsNotReproduced()
    {
        var gap = AccessSimulationDifference.Compare(
            Holding("orders:read"),
            Target(AccessSimulationKind.User, "orders:read"));

        Assert.True(gap.SystemAccessNotReproduced);
        Assert.False(gap.IsFaithful);
    }

    /// <summary>
    /// The other three kinds are team-scoped by definition, so there is nothing unknown about them and
    /// the caller should not be warned about something that cannot happen.
    /// </summary>
    [Theory]
    [InlineData(AccessSimulationKind.Role)]
    [InlineData(AccessSimulationKind.Scopes)]
    [InlineData(AccessSimulationKind.AccessLevel)]
    public void OtherKinds_DoNotClaimSystemAccessIsMissing(AccessSimulationKind kind)
    {
        var gap = AccessSimulationDifference.Compare(Holding("orders:read"), Target(kind, "orders:read"));

        Assert.False(gap.SystemAccessNotReproduced);
        Assert.True(gap.IsFaithful);
    }

    [Fact]
    public void TheReportIsDeduplicatedAndOrdered()
    {
        var gap = AccessSimulationDifference.Compare(
            Holding(),
            Target(AccessSimulationKind.Role, "z:one", "a:two", "z:one"));

        Assert.Equal(["a:two", "z:one"], gap.UnreachableScopes);
    }

    [Fact]
    public void ScopeComparisonIsCaseInsensitive()
    {
        var gap = AccessSimulationDifference.Compare(
            Holding("Orders:Read"),
            Target(AccessSimulationKind.Role, "orders:read"));

        Assert.Empty(gap.UnreachableScopes);
    }

    /// <summary>
    /// System scopes are not team scopes. A caller holding <c>audit:read</c> system-wide has not thereby
    /// got it on this team, so it must still be reported as unreachable.
    /// </summary>
    [Fact]
    public void ASystemScopeDoesNotCoverATeamScopeOfTheSameName()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(TeamClaimTypes.SystemScope, "audit:read")], "Test"));

        var gap = AccessSimulationDifference.Compare(principal, Target(AccessSimulationKind.Role, "audit:read"));

        Assert.Equal(["audit:read"], gap.UnreachableScopes);
    }

    [Fact]
    public void NoSimulation_IsFaithful()
    {
        Assert.True(AccessSimulationDifference.Compare(Holding("orders:read"), null).IsFaithful);
    }
}
