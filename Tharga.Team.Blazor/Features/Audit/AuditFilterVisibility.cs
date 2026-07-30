namespace Tharga.Team.Blazor.Features.Audit;

/// <summary>
/// Whether a top-bar filter on <c>AuditLogView</c> is worth rendering.
/// </summary>
/// <remarks>
/// Two reasons to hide one, and they are the same reason twice. A dimension the caller pinned is not the
/// reader's to change. A dimension with a single available option cannot change the result either — it
/// reads as a control and behaves as a label. Since filter options are drawn from inside the pinned
/// scope, the second case subsumes the first for most dimensions: a system API key has no team, and a
/// team key has exactly one, so the Team filter disappears without either being special-cased.
/// </remarks>
internal static class AuditFilterVisibility
{
    /// <summary>
    /// Whether to show a filter offering <paramref name="optionCount"/> distinct values.
    /// </summary>
    /// <param name="optionCount">Distinct options available within the current scope.</param>
    /// <param name="isPinned">Whether the caller fixed this dimension via <see cref="AuditPinnedFilter"/>.</param>
    public static bool ShouldShow(int optionCount, bool isPinned)
        => !isPinned && optionCount > 1;
}
