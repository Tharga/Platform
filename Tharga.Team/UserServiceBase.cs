using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Tharga.Toolkit;

namespace Tharga.Team;

public abstract class UserServiceBase : IUserService
{
    protected readonly AuthenticationStateProvider _authenticationStateProvider;

    private static readonly ConcurrentDictionary<string, IUser> _userCache = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastSeenStamped = new();
    private static readonly ConcurrentDictionary<string, byte> _directoryIdBackfillAttempted = new();
    private readonly ILogger<UserServiceBase> _logger;

    protected UserServiceBase(AuthenticationStateProvider authenticationStateProvider, ILogger<UserServiceBase> logger = null)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// How often (at most) <see cref="IUser.LastSeen"/> is written on resolve. Null disables stamping;
    /// <see cref="TimeSpan.Zero"/> stamps on every resolve. The throttle is per process, so a multi-instance
    /// deployment writes at most once per interval per instance.
    /// </summary>
    protected virtual TimeSpan? LastSeenStampInterval => TimeSpan.FromMinutes(15);

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

        if (!_userCache.TryGetValue(identity, out var user))
        {
            user = await GetUserAsync(claimsPrincipal);
            _userCache.TryAdd(identity, user);
        }

        await TouchUserAsync(user, claimsPrincipal);

        return user;
    }

    private async Task TouchUserAsync(IUser user, ClaimsPrincipal claimsPrincipal)
    {
        if (user == null || string.IsNullOrEmpty(user.Key)) return;

        // Activity tracking must never break the resolve path (it runs inside the auth pipeline).
        try
        {
            await StampLastSeenAsync(user.Key);
            await BackfillDirectoryIdAsync(user, claimsPrincipal);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to stamp activity for user {UserKey}.", user.Key);
        }
    }

    private async Task StampLastSeenAsync(string userKey)
    {
        var interval = LastSeenStampInterval;
        if (interval == null) return;

        var now = DateTime.UtcNow;
        if (_lastSeenStamped.TryGetValue(userKey, out var stamped) && now - stamped < interval) return;

        _lastSeenStamped[userKey] = now;
        await SetUserLastSeenAsync(userKey, now);
    }

    private async Task BackfillDirectoryIdAsync(IUser user, ClaimsPrincipal claimsPrincipal)
    {
        if (!string.IsNullOrEmpty(user.DirectoryId)) return;

        // One attempt per user per process: if the store does not persist DirectoryId the value stays
        // null, and retrying every resolve would invalidate the user cache on each request.
        if (!_directoryIdBackfillAttempted.TryAdd(user.Key, 0)) return;

        var directoryId = claimsPrincipal.GetDirectoryId();
        if (string.IsNullOrEmpty(directoryId)) return;

        await SetUserDirectoryIdAsync(user.Key, directoryId);
    }

    public virtual IAsyncEnumerable<IUser> GetAsync()
    {
        return GetAllAsync();
    }

    public virtual Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;

    public virtual Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;

    public virtual async Task<IUser> GetUserByKeyAsync(string userKey)
    {
        if (string.IsNullOrEmpty(userKey)) return null;

        await foreach (var user in GetAllAsync())
        {
            if (user.Key == userKey) return user;
        }

        return null;
    }

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