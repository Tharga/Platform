namespace Tharga.Team.Service.Tests;

/// <summary>
/// Detection of the one way the <see cref="ITeamCache"/> seam can be configured and silently not take effect:
/// a host registers a shared cache, but its own service constructor never forwards it, so the base falls back
/// to the process-local instance.
/// </summary>
/// <remarks>
/// <b>Both directions matter equally.</b> Missing the real failure leaves a host believing Redis is in effect
/// while a suspended member keeps their scopes. Firing on a correct or default setup is arguably worse — a
/// startup error that everyone learns to ignore protects nothing, and this one throws.
/// </remarks>
public class TeamCacheWiringTests
{
    /// <summary>Stands in for a host's shared cache. Holds nothing; identity is all these tests need.</summary>
    private sealed class SharedCache : ITeamCache
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

    private static TestTeamService Service(ITeamCache cache)
        => new(Substitute.For<IUserService>(), cache);

    /// <summary>The reported hazard: a shared cache is registered and the service never got it.</summary>
    [Fact]
    public void AServiceThatDidNotReceiveTheCache_IsReported()
    {
        var registered = new SharedCache();

        // Constructed without a cache — exactly what a host constructor that omits the parameter produces.
        var unwired = TeamCacheWiring.FindUnwired(registered, Service(cache: null));

        Assert.Equal([nameof(TestTeamService)], unwired);
    }

    [Fact]
    public void AServiceThatForwardedTheCache_IsNotReported()
    {
        var registered = new SharedCache();

        Assert.Empty(TeamCacheWiring.FindUnwired(registered, Service(registered)));
    }

    /// <summary>
    /// Forwarding a *different* shared cache is still wrong — the point is that the registered one is the one
    /// in use, not merely that something was passed.
    /// </summary>
    [Fact]
    public void ForwardingADifferentCache_IsReported()
    {
        var unwired = TeamCacheWiring.FindUnwired(new SharedCache(), Service(new SharedCache()));

        Assert.Equal([nameof(TestTeamService)], unwired);
    }

    /// <summary>
    /// **The false-positive guard, and the reason the check is safe to throw on.** In a default setup the
    /// container's built-in cache and the base's fallback are two different `InMemoryTeamCache` instances, so a
    /// naive identity comparison would fire on every host that has configured nothing. Both are process-local
    /// with no expiry, so not forwarding changes nothing — there is no defect to report.
    /// </summary>
    [Fact]
    public void TheBuiltInCache_IsNeverReported()
    {
        // Distinct instances, mirroring TryAddSingleton creating its own while the base holds Shared.
        var unwired = TeamCacheWiring.FindUnwired(new InMemoryTeamCache(), Service(cache: null));

        Assert.Empty(unwired);
    }

    [Fact]
    public void NoCacheRegisteredAtAll_IsNotReported()
    {
        Assert.Empty(TeamCacheWiring.FindUnwired(registered: null, Service(cache: null)));
    }

    /// <summary>A host service that does not derive from the toolkit's bases has no cache to forward.</summary>
    [Fact]
    public void AServiceThatIsNotOurs_IsIgnored()
    {
        Assert.Empty(TeamCacheWiring.FindUnwired(new SharedCache(), new object()));
    }

    [Fact]
    public void NullsAmongTheServices_AreIgnored()
    {
        Assert.Empty(TeamCacheWiring.FindUnwired(new SharedCache(), null, null));
    }

    /// <summary>Every offending service is named, not just the first — a host usually forgets both at once.</summary>
    [Fact]
    public void EveryUnwiredService_IsNamed()
    {
        var registered = new SharedCache();

        var unwired = TeamCacheWiring.FindUnwired(registered, Service(cache: null), Service(cache: null));

        Assert.Equal(2, unwired.Count);
    }

    /// <summary>
    /// The message has to name the types and the fix — a diagnostic that says only "misconfigured" sends the
    /// reader back to the source to work out which constructor.
    /// </summary>
    [Fact]
    public void TheFailureMessage_NamesTheTypeAndTheFix()
    {
        var message = TeamCacheWiring.DescribeFailure([nameof(TestTeamService)]);

        Assert.Contains(nameof(TestTeamService), message);
        Assert.Contains("constructor", message);
        Assert.Contains("suspended member", message);
    }
}
