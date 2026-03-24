using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Tharga.Team.MongoDB;

public abstract record TeamMemberBase : ITeamMember
{
    public string Key { get; set; }
    public string Name { get; init; }
    public Invitation Invitation { get; init; }
    public DateTime? LastSeen { get; init; }

    [BsonRepresentation(BsonType.String)]
    public MembershipState? State { get; init; }

    [BsonRepresentation(BsonType.String)]
    public AccessLevel AccessLevel { get; init; }

    [BsonIgnoreIfNull]
    public string[] TenantRoles { get; init; }

    [BsonIgnoreIfNull]
    public string[] ScopeOverrides { get; init; }
}