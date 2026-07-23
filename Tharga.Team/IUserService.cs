using System.Security.Claims;

namespace Tharga.Team;

/// <summary>
/// The user store. Authorization is enforced in the service layer by
/// <c>AuthorizationUserServiceDecorator</c>: resolving the current user and the invitation-accept name
/// seeding are self-service; setting a display name is allowed on the caller's own record (otherwise
/// <c>users:manage</c>); the <c>[RequireScope]</c>-annotated members document the scope they require.
/// </summary>
public interface IUserService
{
    Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal = null);

    /// <summary>All users. Cross-user enumeration — requires <see cref="SystemUserScopes.Manage"/>.</summary>
    [RequireScope(SystemUserScopes.Manage)]
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
    [RequireScope(SystemUserScopes.Manage)]
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
    /// Stamps when the user last made an authenticated request. The automatic throttled stamping is an
    /// internal self-call that bypasses the authorization decorator; calling this member from outside
    /// requires <see cref="SystemUserScopes.Manage"/>. The default is a no-op — stores that track
    /// <see cref="IUser.LastSeen"/> override it.
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    Task SetUserLastSeenAsync(string userKey, DateTime lastSeen) => Task.CompletedTask;

    /// <summary>
    /// Links the user to their external-directory id (<see cref="IUser.DirectoryId"/>). Called by the
    /// oid backfill (internal self-call) and by directory verification on an email-fallback match
    /// (relink). Default is a no-op — stores that track the directory id override it.
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    Task SetUserDirectoryIdAsync(string userKey, string directoryId) => Task.CompletedTask;

    /// <summary>
    /// Deletes the user record from the store, with no team-membership cleanup — call through
    /// <see cref="IUserManagementService.DeleteUserAsync"/>, which removes team memberships and audits.
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    Task DeleteUserAsync(string userKey)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(DeleteUserAsync)}. Implement it to support " +
            $"user deletion (the '{SystemUserScopes.Manage}' system scope).");
}