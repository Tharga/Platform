namespace Tharga.Team;

/// <summary>
/// The thing an icon is being resolved for, passed to each <see cref="IIconSource"/>. Carries what a
/// source might key off: the kind and key, a display name (for an initials fallback), any explicitly-set
/// icon reference, and — for user icons (Phase B) — the email and directory id.
/// </summary>
public sealed record IconSubject
{
    public required IconKind Kind { get; init; }
    public required string Key { get; init; }

    /// <summary>Display name, used for the initials fallback.</summary>
    public string Name { get; init; }

    /// <summary>The reference of an explicitly-set icon (from <see cref="IIconStore"/>), when one is set.</summary>
    public string IconReference { get; init; }

    /// <summary>The subject's email — used by user-icon sources such as Gravatar (Phase B).</summary>
    public string EMail { get; init; }

    /// <summary>The subject's external-directory id — used by the Entra-photo source (Phase B).</summary>
    public string DirectoryId { get; init; }
}
