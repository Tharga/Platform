namespace Tharga.Team;

/// <summary>
/// The result of a cache lookup: whether an entry existed, and its value.
/// </summary>
/// <remarks>
/// <b>Found and value are separate on purpose.</b> Both cached lookups legitimately store <c>null</c> — a
/// caller who is not a member of a team is <i>remembered</i> as not being one. Collapsing "no entry" into
/// "null value" would send every non-member request back to the store, which is the cost the cache exists to
/// avoid.
/// </remarks>
/// <typeparam name="T">The cached value's type.</typeparam>
/// <param name="Found">Whether an entry existed for the key.</param>
/// <param name="Value">The cached value. Meaningful only when <paramref name="Found"/> is true.</param>
public readonly record struct CachedValue<T>(bool Found, T Value)
{
    /// <summary>A miss — no entry existed.</summary>
    public static CachedValue<T> Miss => new(false, default);

    /// <summary>A hit carrying <paramref name="value"/>, which may itself be null.</summary>
    public static CachedValue<T> Hit(T value) => new(true, value);
}

/// <summary>
/// Where the toolkit keeps the three lookups its claims path performs on every authenticating request: the
/// caller, their membership in the selected team, and that team's custom roles.
/// </summary>
/// <remarks>
/// <b>Implement this to run more than one instance.</b> The built-in <see cref="InMemoryTeamCache"/> is
/// process-local, so a permission change made through one instance never reaches the others — a suspended
/// member keeps their scopes, and a disabled user keeps their session, on every instance that did not handle
/// the write, until that instance restarts. Periodic claim revalidation does not correct it: it recomputes
/// through this same cache. A shared implementation (Redis, SQL, or any store every instance can see) is what
/// makes a multi-instance deployment enforce a change everywhere.
/// <para>
/// <b>Only you can serialize these values.</b> <see cref="IUser"/> and <see cref="ITeamMember"/> are
/// interfaces your own entities implement, so a distributed adapter serializes types it defines. That is why
/// this port exists rather than the toolkit shipping a distributed cache: the toolkit does not know the
/// concrete types.
/// </para>
/// <para>
/// <b>Every member may be a no-op.</b> Returning <see cref="CachedValue{T}.Miss"/> from every read is a
/// correct implementation — it disables caching and sends each lookup to the store. Nothing here may throw on
/// a miss, and a cache that fails should prefer reporting a miss over propagating: a read that cannot be
/// cached is slow, whereas one that throws breaks sign-in.
/// </para>
/// <para>
/// Removal is expressed as <b>what the caller changed</b>, not as a key to delete, so an adapter is free to
/// key its store however it likes. The two by-user removals are the awkward pair for a distributed store,
/// because neither is keyed the way the entry is: expect to keep a companion index from a user key to that
/// user's identity and teams.
/// </para>
/// </remarks>
public interface ITeamCache
{
    /// <summary>The user resolved for <paramref name="identity"/>, or a miss.</summary>
    Task<CachedValue<IUser>> GetUserAsync(string identity);

    /// <summary>Remembers <paramref name="user"/> — which may be null — for <paramref name="identity"/>.</summary>
    Task SetUserAsync(string identity, IUser user);

    /// <summary>Forgets the user cached for <paramref name="identity"/>. A no-op when nothing is cached.</summary>
    Task RemoveUserAsync(string identity);

    /// <summary>
    /// Forgets whichever cached user has this <see cref="IUser.Key"/>. Needed because the entry is keyed by
    /// identity while the toolkit's write paths name a user by key.
    /// </summary>
    Task RemoveUserByKeyAsync(string userKey);

    /// <summary>The membership of <paramref name="userKey"/> in <paramref name="teamKey"/>, or a miss.</summary>
    Task<CachedValue<ITeamMember>> GetMemberAsync(string teamKey, string userKey);

    /// <summary>Remembers <paramref name="member"/> — which may be null, meaning "not a member" — for this pair.</summary>
    Task SetMemberAsync(string teamKey, string userKey, ITeamMember member);

    /// <summary>Forgets one membership. A no-op when nothing is cached.</summary>
    Task RemoveMemberAsync(string teamKey, string userKey);

    /// <summary>Forgets every membership cached for <paramref name="userKey"/>, across all teams.</summary>
    Task RemoveMembersForUserAsync(string userKey);

    /// <summary>The custom roles defined on <paramref name="teamKey"/>, or a miss.</summary>
    Task<CachedValue<IReadOnlyList<TenantRoleDefinition>>> GetCustomRolesAsync(string teamKey);

    /// <summary>Remembers a team's custom roles, including an empty set — the common case, and worth caching.</summary>
    Task SetCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles);

    /// <summary>Forgets one team's custom roles. A no-op when nothing is cached.</summary>
    Task RemoveCustomRolesAsync(string teamKey);
}
