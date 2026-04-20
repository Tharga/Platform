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

    /// <summary>Arbitrary key-value metadata associated with this API key.</summary>
    Dictionary<string, string> Tags { get; }

    /// <summary>Access level assigned to this API key. Null defaults to Administrator.</summary>
    AccessLevel? AccessLevel { get; }

    /// <summary>Roles assigned to this API key.</summary>
    string[] Roles { get; }

    /// <summary>Scope overrides for this API key.</summary>
    string[] ScopeOverrides { get; }

    /// <summary>Expiry date, or null if no expiry.</summary>
    DateTime? ExpiryDate { get; }

    /// <summary>When the key was created.</summary>
    DateTime? CreatedAt { get; }
}
