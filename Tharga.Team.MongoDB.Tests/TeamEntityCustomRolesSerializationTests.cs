using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// Verifies the runtime-defined <see cref="TeamEntityBase{T}.CustomRoles"/> field (Tharga/Platform#117)
/// round-trips through the MongoDB serializer — including each role's nested scope list — and that a
/// null value is omitted from the document (<c>[BsonIgnoreIfNull]</c>), matching <c>ConsentedRoles</c>.
/// </summary>
public class TeamEntityCustomRolesSerializationTests
{
    public record TestMember : TeamMemberBase;

    public record TestTeamEntity : TeamEntityBase<TestMember>;

    private static TestTeamEntity NewTeam(IReadOnlyList<TenantRoleDefinition> customRoles) => new()
    {
        Key = "team-1",
        Name = "Team One",
        Members = [],
        CustomRoles = customRoles
    };

    [Fact]
    public void CustomRoles_round_trips_through_the_serializer_including_nested_scopes()
    {
        var team = NewTeam(
        [
            new TenantRoleDefinition("Registrar", ["case:read", "case:write"], "Registers incoming cases"),
            new TenantRoleDefinition("Reader", ["case:read"])
        ]);

        var doc = team.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<TestTeamEntity>(doc);

        Assert.NotNull(restored.CustomRoles);
        Assert.Equal(2, restored.CustomRoles.Count);

        var registrar = restored.CustomRoles.Single(r => r.Name == "Registrar");
        Assert.Equal(["case:read", "case:write"], registrar.Scopes);
        Assert.Equal("Registers incoming cases", registrar.Description);

        var reader = restored.CustomRoles.Single(r => r.Name == "Reader");
        Assert.Equal(["case:read"], reader.Scopes);
        Assert.Null(reader.Description);
    }

    [Fact]
    public void CustomRoles_null_is_omitted_from_the_document()
    {
        var doc = NewTeam(null).ToBsonDocument();

        Assert.False(doc.Contains(nameof(TeamEntityBase<TestMember>.CustomRoles)));
    }
}
