using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tharga.Toolkit;

namespace Tharga.Team;

public abstract class UserServiceBase : IUserService
{
    protected readonly AuthenticationStateProvider _authenticationStateProvider;

    private static readonly ConcurrentDictionary<string, IUser> _userCache = new();

    protected UserServiceBase(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    protected virtual async Task<ClaimsPrincipal> GetClaims(ClaimsPrincipal claimsPrincipal)
    {
        claimsPrincipal ??= (await _authenticationStateProvider.GetAuthenticationStateAsync()).User;
        return claimsPrincipal;
    }

    protected abstract Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal);
    protected abstract IAsyncEnumerable<IUser> GetAllAsync();

    public async Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal)
    {
        claimsPrincipal = await GetClaims(claimsPrincipal);
        var identity = claimsPrincipal.GetIdentity().Identity;
        if (identity == null) return null;

        if (_userCache.TryGetValue(identity, out var user)) return user;

        var userEntity = await GetUserAsync(claimsPrincipal);

        _userCache.TryAdd(identity, userEntity);

        return userEntity;
    }

    public virtual IAsyncEnumerable<IUser> GetAsync()
    {
        return GetAllAsync();
    }

    public virtual Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;

    public virtual Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;

    public virtual Task SetUserLastSeenAsync(string userKey, DateTime lastSeen) => Task.CompletedTask;

    public virtual Task SetUserDirectoryIdAsync(string userKey, string directoryId) => Task.CompletedTask;

    public virtual Task DeleteUserAsync(string userKey)
        => throw new NotSupportedException(
            $"'{GetType().Name}' does not implement {nameof(DeleteUserAsync)}. Implement it to support " +
            $"user deletion (the '{SystemUserScopes.Manage}' system scope).");

    protected void InvalidateUserCache(string identity)
    {
        if (!string.IsNullOrEmpty(identity)) _userCache.TryRemove(identity, out _);
    }
}