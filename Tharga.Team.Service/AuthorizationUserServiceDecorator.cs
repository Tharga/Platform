using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Decorator over <see cref="IUserService"/> that enforces user-store authorization in the service
/// layer (so the same checks protect the Blazor circuit and any consumer's REST controller):
/// <list type="bullet">
/// <item>Resolve current user / seed-own-name (invitation accept) — pass through (self-service).</item>
/// <item>Set display name — allowed on the caller's own record, otherwise <c>users:manage</c>.</item>
/// <item>Enumerate users, read by key, write activity/directory fields, delete — <c>users:manage</c> (system).</item>
/// </list>
/// The automatic LastSeen stamping and oid backfill are internal self-calls inside
/// <see cref="UserServiceBase"/> and never pass through this decorator.
/// </summary>
public sealed class AuthorizationUserServiceDecorator : IUserService
{
    private readonly IUserService _inner;
    private readonly TeamAuthorizer _authorizer;

    public AuthorizationUserServiceDecorator(IUserService inner, TeamAuthorizer authorizer)
    {
        _inner = inner;
        _authorizer = authorizer;
    }

    // Self-service — pass through.
    public Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal = null) => _inner.GetCurrentUserAsync(claimsPrincipal);
    public Task SeedUserNameAsync(string userKey, string name) => _inner.SeedUserNameAsync(userKey, name);

    public async Task SetUserNameAsync(string userKey, string name)
    {
        await RequireSelfOrUsersManageAsync(userKey, nameof(SetUserNameAsync));
        await _inner.SetUserNameAsync(userKey, name);
    }

    // Administration — users:manage.
    public async IAsyncEnumerable<IUser> GetAsync()
    {
        await RequireUsersManageAsync(nameof(GetAsync));
        await foreach (var user in _inner.GetAsync())
        {
            yield return user;
        }
    }

    public async Task<IUser> GetUserByKeyAsync(string userKey)
    {
        await RequireUsersManageAsync(nameof(GetUserByKeyAsync));
        return await _inner.GetUserByKeyAsync(userKey);
    }

    public async Task SetUserLastSeenAsync(string userKey, DateTime lastSeen)
    {
        await RequireUsersManageAsync(nameof(SetUserLastSeenAsync));
        await _inner.SetUserLastSeenAsync(userKey, lastSeen);
    }

    public async Task SetUserDirectoryIdAsync(string userKey, string directoryId)
    {
        await RequireUsersManageAsync(nameof(SetUserDirectoryIdAsync));
        await _inner.SetUserDirectoryIdAsync(userKey, directoryId);
    }

    public async Task DeleteUserAsync(string userKey)
    {
        await RequireUsersManageAsync(nameof(DeleteUserAsync));
        await _inner.DeleteUserAsync(userKey);
    }

    private async Task RequireSelfOrUsersManageAsync(string userKey, string operation)
    {
        if (!string.IsNullOrEmpty(userKey))
        {
            var current = await _inner.GetCurrentUserAsync();
            if (current != null && current.Key == userKey) return;
        }

        await RequireUsersManageAsync(operation);
    }

    private async Task RequireUsersManageAsync(string operation)
    {
        if (await _authorizer.HasSystemScopeAsync(SystemUserScopes.Manage)) return;
        throw new UnauthorizedAccessException($"{operation} requires the '{SystemUserScopes.Manage}' system scope.");
    }
}
