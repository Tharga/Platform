using MongoDB.Bson.Serialization.Attributes;
using Tharga.MongoDB;

namespace Tharga.Team.Service;

/// <summary>
/// Default MongoDB entity for API keys.
/// </summary>
public record ApiKeyEntity : EntityBase, IApiKey
{
    /// <inheritdoc />
    public required string Key { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    [BsonIgnoreIfDefault]
    public string ApiKey { get; init; }

    /// <inheritdoc />
    [BsonIgnoreIfDefault]
    public string TeamKey { get; init; }

    /// <inheritdoc />
    [BsonIgnoreIfNull]
    public string[] SystemScopes { get; init; }

    /// <inheritdoc />
    [BsonIgnoreIfNull]
    public string CreatedBy { get; init; }

    /// <inheritdoc />
    [BsonIgnoreIfDefault]
    public Dictionary<string, string> Tags { get; init; } = new();

    /// <summary>Hashed value of the API key used for verification.</summary>
    public required string ApiKeyHash { get; init; }

    /// <summary>First 8 characters of the raw API key for indexed prefix lookup. Avoids full table scan.</summary>
    [BsonIgnoreIfNull]
    public string ApiKeyPrefix { get; init; }

    /// <summary>Access level for this API key. Null defaults to Administrator.</summary>
    [BsonIgnoreIfNull]
    public AccessLevel? AccessLevel { get; init; }

    /// <summary>Tenant roles assigned to this API key.</summary>
    [BsonIgnoreIfNull]
    public string[] Roles { get; init; }

    /// <summary>Individual scope overrides (additional scopes beyond AccessLevel and roles).</summary>
    [BsonIgnoreIfNull]
    public string[] ScopeOverrides { get; init; }

    /// <summary>Expiry date. Null means no expiry.</summary>
    [BsonIgnoreIfNull]
    public DateTime? ExpiryDate { get; init; }

    /// <summary>When this key was created.</summary>
    [BsonIgnoreIfNull]
    public DateTime? CreatedAt { get; init; }
}
