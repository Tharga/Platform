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
    public static bool ShowCreateTeamLink(int teamCount, bool allowTeamCreation)
        => teamCount == 0 && allowTeamCreation;

    /// <summary>
    /// The team count at and above which the selector offers a search box.
    /// </summary>
    /// <remarks>
    /// A short list is read faster than it is typed into, so a filter below this is a control that costs
    /// attention and saves none. Around eight is where scanning stops being the quicker option; it is a
    /// judgement rather than a measurement, which is why <c>FilterThreshold</c> exists to move it.
    /// </remarks>
    public const int DefaultFilterThreshold = 8;

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
        => allowFiltering ?? teamCount >= threshold;
}
