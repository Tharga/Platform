namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Decides what the team selector offers a caller who belongs to no team.
/// </summary>
/// <remarks>
/// Pure and static so it is unit-testable — the project has no bUnit, so a decision left in razor markup
/// is unreachable from tests. Mirrors <c>TeamActionGate</c> / <c>TeamVisibility</c> / <c>MemberHighlight</c>.
/// </remarks>
internal static class TeamSelectorGate
{
    /// <summary>
    /// Whether the teamless branch offers a "Create team" affordance.
    /// </summary>
    /// <remarks>
    /// <c>AllowTeamCreation</c> is documented as hiding the Create and Delete buttons, and
    /// <c>CreateTeamPath</c> names the selector's teamless link as one of the two built-in entry points —
    /// so the option unambiguously governs this link. It was read by <c>TeamComponent</c> only, which left
    /// the two surfaces contradicting each other: the selector offered creation that the service layer
    /// then refused, because creating a team requires <c>AllowTeamCreation</c> at the service since 3.1.2.
    /// <para>
    /// Applies to <b>both</b> link variants — the host-callback branch and the plain navigation branch.
    /// Gating only one would leave the defect in place for whichever hosts use the other.
    /// </para>
    /// </remarks>
    public static bool ShowCreateTeamLink(int teamCount, bool allowTeamCreation)
        => teamCount == 0 && allowTeamCreation;
}
