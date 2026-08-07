namespace Tharga.Team.Service.Tests;

/// <summary>
/// <see cref="ITeamCache"/> is the seam a multi-instance host replaces. These tests assert the seam is
/// actually used — that the toolkit reads and invalidates through whatever implementation it was given, rather
/// than through a cache of its own that a host cannot reach.
/// </summary>
/// <remarks>
/// The built-in <see cref="InMemoryTeamCache"/> is process-local, so a permission change made through one
/// instance is invisible to the others. That is what these tests protect: if a future change reintroduces a
/// private cache inside the service, substitution stops working and a multi-instance deployment silently goes
/// back to enforcing changes on one instance only.
/// </remarks>
public class TeamCacheSubstitutionTests
{
    private static readonly IReadOnlyList<TenantRoleDefinition> Registrar =
        [new TenantRoleDefinition("Registrar", ["case:read"])];

    /// <summary>Records what the toolkit asked of the cache, and answers only what it was told to hold.</summary>
    private sealed class RecordingCache : ITeamCache
    {
        private readonly InMemoryTeamCache _inner = new();

        public readonly List<string> Calls = [];

        public Task<CachedValue<IUser>> GetUserAsync(string identity)
        {
            Calls.Add($"get-user:{identity}");
            return _inner.GetUserAsync(identity);
        }

        public Task SetUserAsync(string identity, IUser user)
        {
            Calls.Add($"set-user:{identity}");
            return _inner.SetUserAsync(identity, user);
        }

        public Task RemoveUserAsync(string identity)
        {
            Calls.Add($"remove-user:{identity}");
            return _inner.RemoveUserAsync(identity);
        }

        public Task RemoveUserByKeyAsync(string userKey)
        {
            Calls.Add($"remove-user-by-key:{userKey}");
            return _inner.RemoveUserByKeyAsync(userKey);
        }

        public Task<CachedValue<ITeamMember>> GetMemberAsync(string teamKey, string userKey)
        {
            Calls.Add($"get-member:{teamKey}.{userKey}");
            return _inner.GetMemberAsync(teamKey, userKey);
        }

        public Task SetMemberAsync(string teamKey, string userKey, ITeamMember member)
        {
            Calls.Add($"set-member:{teamKey}.{userKey}");
            return _inner.SetMemberAsync(teamKey, userKey, member);
        }

        public Task RemoveMemberAsync(string teamKey, string userKey)
        {
            Calls.Add($"remove-member:{teamKey}.{userKey}");
            return _inner.RemoveMemberAsync(teamKey, userKey);
        }

        public Task RemoveMembersForUserAsync(string userKey)
        {
            Calls.Add($"remove-members-for-user:{userKey}");
            return _inner.RemoveMembersForUserAsync(userKey);
        }

        public Task<CachedValue<IReadOnlyList<TenantRoleDefinition>>> GetCustomRolesAsync(string teamKey)
        {
            Calls.Add($"get-custom-roles:{teamKey}");
            return _inner.GetCustomRolesAsync(teamKey);
        }

        public Task SetCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles)
        {
            Calls.Add($"set-custom-roles:{teamKey}");
            return _inner.SetCustomRolesAsync(teamKey, customRoles);
        }

        public Task RemoveCustomRolesAsync(string teamKey)
        {
            Calls.Add($"remove-custom-roles:{teamKey}");
            return _inner.RemoveCustomRolesAsync(teamKey);
        }
    }

    /// <summary>A cache that holds nothing — the shape of a host that wants caching off.</summary>
    private sealed class NeverCache : ITeamCache
    {
        public Task<CachedValue<IUser>> GetUserAsync(string identity) => Task.FromResult(CachedValue<IUser>.Miss);
        public Task SetUserAsync(string identity, IUser user) => Task.CompletedTask;
        public Task RemoveUserAsync(string identity) => Task.CompletedTask;
        public Task RemoveUserByKeyAsync(string userKey) => Task.CompletedTask;
        public Task<CachedValue<ITeamMember>> GetMemberAsync(string teamKey, string userKey) => Task.FromResult(CachedValue<ITeamMember>.Miss);
        public Task SetMemberAsync(string teamKey, string userKey, ITeamMember member) => Task.CompletedTask;
        public Task RemoveMemberAsync(string teamKey, string userKey) => Task.CompletedTask;
        public Task RemoveMembersForUserAsync(string userKey) => Task.CompletedTask;
        public Task<CachedValue<IReadOnlyList<TenantRoleDefinition>>> GetCustomRolesAsync(string teamKey) => Task.FromResult(CachedValue<IReadOnlyList<TenantRoleDefinition>>.Miss);
        public Task SetCustomRolesAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles) => Task.CompletedTask;
        public Task RemoveCustomRolesAsync(string teamKey) => Task.CompletedTask;
    }

    private static TestTeamService Build(ITeamCache cache, string teamKey, params TestMember[] members)
    {
        var caller = Substitute.For<IUser>();
        caller.Key.Returns("cache-caller");

        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns(caller);
        userService.GetCurrentUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(caller);

        var sut = new TestTeamService(userService, cache);
        sut.AddTeam(teamKey, "Cache Probe", members);
        return sut;
    }

    [Fact]
    public async Task TheMembershipLookup_GoesThroughTheSuppliedCache()
    {
        var cache = new RecordingCache();
        var sut = Build(cache, "sub-member", new TestMember { Key = "u1", AccessLevel = AccessLevel.Viewer });

        await sut.GetTeamMemberAsync("sub-member", "u1");
        await sut.GetTeamMemberAsync("sub-member", "u1");

        Assert.Equal(
            ["get-member:sub-member.u1", "set-member:sub-member.u1", "get-member:sub-member.u1"],
            cache.Calls);
        Assert.Equal(1, sut.GetTeamMembersCallCount);
    }

    [Fact]
    public async Task TheCustomRolesLookup_GoesThroughTheSuppliedCache()
    {
        var cache = new RecordingCache();
        var sut = Build(cache, "sub-roles");
        sut.SeedCustomRoles("sub-roles", Registrar);

        await sut.GetTeamCustomRolesAsync("sub-roles");
        await sut.GetTeamCustomRolesAsync("sub-roles");

        Assert.Equal(
            ["get-custom-roles:sub-roles", "set-custom-roles:sub-roles", "get-custom-roles:sub-roles"],
            cache.Calls);
        Assert.Equal(1, sut.GetTeamCallCount);
    }

    /// <summary>
    /// The invalidations are the half that matters for multi-instance: a shared cache is only useful if the
    /// toolkit tells it what changed.
    /// </summary>
    [Fact]
    public async Task AMemberWrite_InvalidatesThroughTheSuppliedCache()
    {
        var cache = new RecordingCache();
        var sut = Build(cache, "sub-write",
            new TestMember { Key = "admin", AccessLevel = AccessLevel.Administrator, State = MembershipState.Member },
            new TestMember { Key = "u1", AccessLevel = AccessLevel.Viewer, State = MembershipState.Member });

        await sut.SetMemberTenantRolesAsync("sub-write", "u1", ["Registrar"]);

        Assert.Contains("remove-member:sub-write.u1", cache.Calls);
    }

    [Fact]
    public async Task ACustomRolesWrite_InvalidatesThroughTheSuppliedCache()
    {
        var cache = new RecordingCache();
        var sut = Build(cache, "sub-roles-write");

        await sut.SetTeamCustomRolesAsync("sub-roles-write", Registrar);

        Assert.Contains("remove-custom-roles:sub-roles-write", cache.Calls);
    }

    [Fact]
    public async Task RemovingAUserFromAllTeams_InvalidatesThroughTheSuppliedCache()
    {
        var cache = new RecordingCache();
        var sut = Build(cache, "sub-purge");

        await Assert.ThrowsAsync<NotSupportedException>(() => sut.RemoveUserFromAllTeamsAsync("u1"));

        // The store hook is unimplemented on the test service, so the purge never reaches the cache -- which
        // is correct: nothing was removed, so nothing should be forgotten.
        Assert.DoesNotContain("remove-members-for-user:u1", cache.Calls);
    }

    /// <summary>
    /// A cache that holds nothing must still be correct — every lookup simply reaches the store. This is the
    /// contract that lets a host disable caching, and it is what an adapter degrades to when its backing store
    /// is unreachable.
    /// </summary>
    [Fact]
    public async Task ACacheThatHoldsNothing_IsStillCorrect()
    {
        var sut = Build(new NeverCache(), "sub-never",
            new TestMember { Key = "u1", AccessLevel = AccessLevel.Viewer });
        sut.SeedCustomRoles("sub-never", Registrar);

        var first = await sut.GetTeamMemberAsync("sub-never", "u1");
        var second = await sut.GetTeamMemberAsync("sub-never", "u1");
        var roles = await sut.GetTeamCustomRolesAsync("sub-never");

        Assert.Equal("u1", first.Key);
        Assert.Equal("u1", second.Key);
        Assert.Equal("Registrar", Assert.Single(roles).Name);
        Assert.Equal(2, sut.GetTeamMembersCallCount);
    }

    /// <summary>
    /// Two services sharing one cache see each other's entries — the property a multi-instance host is buying,
    /// expressed the only way a single-process test can: the second service must not re-read the store.
    /// </summary>
    [Fact]
    public async Task TwoServicesSharingACache_ShareItsEntries()
    {
        var cache = new InMemoryTeamCache();
        var first = Build(cache, "sub-shared", new TestMember { Key = "u1", AccessLevel = AccessLevel.Viewer });
        var second = Build(cache, "sub-shared", new TestMember { Key = "u1", AccessLevel = AccessLevel.Viewer });

        await first.GetTeamMemberAsync("sub-shared", "u1");
        await second.GetTeamMemberAsync("sub-shared", "u1");

        Assert.Equal(1, first.GetTeamMembersCallCount);
        Assert.Equal(0, second.GetTeamMembersCallCount);
    }
}
