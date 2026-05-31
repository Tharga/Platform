using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Guards the #75 type change: <c>ApiKeyEntity.Tags</c> moved from <c>Dictionary&lt;string,string&gt;</c>
/// to <c>IReadOnlyList&lt;Tag&gt;</c>. Verifies the Bson serializer round-trips it (the driver must map
/// the read-only interface back to a concrete list), including duplicate keys.
/// </summary>
public class ApiKeyEntityTagsSerializationTests
{
    [Fact]
    public void Tags_RoundTrip_Through_Bson_Preserving_Order_And_Duplicate_Keys()
    {
        var entity = new ApiKeyEntity
        {
            Id = ObjectId.GenerateNewId(),
            Key = "key-1",
            Name = "Test",
            ApiKeyHash = "hash",
            TeamKey = "team-1",
            Tags = new[]
            {
                new Tag("Type", "firewall"),
                new Tag("Type", "PIM"),
                new Tag("firewall.groupId", "ABC123"),
            },
        };

        var bson = entity.ToBson();
        var back = BsonSerializer.Deserialize<ApiKeyEntity>(bson);

        Assert.NotNull(back.Tags);
        Assert.Equal(3, back.Tags.Count);
        Assert.Equal(new[] { "firewall", "PIM" }, back.Tags.Where(t => t.Key == "Type").Select(t => t.Value));
        Assert.Equal("ABC123", back.Tags.Single(t => t.Key == "firewall.groupId").Value);
    }

    [Fact]
    public void Legacy_Document_Tags_Deserialize_As_Null_Without_Throwing()
    {
        // Simulates a pre-#75 document where Tags was an (empty) Dictionary<string,string> => BSON document.
        var doc = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "Key", "legacy-1" },
            { "Name", "Legacy" },
            { "ApiKeyHash", "hash" },
            { "TeamKey", "team-1" },
            { "Tags", new BsonDocument() },
        };

        var back = BsonSerializer.Deserialize<ApiKeyEntity>(doc);

        Assert.Null(back.Tags);
    }

    [Fact]
    public void Null_Tags_RoundTrip_As_Null()
    {
        var entity = new ApiKeyEntity
        {
            Id = ObjectId.GenerateNewId(),
            Key = "key-2",
            Name = "Test",
            ApiKeyHash = "hash",
            TeamKey = "team-1",
        };

        var back = BsonSerializer.Deserialize<ApiKeyEntity>(entity.ToBson());

        Assert.Null(back.Tags);
    }
}
