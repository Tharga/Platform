using Tharga.Team.MongoDB;

namespace Tharga.Platform.Sample.Framework.Team;

/// <summary>
/// Sample-only admin service for editing / deleting users. In a real app, back this with audit
/// logging, team-membership checks before delete, etc. Demonstrates how a consumer wires its own
/// management service alongside the Platform-provided <see cref="Tharga.Team.Blazor.Features.User.UsersListView"/>.
/// </summary>
public class AppUserAdminService
{
    private readonly IUserRepositoryCollection<UserEntity> _collection;

    public AppUserAdminService(IUserRepositoryCollection<UserEntity> collection)
    {
        _collection = collection;
    }

    public async Task<UserEntity> GetByKeyAsync(string key)
    {
        return await _collection.GetOneAsync(x => x.Key == key);
    }

    public async Task UpdateAsync(string key, string name, string email)
    {
        var existing = await _collection.GetOneAsync(x => x.Key == key);
        if (existing == null) return;
        var updated = existing with { Name = name, EMail = email };
        await _collection.ReplaceOneAsync(updated);
    }

    public async Task DeleteAsync(string key)
    {
        await _collection.DeleteOneAsync(x => x.Key == key);
    }
}
