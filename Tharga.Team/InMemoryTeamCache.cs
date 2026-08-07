using System.Collections.Concurrent;

namespace Tharga.Team;

/// <summary>
/// The built-in <see cref="ITeamCache"/>: entries held in this process, with no expiry, dropped only when a
/// write invalidates them. Registered as a singleton by the toolkit unless a host registered its own.
/// </summary>
/// <remarks>
/// <b>Correct for exactly one instance.</b> Every entry is local to this process, so a change made through
/// another instance is not seen here — see <see cref="ITeamCache"/> for what goes stale and why revalidation
/// does not fix it. Running more than one instance means supplying a shared implementation instead.
/// <para>
/// No expiry is deliberate rather than an omission: a bounded lifetime would make staleness quieter without
/// making it shorter in the case that matters, since the entries that go stale are the ones nothing here
/// writes to. Invalidation is driven by the write paths on <c>UserServiceBase</c> and <c>TeamServiceBase</c>,
/// which are non-virtual for that reason — a host supplies persistence by overriding the protected member
/// underneath and cannot skip the invalidation above it.
/// </para>
/// </remarks>
public sealed class InMemoryTeamCache : ITeamCache
{
    private readonly ConcurrentDictionary<string, IUser> _users = new();
    private readonly ConcurrentDictionary<string, ITeamMember> _members = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<TenantRoleDefinition>> _customRoles = new();

    /// <summary>
    /// The instance used by a service that was constructed without one — which is what a host's own service
    /// does until its constructor forwards <see cref="ITeamCache"/>.
    /// </summary>
    /// <remarks>
    /// Shared rather than per-service because the services are scoped: a per-instance cache would live for
    /// one request and cache nothing across the requests this exists to serve. A host that registers its own
    /// <see cref="ITeamCache"/> and forwards it never touches this.
    /// </remarks>
    internal static readonly InMemoryTeamCache Shared = new();

    private static string MemberKey(string teamKey, string userKey) => $"{teamKey}.{userKey}";

    public Task<CachedValue<IUser>> GetUserAsync(string identity)
        => Task.FromResult(Read(_users, identity));

    public Task SetUserAsync(string identity, IUser user)
    {
        if (!string.IsNullOrEmpty(identity)) _users.TryAdd(identity, user);
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(string identity)
    {
        if (!string.IsNullOrEmpty(identity)) _users.TryRemove(identity, out _);
        return Task.CompletedTask;
    }

    /// <remarks>
    /// A scan, because the entry is keyed by identity. Cheap in practice — one entry per signed-in user per
    /// process — and cheaper than maintaining a second index for a call that happens on a write.
    /// </remarks>
    public Task RemoveUserByKeyAsync(string userKey)
    {
        if (string.IsNullOrEmpty(userKey)) return Task.CompletedTask;

        foreach (var entry in _users)
        {
            if (entry.Value?.Key == userKey) _users.TryRemove(entry.Key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<CachedValue<ITeamMember>> GetMemberAsync(string teamKey, string userKey)
        => Task.FromResult(Read(_members, MemberKey(teamKey, userKey)));

    public Task SetMemberAsync(string teamKey, string userKey, ITeamMember member)
    {
        _members.TryAdd(MemberKey(teamKey, userKey), member);
        return Task.CompletedTask;
    }

    public Task RemoveMemberAsync(string teamKey, string userKey)
    {
        _members.TryRemove(MemberKey(teamKey, userKey), out _);
        return Task.CompletedTask;
    }

    public Task RemoveMembersForUserAsync(string userKey)
    {
        if (string.IsNullOrEmpty(userKey)) return Task.CompletedTask;

        var suffix = $".{userKey}";
        foreach (var key in _members.Keys.Where(x => x.EndsWith(suffix, StringComparison.Ordinal)).ToArray())
        {
            _members.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<CachedValue<IReadOnlyList<TenantRoleDefinition>>> GetCustomRolesAsync(string teamKey)
        => Task.FromResult(Read(_customRoles, teamKey));

    public Task SetCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles)
    {
        if (!string.IsNullOrEmpty(teamKey)) _customRoles.TryAdd(teamKey, customRoles);
        return Task.CompletedTask;
    }

    public Task RemoveCustomRolesAsync(string teamKey)
    {
        if (!string.IsNullOrEmpty(teamKey)) _customRoles.TryRemove(teamKey, out _);
        return Task.CompletedTask;
    }

    // An unusable key is reported as a miss rather than looked up: ConcurrentDictionary rejects a null key.
    private static CachedValue<T> Read<T>(ConcurrentDictionary<string, T> store, string key)
    {
        if (string.IsNullOrEmpty(key)) return CachedValue<T>.Miss;

        return store.TryGetValue(key, out var value) ? CachedValue<T>.Hit(value) : CachedValue<T>.Miss;
    }
}
