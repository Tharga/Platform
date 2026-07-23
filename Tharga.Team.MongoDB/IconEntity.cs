using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

/// <summary>
/// An icon document: the image bytes plus enough metadata to serve and manage them. Stored in its own
/// collection (default <c>Icon</c>), keyed by <see cref="Key"/> (the reference returned to callers) — never
/// inlined into the team/user documents, which are read on hot paths.
/// </summary>
public record IconEntity : EntityBase
{
    /// <summary>The reference used to load/delete this icon and stored on the owning team/user record.</summary>
    public required string Key { get; init; }

    [BsonRepresentation(BsonType.String)]
    public IconKind Kind { get; init; }

    /// <summary>The key of the team/user the icon was saved for (informational; not the load key).</summary>
    public string OwnerKey { get; init; }

    public required string ContentType { get; init; }

    public required byte[] Data { get; init; }

    public int Size { get; init; }

    public DateTime CreatedUtc { get; init; }
}
