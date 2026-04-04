using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.MongoDB;
using Tharga.MongoDB.Disk;

namespace Tharga.Team.MongoDB;

internal class UserRepositoryCollection<TUserEntity> : DiskRepositoryCollectionBase<TUserEntity>, IUserRepositoryCollection<TUserEntity>
    where TUserEntity : EntityBase, IUser
{
    private readonly string _collectionName;

    public UserRepositoryCollection(IMongoDbServiceFactory mongoDbServiceFactory, ILogger<UserRepositoryCollection<TUserEntity>> logger, IOptions<ThargaTeamOptions> options = null)
        : base(mongoDbServiceFactory, logger)
    {
        _collectionName = options?.Value.UserCollectionName ?? "User";
    }

    public override string CollectionName => _collectionName;
}
