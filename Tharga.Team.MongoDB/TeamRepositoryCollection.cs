using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Tharga.MongoDB;
using Tharga.MongoDB.Disk;

namespace Tharga.Team.MongoDB;

internal class TeamRepositoryCollection<TTeamEntity, TMember> : DiskRepositoryCollectionBase<TTeamEntity>, ITeamRepositoryCollection<TTeamEntity, TMember>
    where TTeamEntity : TeamEntityBase<TMember>
    where TMember : TeamMemberBase
{
    private readonly string _collectionName;

    public TeamRepositoryCollection(IMongoDbServiceFactory mongoDbServiceFactory, ILogger<RepositoryCollectionBase<TTeamEntity, ObjectId>> logger, IOptions<ThargaTeamOptions> options = null)
        : base(mongoDbServiceFactory, logger)
    {
        _collectionName = options?.Value.TeamCollectionName ?? "Team";
    }

    public override string CollectionName => _collectionName;

    public override IEnumerable<CreateIndexModel<TTeamEntity>> Indices =>
    [
        new(Builders<TTeamEntity>.IndexKeys.Ascending(x => x.Key), new CreateIndexOptions { Unique = true, Name = "Key" }),
        new(Builders<TTeamEntity>.IndexKeys.Combine(
            Builders<TTeamEntity>.IndexKeys.Ascending(x => x.Id),
            Builders<TTeamEntity>.IndexKeys.Ascending("Members.Key")
        ), new CreateIndexOptions { Unique = true, Name = "UniqueTeamMemberKey" })
    ];
}
