namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// How an edited member name on the team surface becomes a stored per-team override.
/// </summary>
/// <remarks>
/// A member row shows the user's root name when the team holds no override, so submitting that
/// displayed value unchanged must not silently create an override that pins the name — it would then
/// stop tracking the user's own later renames. Persisting null in that case keeps the row a
/// pass-through. This surface never writes the root <c>IUser.Name</c>; that belongs to the user's own
/// profile page and to user administration.
/// </remarks>
internal static class MemberNamePolicy
{
    /// <summary>
    /// The override to persist for <paramref name="input"/>, given the name the row would display
    /// with no override. Null means "no override" — blank input, or input equal to the default.
    /// </summary>
    public static string ResolveOverride(string input, string resolvedDefaultName)
    {
        var trimmed = string.IsNullOrWhiteSpace(input) ? null : input.Trim();
        return trimmed == resolvedDefaultName ? null : trimmed;
    }
}
