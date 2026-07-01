namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Splits and merges tenant-role selections around the set of roles currently visible for a team,
/// so roles assigned to a member but hidden for that team are preserved rather than pruned when the
/// operator edits the visible ones.
/// </summary>
internal static class RoleSelectionResolver
{
    /// <summary>
    /// Partitions <paramref name="selected"/> into the roles that are visible (shown and editable)
    /// and those that are hidden for the team (kept but not shown).
    /// </summary>
    internal static (IReadOnlyList<string> Visible, IReadOnlyList<string> Hidden) Split(
        IEnumerable<string> visibleRoleNames, IEnumerable<string> selected)
    {
        var visibleSet = new HashSet<string>(visibleRoleNames ?? Enumerable.Empty<string>());
        var assigned = (selected ?? Enumerable.Empty<string>()).ToList();

        var visible = assigned.Where(visibleSet.Contains).ToList();
        var hidden = assigned.Where(s => !visibleSet.Contains(s)).ToList();
        return (visible, hidden);
    }

    /// <summary>
    /// Combines the operator's new visible selection with the previously-hidden assignments,
    /// preserving hidden roles without duplicating any.
    /// </summary>
    internal static string[] Merge(IEnumerable<string> visibleSelection, IEnumerable<string> hiddenSelected)
        => (visibleSelection ?? Enumerable.Empty<string>())
            .Concat(hiddenSelected ?? Enumerable.Empty<string>())
            .Distinct()
            .ToArray();
}
