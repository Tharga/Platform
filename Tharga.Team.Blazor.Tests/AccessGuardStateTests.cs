using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

public class AccessGuardStateTests
{
    /// <summary>
    /// The defect this exists to prevent. On the first render both flags are false, because the access
    /// flag is only assigned after the awaits in OnInitializedAsync. Resolving that to Denied shows
    /// "Access denied" to a caller with full rights on every single load.
    /// </summary>
    [Fact]
    public void NotLoadedAndNotYetAuthorized_IsLoading_NotDenied()
    {
        Assert.Equal(AccessGuard.Loading, AccessGuardState.Resolve(notConfigured: false, loaded: false, hasAccess: false));
    }

    [Fact]
    public void NotLoaded_IsLoading_EvenWhenAccessAlreadyResolvedTrue()
    {
        Assert.Equal(AccessGuard.Loading, AccessGuardState.Resolve(notConfigured: false, loaded: false, hasAccess: true));
    }

    [Fact]
    public void LoadedWithoutAccess_IsDenied()
    {
        Assert.Equal(AccessGuard.Denied, AccessGuardState.Resolve(notConfigured: false, loaded: true, hasAccess: false));
    }

    [Fact]
    public void LoadedWithAccess_IsReady()
    {
        Assert.Equal(AccessGuard.Ready, AccessGuardState.Resolve(notConfigured: false, loaded: true, hasAccess: true));
    }

    /// <summary>
    /// A missing service is known synchronously and is not an access question, so it outranks both —
    /// telling the caller they lack permission when the feature was never registered sends them to their
    /// administrator for a grant that would not help.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NotConfigured_OutranksEverything(bool loaded, bool hasAccess)
    {
        Assert.Equal(AccessGuard.NotConfigured, AccessGuardState.Resolve(notConfigured: true, loaded, hasAccess));
    }
}
