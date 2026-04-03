using MongoDB.Bson.Serialization.Attributes;
using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

public abstract record TeamEntityBase<TTeamMemberModel> : EntityBase, ITeam<TTeamMemberModel>
    where TTeamMemberModel : TeamMemberBase
{
    public required string Key { get; init; }

    [BsonIgnoreIfNull]
    public string Icon { get; init; }

    public required string Name { get; init; }
    public TTeamMemberModel[] Members { get; init; }

    [BsonIgnoreIfNull]
    public string[] ConsentedRoles { get; init; }
}