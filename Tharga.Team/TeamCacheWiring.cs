namespace Tharga.Team;

/// <summary>
/// Whether the services a host registered are actually using the <see cref="ITeamCache"/> the container
/// resolves — the one way the cache seam can be configured and silently not take effect.
/// </summary>
/// <remarks>
/// <b>The hazard this exists to make loud.</b> The bases accept <see cref="ITeamCache"/> as an <i>optional</i>
/// constructor parameter, so a host service whose own constructor does not forward it compiles, starts, passes
/// its tests, and quietly uses the process-local fallback instead. A host that registered Redis for a
/// multi-instance deployment would believe it was in effect while a suspended member kept their team scopes on
/// every instance that did not handle the write.
/// <para>
/// <b>It inspects the outcome, not the constructor.</b> Comparing the cache each live service holds against
/// the registered one cannot produce a false positive, whereas reflecting over constructor parameters would
/// misreport any host obtaining the cache some other way.
/// </para>
/// </remarks>
public static class TeamCacheWiring
{
    /// <summary>
    /// The names of the supplied services that are not using <paramref name="registered"/>. Empty when the
    /// wiring is correct, when no custom cache is registered, or when a service is not one of ours.
    /// </summary>
    /// <param name="registered">The <see cref="ITeamCache"/> the container resolves.</param>
    /// <param name="services">The concrete team and user services the host registered.</param>
    public static IReadOnlyList<string> FindUnwired(ITeamCache registered, params object[] services)
    {
        // The built-in default needs no forwarding: the fallback is another InMemoryTeamCache, so the two are
        // behaviourally identical -- both process-local with no expiry. Warning here would fire on every
        // default host and teach everyone to ignore the message. A custom registration is the only case where
        // not forwarding changes what the toolkit does, and it is always deliberate.
        if (registered is null or InMemoryTeamCache) return [];

        var unwired = new List<string>();

        foreach (var service in services ?? [])
        {
            var inUse = CacheOf(service);
            if (inUse == null) continue;
            if (ReferenceEquals(inUse, registered)) continue;

            unwired.Add(service.GetType().Name);
        }

        return unwired;
    }

    /// <summary>The cache a service is using, or null when it is not one of the toolkit's bases.</summary>
    private static ITeamCache CacheOf(object service) => service switch
    {
        TeamServiceBase teamService => teamService.CacheInUse,
        UserServiceBase userService => userService.CacheInUse,
        _ => null
    };

    /// <summary>
    /// The diagnostic shown when <see cref="FindUnwired"/> reports something, naming the types and the fix.
    /// </summary>
    public static string DescribeFailure(IReadOnlyList<string> unwired)
    {
        var names = string.Join(", ", unwired);

        return
            $"A custom {nameof(ITeamCache)} is registered, but {names} " +
            $"{(unwired.Count == 1 ? "is" : "are")} not using it — so those lookups are being served from a " +
            "process-local cache instead. On a single instance that is merely slower; across several it means a " +
            "suspended member keeps their team scopes and a disabled user keeps their session on every instance " +
            "that did not handle the write. " +
            $"Fix: add an optional '{nameof(ITeamCache)} cache = null' parameter to the constructor of each type " +
            "named above and pass it to the base constructor. See 'Claims-path caching' in the implementation guide.";
    }
}
