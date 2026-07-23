namespace Tharga.Team;

/// <summary>
/// Derives the initials shown as an avatar fallback when no icon image is resolved. One letter for a
/// single word, first+last for multiple; falls back to "?" for an empty name.
/// </summary>
public static class IconInitials
{
    public static string From(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";

        var parts = name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1)
        {
            var word = parts[0];
            return (word.Length == 1 ? word : word[..2]).ToUpperInvariant();
        }

        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }
}
