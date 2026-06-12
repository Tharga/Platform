namespace Tharga.Team;

/// <summary>
/// Represents an API key with associated metadata.
/// </summary>
public interface IApiKey
{
    /// <summary>Unique identifier for this API key entry.</summary>
    string Key { get; }

    /// <summary>Human-readable name for this API key.</summary>
    string Name { get; }

    /// <summary>The raw API key value (only populated on creation; otherwise empty).</summary>
    string ApiKey { get; }

    /// <summary>Team that owns this API key. Null for system keys (not bound to a team).</summary>
    string TeamKey { get; }

    /// <summary>Explicit scopes granted to a system key at creation time. Null/empty for team keys.</summary>
    string[] SystemScopes { get; }

    /// <summary>User who created this key (identity/display name). Null if not recorded.</summary>
    string CreatedBy { get; }

    /// <summary>
    /// Owning team member (<see cref="ITeamMember.Key"/>) for an owner-scoped ("private") key, or null
    /// for a normal team-wide key. Private keys are hidden from other members in the UI and can only be
    /// recycled/locked/deleted by their owner (a Developer-role caller may still manage them for audit).
    /// </summary>
    string OwnerMemberKey { get; }

    /// <summary>
    /// System-set key-value tags on this API key. A list (not a map), so a key may repeat.
    /// Set only at creation via the service; immutable thereafter and not editable from the UI.
    /// Each tag is surfaced as a <c>tag.{Key}</c> claim on the authenticated principal.
    /// </summary>
    IReadOnlyList<Tag> Tags { get; }

    /// <summary>Access level assigned to this API key. Null defaults to Administrator.</summary>
    AccessLevel? AccessLevel { get; }

    /// <summary>Roles assigned to this API key.</summary>
    string[] Roles { get; }

    /// <summary>Scope overrides for this API key.</summary>
    string[] ScopeOverrides { get; }

    /// <summary>Expiry date, or null if no expiry.</summary>
    DateTime? ExpiryDate { get; }

    /// <summary>When the key was created. Reset when the key is recycled (refreshed).</summary>
    DateTime? CreatedAt { get; }

    /// <summary>When the key was last used to authenticate, or null if never used. Reset when the key is recycled (refreshed).</summary>
    DateTime? LastUsedAt { get; }
}
