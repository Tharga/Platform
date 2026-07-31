using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// The ambient actor scope. These lean on concurrency and nesting rather than the happy path, because a
/// leaked or wrongly-restored scope attributes one job's actions to another — the same class of false
/// attribution this feature removes, and far harder to notice.
/// </summary>
public class AuditContextAccessorTests
{
    private static readonly AuditActor Worker = new("fortdocs-worker");

    [Fact]
    public void NoScope_HasNoActor()
    {
        Assert.Null(new AuditContextAccessor().Current);
    }

    [Fact]
    public void InsideAScope_TheActorIsRecorded()
    {
        var sut = new AuditContextAccessor();

        using (sut.Push(Worker))
        {
            var entry = Build();
            Assert.Equal(AuditCallerType.System, entry.CallerType);
            Assert.Equal(AuditCallerSource.Background, entry.CallerSource);
            Assert.Equal("fortdocs-worker", entry.CallerIdentity);
        }

        Assert.Equal(AuditCallerType.Unknown, Build().CallerType);
    }

    /// <summary>The whole point of <c>AsyncLocal</c> — the scope has to survive the awaits inside a job.</summary>
    [Fact]
    public async Task TheScopeSurvivesAnAwait()
    {
        var sut = new AuditContextAccessor();

        using (sut.Push(Worker))
        {
            await Task.Yield();
            await Task.Delay(1);

            Assert.Equal("fortdocs-worker", Build().CallerIdentity);
        }
    }

    /// <summary>A nested scope hands back to its parent, not to nothing.</summary>
    [Fact]
    public void NestedScopes_RestoreTheOuterActor()
    {
        var sut = new AuditContextAccessor();

        using (sut.Push(Worker))
        {
            using (sut.Push(new AuditActor("inner-job")))
            {
                Assert.Equal("inner-job", Build().CallerIdentity);
            }

            Assert.Equal("fortdocs-worker", Build().CallerIdentity);
        }

        Assert.Null(sut.Current);
    }

    /// <summary>
    /// Two jobs running at once must not see each other's actor. This is the failure that would corrupt an
    /// audit log quietly — every row plausible, some of them attributed to the wrong job.
    /// </summary>
    [Fact]
    public async Task ConcurrentFlows_DoNotSeeEachOthersActor()
    {
        var sut = new AuditContextAccessor();

        async Task<string> RunAsync(string name)
        {
            using (sut.Push(new AuditActor(name)))
            {
                await Task.Delay(Random.Shared.Next(1, 10));
                return Build().CallerIdentity;
            }
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => RunAsync($"job-{i}")));

        Assert.Equal(Enumerable.Range(0, 20).Select(i => $"job-{i}"), results);
    }

    /// <summary>
    /// The precedence rule. A scope left open on a pooled thread must never be able to relabel a real
    /// user's action as the system's — an authenticated principal always wins.
    /// </summary>
    [Fact]
    public void AnAuthenticatedCaller_IgnoresTheAmbientActor()
    {
        var sut = new AuditContextAccessor();

        using (sut.Push(Worker))
        {
            var entry = Build(Accessor(authenticated: true));

            Assert.Equal(AuditCallerType.User, entry.CallerType);
            Assert.Equal("someone@example.com", entry.CallerIdentity);
        }
    }

    /// <summary>
    /// An anonymous request is not a caller, so a declared actor is better than nothing — a job triggered
    /// through an unauthenticated endpoint still knows what it is.
    /// </summary>
    [Fact]
    public void AnAnonymousRequest_StillUsesTheAmbientActor()
    {
        var sut = new AuditContextAccessor();

        using (sut.Push(Worker))
        {
            Assert.Equal("fortdocs-worker", Build(Accessor(authenticated: false)).CallerIdentity);
        }
    }

    [Fact]
    public void ACorrelationIdOnTheActor_GroupsTheJobsEntries()
    {
        var sut = new AuditContextAccessor();
        var correlationId = Guid.NewGuid();

        using (sut.Push(Worker with { CorrelationId = correlationId }))
        {
            Assert.Equal(correlationId, Build().CorrelationId);
            Assert.Equal(correlationId, Build().CorrelationId);
        }
    }

    [Fact]
    public void DoubleDispose_DoesNotRestoreAStaleActor()
    {
        var sut = new AuditContextAccessor();

        var scope = sut.Push(Worker);
        scope.Dispose();

        using (sut.Push(new AuditActor("later-job")))
        {
            scope.Dispose();
            Assert.Equal("later-job", Build().CallerIdentity);
        }
    }

    private static AuditEntry Build(IHttpContextAccessor httpContextAccessor = null)
        => AuditHelper.BuildEntry(httpContextAccessor, "team", "create", "CreateTeamAsync", 1, true);

    private static IHttpContextAccessor Accessor(bool authenticated)
    {
        var identity = authenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "someone@example.com")], "Cookies")
            : new ClaimsIdentity();

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });
        return accessor;
    }
}
