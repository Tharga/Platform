using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// Who an audit entry says acted, when there is no HTTP request behind the call.
/// </summary>
public class AuditActorTests
{
    /// <summary>
    /// The defect this feature exists to remove (Tharga/Team#163). A hosted service, a message handler or
    /// a background worker has no <c>HttpContext</c>, and every audited call it made was written as
    /// <c>User</c> with a null identity — a row claiming a person did something a machine did. Wrong is
    /// worse than missing here: read back, a false attribution is indistinguishable from a real one.
    /// </summary>
    [Fact]
    public void NoHttpContext_IsUnknown_NotUser()
    {
        var entry = Build(httpContextAccessor: null);

        Assert.Equal(AuditCallerType.Unknown, entry.CallerType);
        Assert.Equal(AuditCallerSource.Unknown, entry.CallerSource);
    }

    /// <summary>An anonymous request is equally not a user — there is no principal to name.</summary>
    [Fact]
    public void AnonymousRequest_IsUnknown_NotUser()
    {
        var entry = Build(Accessor(new ClaimsPrincipal(new ClaimsIdentity())));

        Assert.Equal(AuditCallerType.Unknown, entry.CallerType);
    }

    [Fact]
    public void ApiKeyScheme_IsApiKey()
    {
        var entry = Build(Accessor(Principal(ApiKeyConstants.SchemeName)));

        Assert.Equal(AuditCallerType.ApiKey, entry.CallerType);
        Assert.Equal(AuditCallerSource.Api, entry.CallerSource);
    }

    [Theory]
    [InlineData("Cookies")]
    [InlineData("AuthenticationTypes.Federation")]
    public void WebScheme_IsUser(string authenticationType)
    {
        var entry = Build(Accessor(Principal(authenticationType)));

        Assert.Equal(AuditCallerType.User, entry.CallerType);
        Assert.Equal(AuditCallerSource.Web, entry.CallerSource);
    }

    /// <summary>
    /// An authenticated principal arriving under a scheme we do not recognise is still a person — the
    /// source is unknown, the actor is not. Only the absence of a principal makes the caller unknown.
    /// </summary>
    [Fact]
    public void AuthenticatedUnderAnUnrecognisedScheme_IsStillAUser()
    {
        var entry = Build(Accessor(Principal("SomeCustomScheme")));

        Assert.Equal(AuditCallerType.User, entry.CallerType);
        Assert.Equal(AuditCallerSource.Unknown, entry.CallerSource);
    }

    /// <summary>
    /// The stable identifier. <see cref="AuditEntry.CallerIdentity"/> resolves through a fallback chain
    /// and is matched by substring, so a dialog pinned to an email finds nothing when the identity
    /// provider put a display name in the name claim. This one is the subject or nothing.
    /// </summary>
    [Fact]
    public void CallerUserIdentity_IsTheSubject_NotTheDisplayName()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "Ada Lovelace"),
            new Claim(ClaimTypes.NameIdentifier, "sub-123")
        ], "Cookies"));

        var entry = Build(Accessor(principal));

        Assert.Equal("Ada Lovelace", entry.CallerIdentity);
        Assert.Equal("sub-123", entry.CallerUserIdentity);
    }

    /// <summary>
    /// No fallback chain, deliberately. A field that is sometimes the subject and sometimes a display
    /// name cannot be matched exactly, which is the whole defect being fixed.
    /// </summary>
    [Fact]
    public void CallerUserIdentity_IsNull_WhenNoSubjectClaimIsPresent()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Ada Lovelace")], "Cookies"));

        var entry = Build(Accessor(principal));

        Assert.Equal("Ada Lovelace", entry.CallerIdentity);
        Assert.Null(entry.CallerUserIdentity);
    }

    [Fact]
    public void CallerUserIdentity_IsNull_ForACallerWithNoPrincipal()
    {
        Assert.Null(Build(httpContextAccessor: null).CallerUserIdentity);
    }

    private static AuditEntry Build(IHttpContextAccessor httpContextAccessor)
        => AuditHelper.BuildEntry(httpContextAccessor, "team", "create", "CreateTeamAsync", 1, true);

    private static ClaimsPrincipal Principal(string authenticationType)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "someone@example.com")], authenticationType));

    private static IHttpContextAccessor Accessor(ClaimsPrincipal principal)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = principal });
        return accessor;
    }
}
