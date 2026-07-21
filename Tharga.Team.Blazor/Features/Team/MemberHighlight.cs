namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Decides whether a team member row represents the signed-in user, so <c>TeamComponent</c> can single
/// it out in the member grid.
/// </summary>
/// <remarks>
/// Pure and static so it is unit-testable — the project has no bUnit, so a decision left in razor markup
/// is unreachable from tests. Mirrors <c>TeamActionGate</c> / <c>TeamVisibility</c>.
/// </remarks>
internal static class MemberHighlight
{
    /// <summary>
    /// True when <paramref name="memberKey"/> is the current user's key. A null or empty key on either
    /// side never matches — invited members can carry a null key, and two of those must not both read as
    /// "you".
    /// </summary>
    public static bool IsCurrentMember(string memberKey, string userKey)
    {
        if (string.IsNullOrEmpty(memberKey) || string.IsNullOrEmpty(userKey)) return false;
        return string.Equals(memberKey, userKey, StringComparison.Ordinal);
    }
}
