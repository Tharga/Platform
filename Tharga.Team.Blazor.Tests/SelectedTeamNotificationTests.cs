using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// <c>SelectedTeamChangedEvent</c> is raised only when the selection actually changed, and raised with the
/// resolution lock released.
/// </summary>
/// <remarks>
/// Issue #195. The event was raised on every resolution, so the idiomatic Blazor pattern — subscribe, and
/// reload when it fires — recursed without bound: reloading needs the current team, reading the current team
/// raised the event again. It was fatal for an authenticated caller with <b>no team</b>, where the selection
/// resolves to null and therefore never settles.
/// <para>
/// The defect was the raise being unconditional, not the comparison being wrong — so these drive
/// <see cref="TeamStateService"/> itself rather than testing a comparison helper, which would have passed
/// throughout the three releases this shipped in.
/// </para>
/// </remarks>
public class SelectedTeamNotificationTests
{
    private const string TeamKey = "team-1";

    /// <summary>
    /// A re-entering handler stops here instead of recursing for ever, so the regression test fails with a
    /// count rather than taking the test host down with a stack overflow.
    /// </summary>
    private const int RecursionCap = 20;

    private readonly Mock<ITeamService> _teamService = new();
    private readonly Mock<ILocalStorageService> _localStorage = new();
    private readonly Mock<IJSRuntime> _js = new();
    private readonly Mock<AuthenticationStateProvider> _authenticationStateProvider = new();
    private readonly FakeNavigationManager _navigation = new();
    private readonly ThargaBlazorOptions _options = new();

    public SelectedTeamNotificationTests()
    {
        _localStorage
            .Setup(x => x.GetItemAsStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        _js
            .Setup(x => x.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

        HasTeams();
    }

    private TeamStateService CreateSut()
    {
        return new TeamStateService(_teamService.Object, _navigation, _localStorage.Object, _js.Object, _authenticationStateProvider.Object, Options.Create(_options));
    }

    private void HasTeams(params ITeam[] teams)
    {
        _teamService.Setup(x => x.GetTeamsAsync()).Returns(() => AsAsync(teams));
    }

    /// <param name="currentTeamKey">
    /// The team the request's claims already name. When it matches the resolved team no refresh is needed,
    /// which is what lets a test observe the event rather than a page reload.
    /// </param>
    private void SignedIn(string currentTeamKey = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
        if (currentTeamKey != null) claims.Add(new Claim(Constants.TeamKeyCookie, currentTeamKey));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _authenticationStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(principal));
    }

    private static async IAsyncEnumerable<ITeam> AsAsync(ITeam[] teams)
    {
        await Task.CompletedTask;
        foreach (var team in teams) yield return team;
    }

    /// <summary>
    /// The regression, at the shape it actually took: the caller has no team, and the handler does what the
    /// idiomatic pattern leads you to write.
    /// </summary>
    [Fact]
    public async Task AHandlerReadingTheSelection_DoesNotRecurse_WhenThereIsNoTeam()
    {
        SignedIn();
        var sut = CreateSut();

        var raised = 0;
        sut.SelectedTeamChangedEvent += async (_, _) =>
        {
            raised++;
            if (raised < RecursionCap) await sut.GetSelectedTeamAsync();
        };

        var team = await sut.GetSelectedTeamAsync();

        Assert.Null(team);
        Assert.Equal(0, raised);
    }

    /// <summary>
    /// The self-check on the test above: with no team, every call really does re-resolve, so a zero count
    /// means the raise was suppressed rather than the resolution being skipped.
    /// </summary>
    [Fact]
    public async Task TheNoTeamCase_ReResolvesOnEveryCall()
    {
        SignedIn();
        var sut = CreateSut();

        await sut.GetSelectedTeamAsync();
        await sut.GetSelectedTeamAsync();

        _localStorage.Verify(x => x.GetItemAsStringAsync(Constants.SelectedTeamLocalStorageKey, It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(0, _navigation.RefreshCount);
    }

    [Fact]
    public async Task ResolvingATeam_RaisesOnce()
    {
        SignedIn(TeamKey);
        HasTeams(new FakeTeam(TeamKey, "Alpha"));
        var sut = CreateSut();

        var raised = new List<ITeam>();
        sut.SelectedTeamChangedEvent += (_, e) => raised.Add(e.SelectedTeam);

        var team = await sut.GetSelectedTeamAsync();

        Assert.Equal(TeamKey, team.Key);
        Assert.Equal(TeamKey, Assert.Single(raised).Key);
    }

    [Fact]
    public async Task ResolvingTheSameTeamAgain_RaisesNothing()
    {
        SignedIn(TeamKey);
        HasTeams(new FakeTeam(TeamKey, "Alpha"));
        var sut = CreateSut();

        await sut.GetSelectedTeamAsync();

        var raised = 0;
        sut.SelectedTeamChangedEvent += (_, _) => raised++;

        await sut.GetSelectedTeamAsync();
        await sut.GetSelectedTeamAsync();

        Assert.Equal(0, raised);
    }

    /// <summary>
    /// A rename keeps the key and so would slip past a key-only comparison, leaving every subscriber
    /// rendering the old name.
    /// </summary>
    [Fact]
    public async Task RenamingTheSelectedTeam_Raises()
    {
        SignedIn(TeamKey);
        HasTeams(new FakeTeam(TeamKey, "Alpha"));
        var sut = CreateSut();

        await sut.GetSelectedTeamAsync();

        var raised = new List<ITeam>();
        sut.SelectedTeamChangedEvent += (_, e) => raised.Add(e.SelectedTeam);

        HasTeams(new FakeTeam(TeamKey, "Beta"));
        var team = await sut.GetSelectedTeamAsync();

        Assert.Equal("Beta", team.Name);
        Assert.Equal("Beta", Assert.Single(raised).Name);
    }

    /// <summary>
    /// The second half of the fix. The raise used to happen inside the resolution lock, so a handler reading
    /// the selection — the natural thing to do from one — queued behind the lock its own raiser still held.
    /// Completing within the raise is what proves the lock was free.
    /// </summary>
    [Fact]
    public async Task TheEventIsRaisedWithTheResolutionLockReleased()
    {
        SignedIn(TeamKey);
        HasTeams(new FakeTeam(TeamKey, "Alpha"));
        var sut = CreateSut();

        var reentered = false;
        ITeam fromHandler = null;
        sut.SelectedTeamChangedEvent += async (_, _) =>
        {
            if (reentered) return;
            reentered = true;
            fromHandler = await sut.GetSelectedTeamAsync();
        };

        await sut.GetSelectedTeamAsync();

        Assert.True(reentered);
        Assert.Equal(TeamKey, fromHandler?.Key);
    }

    [Fact]
    public void TryGetSelectedTeam_ReportsNothingBeforeAnyResolution()
    {
        var sut = CreateSut();

        Assert.False(sut.TryGetSelectedTeam(out var team));
        Assert.Null(team);
        Assert.Empty(_localStorage.Invocations);
        Assert.Empty(_js.Invocations);
    }

    [Fact]
    public async Task TryGetSelectedTeam_ReportsTheResolvedTeam_WithoutInteropOrEvent()
    {
        SignedIn(TeamKey);
        HasTeams(new FakeTeam(TeamKey, "Alpha"));
        var sut = CreateSut();

        await sut.GetSelectedTeamAsync();

        _localStorage.Invocations.Clear();
        _js.Invocations.Clear();
        var raised = 0;
        sut.SelectedTeamChangedEvent += (_, _) => raised++;

        Assert.True(sut.TryGetSelectedTeam(out var team));
        Assert.Equal(TeamKey, team.Key);
        Assert.Empty(_localStorage.Invocations);
        Assert.Empty(_js.Invocations);
        Assert.Equal(0, raised);
    }

    /// <summary>
    /// What keeps the new member additive: an existing implementation that keeps no cache compiles
    /// unchanged, and reports "nothing known" rather than a wrong answer.
    /// </summary>
    [Fact]
    public void TheDefaultImplementation_ReportsNothingKnown()
    {
        ITeamStateService sut = new CacheLessStateService();

        Assert.False(sut.TryGetSelectedTeam(out var team));
        Assert.Null(team);
    }

    private sealed record FakeTeam(string Key, string Name) : ITeam
    {
        public string Icon => null;
    }

    private sealed class FakeNavigationManager : NavigationManager
    {
        public int RefreshCount { get; private set; }

        public FakeNavigationManager()
        {
            Initialize("https://localhost/", "https://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            RefreshCount++;
        }
    }

    private sealed class CacheLessStateService : ITeamStateService
    {
        public event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent { add { } remove { } }
        public event EventHandler<SelectedTeamChangedEventArgs> SelectedTeamChangedEvent { add { } remove { } }

        public Task<ITeam> GetSelectedTeamAsync() => Task.FromResult<ITeam>(null);

        public Task SetSelectedTeamAsync(ITeam selectedTeam) => Task.CompletedTask;
    }
}
