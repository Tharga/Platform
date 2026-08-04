namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Turns a host's configured invite route into the path segment appended to the base URI.
/// </summary>
/// <remarks>
/// Its own type because the string handling has more edge cases than it looks — a host will write
/// <c>/invitation</c>, <c>invitation</c> or <c>/invitation/</c> and mean the same thing, and the base URI
/// already ends in a slash, so a leading one produces <c>https://host//invitation</c>. Getting that wrong
/// breaks the link silently, which is the failure mode Tharga/Team#191 was reported for in the first
/// place.
/// <para>
/// Pure and static so the path handling can be asserted directly, mirroring <see cref="TeamSelectorGate"/>
/// and <see cref="TeamVisibility"/>. bUnit is available for cases that need a render; a string rule is
/// not one.
/// </para>
/// </remarks>
internal static class InvitePathResolver
{
    /// <summary>
    /// Where invitation links pointed before <c>InvitePath</c> existed, and where they still point when
    /// it is not set.
    /// </summary>
    public const string DefaultPath = "team";

    /// <summary>
    /// The path segment for an invitation link, without a leading or trailing slash.
    /// </summary>
    /// <param name="invitePath">The host's <c>InvitePath</c>, in whatever shape they wrote it.</param>
    /// <remarks>
    /// A blank value falls back to the default rather than producing a link to the site root. A host that
    /// sets the option to an empty string has misconfigured it, and a link to the root would look
    /// plausible while redeeming nothing.
    /// </remarks>
    public static string Resolve(string invitePath)
    {
        if (string.IsNullOrWhiteSpace(invitePath)) return DefaultPath;

        var trimmed = invitePath.Trim().Trim('/');

        return trimmed.Length == 0 ? DefaultPath : trimmed;
    }
}
