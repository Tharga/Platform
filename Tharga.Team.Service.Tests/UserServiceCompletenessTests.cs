using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Persistence extension points on <see cref="UserServiceBase"/> are virtual with do-nothing defaults, so
/// forgetting one produces a write that reports success and discards the data. These pin what the
/// startup guard reports — including the <c>protected</c> member an interface map cannot see, which was
/// the one that cost the most to diagnose.
/// </summary>
public class UserServiceCompletenessTests
{
    private abstract class TestServiceBase(AuthenticationStateProvider p) : UserServiceBase(p)
    {
        protected override Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal) => Task.FromResult<IUser>(null);
        protected override IAsyncEnumerable<IUser> GetAllAsync() => AsyncEnumerable.Empty<IUser>();
    }

    /// <summary>A host that overrode nothing — every write is silently discarded.</summary>
    private sealed class ForgetfulService(AuthenticationStateProvider p) : TestServiceBase(p);

    /// <summary>A host that implemented the lot, as a storage base such as UserServiceRepositoryBase does.</summary>
    private sealed class CompleteService(AuthenticationStateProvider p) : TestServiceBase(p)
    {
        public override Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public override Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public override Task SetUserDirectoryIdAsync(string userKey, string directoryId) => Task.CompletedTask;
        protected override Task SetUserIconReferenceAsync(string userKey, string reference) => Task.CompletedTask;
    }

    private abstract class IntermediateBase(AuthenticationStateProvider p) : TestServiceBase(p)
    {
        public override Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public override Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;
    }

    /// <summary>A host extending its own base. The base's overrides count.</summary>
    private sealed class DerivedFromIntermediate(AuthenticationStateProvider p) : IntermediateBase(p);

    [Fact]
    public void CompleteService_ReportsNothing()
    {
        var gaps = UserServiceCompleteness.Find(typeof(CompleteService), iconStoreRegistered: true, directoryRegistered: true);

        Assert.Empty(gaps);
    }

    [Fact]
    public void ForgetfulService_ReportsTheAlwaysReachableGaps()
    {
        var gaps = UserServiceCompleteness.Find(typeof(ForgetfulService), iconStoreRegistered: false, directoryRegistered: false);

        Assert.Equal(["SetUserNameAsync", "SeedUserNameAsync"], gaps.Select(g => g.Member));
    }

    /// <summary>
    /// The case that defines the guard. `SetUserIconReferenceAsync` is `protected`, so it never appears
    /// in an interface map — a guard built on one would miss the worst gap while looking complete.
    /// </summary>
    [Fact]
    public void ProtectedIconMember_IsSeen_WhenAnIconStoreIsRegistered()
    {
        var gaps = UserServiceCompleteness.Find(typeof(ForgetfulService), iconStoreRegistered: true, directoryRegistered: false);

        Assert.Contains(gaps, g => g.Member == "SetUserIconReferenceAsync");
    }

    /// <summary>
    /// Reachability filtering. An un-overridden member is only a defect if something can call it, and
    /// reporting unreachable ones is the noise that trains people to ignore startup output.
    /// </summary>
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, true, true, true)]
    public void UnreachableFeatures_AreNotReported(bool iconStore, bool directory, bool expectIcon, bool expectDirectory)
    {
        var gaps = UserServiceCompleteness.Find(typeof(ForgetfulService), iconStore, directory);

        Assert.Equal(expectIcon, gaps.Any(g => g.Member == "SetUserIconReferenceAsync"));
        Assert.Equal(expectDirectory, gaps.Any(g => g.Member == "SetUserDirectoryIdAsync"));
    }

    /// <summary>An override on the host's own intermediate base counts — the walk goes up the chain.</summary>
    [Fact]
    public void OverrideOnAnIntermediateBase_Counts()
    {
        var gaps = UserServiceCompleteness.Find(typeof(DerivedFromIntermediate), iconStoreRegistered: false, directoryRegistered: false);

        Assert.Empty(gaps);
    }

    /// <summary>Every gap carries what is silently lost, not just the member name.</summary>
    [Fact]
    public void EveryGap_SaysWhatIsLost()
    {
        var gaps = UserServiceCompleteness.Find(typeof(ForgetfulService), iconStoreRegistered: true, directoryRegistered: true);

        Assert.All(gaps, g => Assert.False(string.IsNullOrWhiteSpace(g.Consequence)));
        Assert.Equal(4, gaps.Count);
    }

    [Fact]
    public void NullType_ReportsNothing()
    {
        Assert.Empty(UserServiceCompleteness.Find(null, true, true));
    }
}
