using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// The consumer-facing route into the ambient actor.
/// </summary>
/// <remarks>
/// <see cref="IAuditLogger.Log"/> takes a pre-built entry, so a host writing its own entries used to
/// construct <see cref="AuditEntry"/> by hand — which never consults the declared actor. Background work
/// could therefore push a scope and see no difference at all. These assert the factory closes that.
/// </remarks>
public class AuditEntryFactoryTests
{
    [Fact]
    public void InsideAScope_TheEntryCarriesTheDeclaredActor()
    {
        var context = new AuditContextAccessor();
        var sut = new AuditEntryFactory(NoHttpContext());
        var correlationId = Guid.NewGuid();

        using (context.Push(new AuditActor("nightly-retention", CorrelationId: correlationId)))
        {
            var entry = sut.Create("retention", "sweep", teamKey: "t-1");

            Assert.Equal(AuditCallerType.System, entry.CallerType);
            Assert.Equal(AuditCallerSource.Background, entry.CallerSource);
            Assert.Equal("nightly-retention", entry.CallerIdentity);
            Assert.Equal(correlationId, entry.CorrelationId);
            Assert.Equal("t-1", entry.TeamKey);
        }
    }

    /// <summary>Background work has no selected team to infer, so the caller supplies it.</summary>
    [Fact]
    public void WithoutAScope_TheEntryIsUnknownRatherThanAPhantomUser()
    {
        var sut = new AuditEntryFactory(NoHttpContext());

        var entry = sut.Create("retention", "sweep");

        Assert.Equal(AuditCallerType.Unknown, entry.CallerType);
        Assert.Null(entry.CallerIdentity);
    }

    [Fact]
    public void TheFeatureAndActionAreRecordedAsGiven()
    {
        var sut = new AuditEntryFactory(NoHttpContext());

        var entry = sut.Create("job", "claim", methodName: "ClaimAsync", durationMs: 42, success: false, errorMessage: "boom");

        Assert.Equal("job", entry.Feature);
        Assert.Equal("claim", entry.Action);
        Assert.Equal("ClaimAsync", entry.MethodName);
        Assert.Equal(42, entry.DurationMs);
        Assert.False(entry.Success);
        Assert.Equal("boom", entry.ErrorMessage);
    }

    /// <summary>
    /// The same precedence the decorators use: a real request wins, so a factory call made during one is
    /// attributed to the person, not to a scope someone left open.
    /// </summary>
    [Fact]
    public void DuringAnAuthenticatedRequest_ThePrincipalWins()
    {
        var context = new AuditContextAccessor();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "someone@example.com")], "Cookies"))
        });
        var sut = new AuditEntryFactory(accessor);

        using (context.Push(new AuditActor("nightly-retention")))
        {
            var entry = sut.Create("retention", "sweep");

            Assert.Equal(AuditCallerType.User, entry.CallerType);
            Assert.Equal("someone@example.com", entry.CallerIdentity);
        }
    }

    /// <summary>
    /// A job that works on one team states it once on the scope rather than on every entry. Background
    /// code has no selected team for the toolkit to infer, so without this the entries carry no team and
    /// cannot be found on a team-scoped audit view.
    /// </summary>
    [Fact]
    public void TheScopeCanDeclareTheTeam()
    {
        var context = new AuditContextAccessor();
        var sut = new AuditEntryFactory(NoHttpContext());

        using (context.Push(new AuditActor("nightly-retention", TeamKey: "t-1")))
        {
            Assert.Equal("t-1", sut.Create("retention", "sweep").TeamKey);
        }
    }

    /// <summary>A job that crosses teams overrides per entry.</summary>
    [Fact]
    public void AnExplicitTeamKeyOverridesTheScope()
    {
        var context = new AuditContextAccessor();
        var sut = new AuditEntryFactory(NoHttpContext());

        using (context.Push(new AuditActor("nightly-retention", TeamKey: "t-1")))
        {
            Assert.Equal("t-2", sut.Create("retention", "sweep", teamKey: "t-2").TeamKey);
        }
    }

    private static IHttpContextAccessor NoHttpContext()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext)null);
        return accessor;
    }
}
