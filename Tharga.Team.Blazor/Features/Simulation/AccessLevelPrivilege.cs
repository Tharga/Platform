using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// Compares two access levels by privilege, which is not the same as comparing them by value.
/// </summary>
/// <remarks>
/// <b>The enum is ordered by privilege descending:</b> <c>Owner=0, Administrator=1, User=2, Viewer=3</c>.
/// So <i>less</i> privilege is a <i>larger</i> ordinal, and the instinctive <c>Math.Min</c> guard against
/// escalation does exactly the opposite of what it looks like it does.
/// <para>
/// <b><see cref="AccessLevel.Custom"/> is not a rank.</b> It sits at ordinal 4 but means "no base scopes
/// at all" — <c>ScopeRegistry.GetScopesForAccessLevel</c> returns empty for it and it is exempt from the
/// Owner/Administrator all-scopes rule. Treating it as rank 4 happens to give the right answer for the
/// floor and the wrong reason for it, so it is handled explicitly.
/// </para>
/// </remarks>
internal static class AccessLevelPrivilege
{
    /// <summary>
    /// Whether <paramref name="candidate"/> grants no more privilege than <paramref name="actual"/>.
    /// </summary>
    public static bool IsNoMorePrivilegedThan(AccessLevel candidate, AccessLevel actual)
    {
        if (candidate == actual) return true;

        // The floor: it grants nothing on its own, so it is never an escalation from anything.
        if (candidate == AccessLevel.Custom) return true;

        // Nothing else is a de-escalation *from* the floor — Custom grants no base scopes, so any ranked
        // level is more privileged than it.
        if (actual == AccessLevel.Custom) return false;

        return (int)candidate > (int)actual;
    }

    /// <summary>
    /// The level a simulation may present: <paramref name="candidate"/> when it is a de-escalation, and
    /// otherwise <paramref name="actual"/> unchanged.
    /// </summary>
    /// <remarks>
    /// Falls back rather than throwing. A simulation that cannot be honoured should leave the caller at
    /// their real level and let the difference report say so — throwing would turn a stale cookie into a
    /// broken session.
    /// </remarks>
    public static AccessLevel Clamp(AccessLevel candidate, AccessLevel actual)
        => IsNoMorePrivilegedThan(candidate, actual) ? candidate : actual;
}
