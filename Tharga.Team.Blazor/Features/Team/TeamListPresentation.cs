namespace Tharga.Team.Blazor.Features.Team;

/// <summary>How the team list is drawn.</summary>
public enum TeamListLayout
{
    /// <summary>Cards while the list is short, a grid once it is not. The default.</summary>
    Auto,

    /// <summary>Always expandable cards, however many teams there are.</summary>
    Cards,

    /// <summary>Always a sortable, filterable, paged grid.</summary>
    Grid
}

/// <summary>
/// The one question both team surfaces ask: are there few enough teams to take in at a glance?
/// </summary>
/// <remarks>
/// The selector uses it to decide whether a search box earns its place, and the team list uses it to
/// decide between cards and a grid. Those are two decisions, but they turn on the same fact about the
/// same collection, so they share a threshold rather than drifting apart at two numbers.
/// <para>
/// Pure and static, like <see cref="TeamSelectorGate"/> and <see cref="TeamVisibility"/>, so the rule can
/// be asserted directly. bUnit <i>is</i> available here — see <c>GranularPathRenderTests</c> — but
/// rendering a component to check one boolean is a slow and brittle way to ask a question that has no
/// markup in it. (Several older helpers in this project say bUnit is absent; that has not been true
/// since the granular-path render tests were added.)
/// </para>
/// </remarks>
internal static class TeamListPresentation
{
    /// <summary>
    /// The team count at and above which a list stops being scannable.
    /// </summary>
    /// <remarks>
    /// A short list is read faster than it is typed into, and reads better as cards than as rows. Around
    /// eight is where both of those flip. It is a judgement rather than a measurement, which is why every
    /// caller takes it as a parameter.
    /// </remarks>
    public const int DefaultThreshold = 8;

    /// <summary>Whether there are enough teams that scanning them stops being the quick option.</summary>
    public static bool IsMany(int teamCount, int threshold) => teamCount >= threshold;

    /// <summary>
    /// Whether to draw the list as a grid.
    /// </summary>
    /// <param name="teamCount">Teams the caller can see.</param>
    /// <param name="threshold">The count at and above which a grid is the better shape.</param>
    /// <param name="layout">The host's choice. <see cref="TeamListLayout.Auto"/> defers to the threshold.</param>
    /// <remarks>
    /// Cards suit a handful: the expand affordance is obvious and a grid of three rows looks like an
    /// administrative report of nothing much. Past the threshold that reverses — cards cannot be sorted,
    /// filtered or paged, and a page of stacked accordions is not a list.
    /// </remarks>
    public static bool ShowAsGrid(int teamCount, int threshold, TeamListLayout layout = TeamListLayout.Auto)
        => layout switch
        {
            TeamListLayout.Cards => false,
            TeamListLayout.Grid => true,
            _ => IsMany(teamCount, threshold)
        };
}
