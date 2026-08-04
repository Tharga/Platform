namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Decides what the team selector offers: a way to create a team when the caller belongs to none, and a
/// way to search when they belong to too many to scan.
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
    /// <param name="teamCount">Teams the caller belongs to.</param>
    /// <param name="allowTeamCreation">The host's <c>AllowTeamCreation</c> option.</param>
    /// <param name="showLink">
    /// The selector's own <c>ShowCreateTeamLink</c> parameter — <b>presentation only</b>. It hides the
    /// link in the top bar without saying anything about whether teams may be created, which is what
    /// <paramref name="allowTeamCreation"/> governs and what the service enforces.
    /// </param>
    /// <remarks>
    /// Three inputs, and only one of them is a permission. A host that wants creation reachable from the
    /// team page but not offered in the top bar is making a layout decision, not an authorization one —
    /// so it gets its own switch rather than being folded into the option that the service also reads.
    /// </remarks>
    public static bool ShowCreateTeamLink(int teamCount, bool allowTeamCreation, bool showLink = true)
        => teamCount == 0 && allowTeamCreation && showLink;

    /// <summary>
    /// The team count at and above which the selector offers a search box.
    /// </summary>
    /// <remarks>
    /// Shared with the team list, which uses the same number to decide between cards and a grid. Two
    /// decisions, but both turn on the same fact about the same collection, so they move together.
    /// </remarks>
    public const int DefaultFilterThreshold = TeamListPresentation.DefaultThreshold;

    /// <summary>
    /// Whether the selector offers a search box.
    /// </summary>
    /// <param name="teamCount">Teams the caller can choose between.</param>
    /// <param name="threshold">The count at and above which a filter is worth showing.</param>
    /// <param name="allowFiltering">
    /// A host's explicit answer, which wins outright. Null defers to <paramref name="threshold"/>.
    /// </param>
    /// <remarks>
    /// The same judgement <see cref="Audit.AuditFilterVisibility"/> makes about the audit filter bar —
    /// *"one option is not a filter"* — applied to a different control. Kept here rather than inline in
    /// markup for the reason every decision in this feature is: the project has no bUnit, so a rule left
    /// in a razor file cannot be tested at all.
    /// <para>
    /// A host that forces it on for a caller with one team gets a filter over one team. That is their
    /// call to make and not worth second-guessing — <c>false</c> and <c>true</c> both mean "I have
    /// decided", and the threshold exists precisely for everyone who has not.
    /// </para>
    /// </remarks>
    public static bool ShowFilter(int teamCount, int threshold, bool? allowFiltering = null)
        => allowFiltering ?? TeamListPresentation.IsMany(teamCount, threshold);
}
