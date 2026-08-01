using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Disabling a user: the reversible alternative to deleting them.
/// </summary>
/// <remarks>
/// Deleting a user removes them from every team and drops the record. That is far too final for someone
/// on leave, a contractor between engagements, or an account suspected of compromise — all of which end
/// with the person coming back.
/// <para>
/// <b>The load-bearing test is <see cref="DisablingYourself_IsRefused"/>.</b> The rule is stated in
/// <c>UserAdminGate</c> too, but a gate only decides what a row offers: a host can inject its own entry
/// through <c>ActionItems</c> and dispatch straight to the service. A UI-only guard is one the server
/// never applies.
/// </para>
/// </remarks>
public class UserDisableTests
{
    private sealed record FakeUser(string Key, string Identity, string EMail) : IUser;

    /// <remarks>
    /// <c>GetUserByKeyAsync</c> is stubbed rather than <c>GetAsync</c>, even though the former is a
    /// default interface member implemented in terms of the latter: the substitute proxies every
    /// interface member, so the default body never runs and stubbing <c>GetAsync</c> has no effect.
    /// Unknown keys must be forced to null too — an unconfigured call returns an auto-substitute, not
    /// null, which would make "user not found" untestable.
    /// </remarks>
    private static IUserService UserStore(string callerKey)
    {
        var userService = Substitute.For<IUserService>();
        userService.GetUserByKeyAsync(Arg.Any<string>()).Returns((IUser)null);
        userService.GetUserByKeyAsync("u1").Returns(new FakeUser("u1", "alice", "alice@example.com"));
        userService.GetCurrentUserAsync().Returns(
            callerKey == null ? null : new FakeUser(callerKey, callerKey, $"{callerKey}@example.com"));
        return userService;
    }

    private static (UserManagementService Sut, IUserService UserService) Build(string callerKey)
    {
        var userService = UserStore(callerKey);
        var sut = new UserManagementService(userService, Substitute.For<ITeamService>());
        return (sut, userService);
    }

    [Fact]
    public async Task Disabling_RecordsWhenAndByWhom()
    {
        var (sut, userService) = Build(callerKey: "admin");

        await sut.SetUserDisabledAsync("u1", disabled: true);

        await userService.Received(1).SetUserDisabledAsync("u1", Arg.Is<DateTime?>(d => d != null), "admin");
    }

    /// <summary>Enabling clears both, so a re-enabled user carries no stale trace of the old decision.</summary>
    [Fact]
    public async Task Enabling_ClearsBoth()
    {
        var (sut, userService) = Build(callerKey: "admin");

        await sut.SetUserDisabledAsync("u1", disabled: false);

        await userService.Received(1).SetUserDisabledAsync("u1", null, null);
    }

    /// <summary>
    /// An administrator who disables themselves needs a second administrator to undo it, and refusing
    /// the self-case also guarantees somebody is left holding <c>users:manage</c>.
    /// </summary>
    [Fact]
    public async Task DisablingYourself_IsRefused()
    {
        var (sut, userService) = Build(callerKey: "u1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetUserDisabledAsync("u1", disabled: true));

        await userService.DidNotReceiveWithAnyArgs().SetUserDisabledAsync(default, default, default);
    }

    /// <summary>Key comparison is case-insensitive, matching <c>UserAdminGate</c> — a differently-cased
    /// key is the same account, and the guard must not be side-steppable by how the key was typed.</summary>
    [Theory]
    [InlineData("U1")]
    [InlineData("u1")]
    public async Task DisablingYourself_IsRefusedWhateverTheCasing(string callerKey)
    {
        var (sut, _) = Build(callerKey);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetUserDisabledAsync("u1", disabled: true));
    }

    /// <summary>
    /// The guard is about the self-case only. Enabling is never refused — a disabled user has no session
    /// from which to enable themselves, so the case cannot arise, and blocking it would strand the one
    /// account an operator most needs to repair.
    /// </summary>
    [Fact]
    public async Task EnablingYourself_IsNotRefused()
    {
        var (sut, userService) = Build(callerKey: "u1");

        await sut.SetUserDisabledAsync("u1", disabled: false);

        await userService.Received(1).SetUserDisabledAsync("u1", null, null);
    }

    [Fact]
    public async Task DisablingAnUnknownUser_IsRefused()
    {
        var (sut, _) = Build(callerKey: "admin");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.SetUserDisabledAsync("nobody", disabled: true));
    }

    /// <summary>
    /// No cascade to the user's API keys. A key is not a session: it is an independent credential with
    /// its own lifecycle, and disabling a person should not silently retire integrations they happen to
    /// have minted. Where a compromise means both must stop, that is two deliberate acts.
    /// </summary>
    [Fact]
    public async Task Disabling_DoesNotTouchTeamsOrKeys()
    {
        var userService = UserStore("admin");
        var teamService = Substitute.For<ITeamService>();

        await new UserManagementService(userService, teamService).SetUserDisabledAsync("u1", disabled: true);

        await teamService.DidNotReceiveWithAnyArgs().RemoveUserFromAllTeamsAsync(default);
        await userService.DidNotReceiveWithAnyArgs().DeleteUserAsync(default);
    }

    /// <summary>
    /// A store that has not implemented the hook must say so rather than report success — silently
    /// skipping the write would hide a missing implementation behind an apparent containment.
    /// </summary>
    [Fact]
    public async Task AStoreWithoutTheHook_Throws()
    {
        await Assert.ThrowsAsync<NotSupportedException>(
            () => ((IUserService)new HookLessUserService()).SetUserDisabledAsync("u1", DateTime.UtcNow, "admin"));
    }

    private sealed class HookLessUserService : IUserService
    {
        public Task<IUser> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal claimsPrincipal = null) => Task.FromResult<IUser>(null);
        public IAsyncEnumerable<IUser> GetAsync() => throw new NotSupportedException();
        public Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;
    }
}
