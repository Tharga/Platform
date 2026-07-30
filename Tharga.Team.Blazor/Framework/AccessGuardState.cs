namespace Tharga.Team.Blazor.Framework;

/// <summary>Which guard branch a view should render before its content.</summary>
public enum AccessGuard
{
    /// <summary>A prerequisite service is not registered — the feature cannot work at all.</summary>
    NotConfigured,

    /// <summary>Initialization has not finished, so nothing is yet known about access.</summary>
    Loading,

    /// <summary>Initialization finished and the caller lacks the required permission.</summary>
    Denied,

    /// <summary>Render the content.</summary>
    Ready
}

/// <summary>
/// Chooses the guard branch for a view whose access flag is resolved inside an async lifecycle method.
/// </summary>
/// <remarks>
/// Blazor renders once before the awaits in <c>OnInitializedAsync</c> resolve. An access flag defaulting
/// to <c>false</c> is therefore indistinguishable from a denial on that first frame, so a view that tests
/// its access flag ahead of its loaded flag renders "Access denied" to every caller, including one with
/// full rights, before replacing it with the content.
/// <para>
/// A denial shown from an unresolved state is worse than a blank one: it tells the caller something untrue
/// about their permissions, and it teaches them to ignore a message that is sometimes real. So the order
/// here is deliberate and is the thing under test — <b>not loaded outranks not authorized</b>.
/// </para>
/// </remarks>
public static class AccessGuardState
{
    /// <param name="notConfigured">A required service is missing. Known synchronously, so it ranks first.</param>
    /// <param name="loaded">Initialization has completed and <paramref name="hasAccess"/> is meaningful.</param>
    /// <param name="hasAccess">Whether the caller holds the required permission. Only read once loaded.</param>
    public static AccessGuard Resolve(bool notConfigured, bool loaded, bool hasAccess)
    {
        if (notConfigured) return AccessGuard.NotConfigured;
        if (!loaded) return AccessGuard.Loading;
        return hasAccess ? AccessGuard.Ready : AccessGuard.Denied;
    }
}
