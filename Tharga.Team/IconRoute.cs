namespace Tharga.Team;

/// <summary>
/// The route the platform serves stored icons from. Shared between the built-in
/// <see cref="StoredIconSource"/> (which builds the URL) and the hosting layer (which maps the endpoint),
/// so the two never drift.
/// </summary>
public static class IconRoute
{
    /// <summary>Base path of the icon-serving endpoint.</summary>
    public const string Base = "/_tharga/icon";

    /// <summary>The URL that serves the stored icon with the given reference.</summary>
    public static string Url(string reference) => $"{Base}/{Uri.EscapeDataString(reference)}";
}
