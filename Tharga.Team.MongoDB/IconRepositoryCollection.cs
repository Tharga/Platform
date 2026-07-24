using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Tharga.MongoDB;
using Tharga.MongoDB.Disk;

namespace Tharga.Team.MongoDB;

/// <summary>
/// MongoDB collection for icon documents. Default collection name <c>Icon</c> (override via
/// <see cref="ThargaTeamOptions.IconCollectionName"/>), with a unique index on <see cref="IconEntity.Key"/>.
/// </summary>
public class IconRepositoryCollection : DiskRepositoryCollectionBase<IconEntity>, IIconRepositoryCollection
{
    private readonly string _collectionName;

    public IconRepositoryCollection(IMongoDbServiceFactory mongoDbServiceFactory, ILogger<IconRepositoryCollection> logger, IOptions<ThargaTeamOptions> options = null)
        : base(mongoDbServiceFactory, logger)
    {
        _collectionName = options?.Value.IconCollectionName ?? "Icon";
    }

    public override string CollectionName => _collectionName;

    public override IEnumerable<CreateIndexModel<IconEntity>> Indices =>
    [
        new(Builders<IconEntity>.IndexKeys.Ascending(x => x.Key),
            new CreateIndexOptions { Unique = true, Name = "Key" })
    ];
}
