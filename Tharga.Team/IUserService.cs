using System.Security.Claims;

namespace Tharga.Team;

public interface IUserService
{
    Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal = null);
    IAsyncEnumerable<IUser> GetAsync();

    /// <summary>
    /// Sets the user's display name only if it is currently null/empty. Used by the
    /// invitation-accept flow to promote the admin-entered invitation name into the
    /// new user's identity without clobbering an IdP-provided name.
    /// </summary>
    Task SeedUserNameAsync(string userKey, string name);

    /// <summary>
    /// Always sets the user's display name. Used by the user self-edit flow where the
    /// caller has explicitly chosen a name for themselves.
    /// </summary>
    Task SetUserNameAsync(string userKey, string name);

    /// <summary>
    /// The user with the given key, or null. The default implementation scans <see cref="GetAsync"/>;
    /// storage-backed services override it with a direct read.
    /// </summary>
    async Task<IUser> GetUserByKeyAsync(string userKey)
    {
        if (string.IsNullOrEmpty(userKey)) return null;

        await foreach (var user in GetAsync())
        {
            if (user.Key == userKey) return user;
        }

        return null;
    }

    /// <summary>
    /// Stamps when the user last made an authenticated request. Called automatically by the resolve
    /// path (throttled), so the default is a no-op — stores that track <see cref="IUser.LastSeen"/>
    /// override it.
    /// </summary>
    Task SetUserLastSeenAsync(string userKey, DateTime lastSeen) => Task.CompletedTask;

    /// <summary>
    /// Links the user to their external-directory id (<see cref="IUser.DirectoryId"/>). Called by the
    /// oid backfill and by directory verification on an email-fallback match (relink). Default is a
    /// no-op — stores that track the directory id override it.
    /// </summary>
    Task SetUserDirectoryIdAsync(string userKey, string directoryId) => Task.CompletedTask;

    /// <summary>
    /// Deletes the user record from the store. Low-level storage operation with no authorization check
    /// or team-membership cleanup — call through <see cref="IUserManagementService.DeleteUserAsync"/>,
    /// which removes team memberships, audits, and enforces <see cref="SystemUserScopes.Manage"/>.
    /// </summary>
    Task DeleteUserAsync(string userKey)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(DeleteUserAsync)}. Implement it to support " +
            $"user deletion (the '{SystemUserScopes.Manage}' system scope).");
}