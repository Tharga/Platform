using Tharga.Team.MongoDB;

namespace Tharga.Platform.Sample.Framework.Team;

/// <summary>
/// Sample-only admin service for editing a user's name and email — data the toolkit does not manage.
/// Deletion is no longer here: the Platform's <see cref="Tharga.Team.IUserManagementService"/> handles
/// it (all-team removal, audit, the users:manage scope, and the opt-in directory delete).
/// </summary>
public class AppUserAdminService
{
    private readonly IUserRepositoryCollection<UserEntity> _collection;

    public AppUserAdminService(IUserRepositoryCollection<UserEntity> collection)
    {
        _collection = collection;
    }

    public async Task UpdateAsync(string key, string name, string email)
    {
        var existing = await _collection.GetOneAsync(x => x.Key == key);
        if (existing == null) return;
        var updated = existing with { Name = name, EMail = email };
        await _collection.ReplaceOneAsync(updated);
    }
}
