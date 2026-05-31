using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Serializer for an API key's <c>Tags</c> (a list of <see cref="Tag"/>). Tolerates the legacy
/// representation: before #75, <c>Tags</c> was a <c>Dictionary&lt;string,string&gt;</c> persisted as a
/// BSON <em>document</em> (always empty in practice). Reading such a document as the new array type
/// would throw; this serializer maps a legacy document (or null) to <c>null</c> and otherwise reads a
/// normal array. New writes are always arrays. Pair with <c>IApiKeyRepository.CleanLegacyTagsAsync</c>
/// to purge the legacy field permanently.
/// </summary>
internal sealed class TagListBsonSerializer : SerializerBase<IReadOnlyList<Tag>>
{
    private static readonly IBsonSerializer<Tag> TagSerializer = BsonSerializer.LookupSerializer<Tag>();

    public override IReadOnlyList<Tag> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        switch (reader.GetCurrentBsonType())
        {
            case BsonType.Null:
                reader.ReadNull();
                return null;
            case BsonType.Document:
                // Legacy Dictionary<string,string> (empty) — treat as no tags.
                reader.SkipValue();
                return null;
            case BsonType.Array:
                var list = new List<Tag>();
                reader.ReadStartArray();
                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    list.Add(TagSerializer.Deserialize(context));
                reader.ReadEndArray();
                return list;
            default:
                throw new FormatException($"Cannot deserialize Tags from BSON type '{reader.GetCurrentBsonType()}'.");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IReadOnlyList<Tag> value)
    {
        var writer = context.Writer;
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        foreach (var tag in value)
            TagSerializer.Serialize(context, tag);
        writer.WriteEndArray();
    }
}
