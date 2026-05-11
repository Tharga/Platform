using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Tharga.MongoDB;
using Tharga.MongoDB.Disk;

namespace Tharga.Team.MongoDB;

/// <summary>
/// MongoDB collection definition for User documents. Public so consumers can subclass it
/// to declare per-deployment indices on their own <typeparamref name="TUserEntity"/> shape.
/// Register the subclass via <see cref="ThargaTeamOptions.RegisterUserRepository{TUserEntity, TCollection}"/>.
/// </summary>
public class UserRepositoryCollection<TUserEntity> : DiskRepositoryCollectionBase<TUserEntity>, IUserRepositoryCollection<TUserEntity>
    where TUserEntity : EntityBase, IUser
{
    private readonly string _collectionName;

    public UserRepositoryCollection(IMongoDbServiceFactory mongoDbServiceFactory, ILogger<UserRepositoryCollection<TUserEntity>> logger, IOptions<ThargaTeamOptions> options = null)
        : base(mongoDbServiceFactory, logger)
    {
        _collectionName = options?.Value.UserCollectionName ?? "User";
    }

    public override string CollectionName => _collectionName;

    public override IEnumerable<CreateIndexModel<TUserEntity>> Indices =>
    [
        new(Builders<TUserEntity>.IndexKeys.Ascending(x => x.Identity),
            new CreateIndexOptions { Unique = true, Name = "Identity" })
    ];
}
