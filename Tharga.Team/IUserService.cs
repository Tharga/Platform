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
}