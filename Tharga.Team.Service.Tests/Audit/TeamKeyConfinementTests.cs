using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// <b>I2.</b> A team API key never reaches another team — even when it names that team explicitly.
/// Knowing a team key is not authority over it.
/// </summary>
/// <remarks>
/// These drive the real <see cref="ApiKeyAuthenticationHandler"/> into the real
/// <see cref="AuditAccess.CanRead"/>, rather than asserting against a hand-built principal. That matters
/// here more than usual: <c>AuditAccessTests</c> already proves the rule is correct, so a hand-built
/// principal would only re-prove it. What was unproven is whether the principal a real key produces
/// carries the provenance the rule depends on — if the handler issued a system grant for a team key, the
/// rule would still be right and the system would still be wrong.
/// <para>
/// I2 was blocked three times on "mint a key on a second team" in the running sample. That made the
/// invariant depend on someone's sample data and proved it only until the next restart. Two keys with
/// different <c>TeamKey</c> values is a fixture.
/// </para>
/// </remarks>
public class TeamKeyConfinementTests
{
    private const string TeamA = "team-a";
    private const string TeamB = "team-b";

    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();

    /// <summary>
    /// The case that could not be tested while both sample keys shared a team: two keys, two teams, each
    /// confined to its own.
    /// </summary>
    [Fact]
    public async Task TwoKeysOnDifferentTeams_EachReadsOnlyItsOwn()
    {
        var callerA = await AuthenticateTeamKey("key-a", TeamA);
        var callerB = await AuthenticateTeamKey("key-b", TeamB);

        Assert.True(AuditAccess.CanRead(callerA, TeamA));
        Assert.True(AuditAccess.CanRead(callerB, TeamB));

        Assert.False(AuditAccess.CanRead(callerA, TeamB));
        Assert.False(AuditAccess.CanRead(callerB, TeamA));
    }

    /// <summary>
    /// I2 stated directly. The caller names the other team — the one piece of information an attacker
    /// most plausibly has, since team keys appear in URLs and configuration — and is still refused.
    /// </summary>
    [Fact]
    public async Task NamingAnotherTeamExplicitly_IsRefused()
    {
        var caller = await AuthenticateTeamKey("key-a", TeamA);

        Assert.False(AuditAccess.CanRead(caller, TeamB));
    }

    /// <summary>
    /// <b>I1</b>, end-to-end rather than against the rule alone: a team grant must not span the system.
    /// Reading with no team named is a query across every team.
    /// </summary>
    [Fact]
    public async Task ATeamKey_CannotReadAcrossAllTeams()
    {
        var caller = await AuthenticateTeamKey("key-a", TeamA);

        Assert.False(AuditAccess.CanRead(caller, teamKey: null));
    }

    /// <summary>
    /// The mechanism behind the three tests above, asserted separately so a regression reports its cause.
    /// A team key's scopes are team grants and must carry the team claim type; issuing them as system
    /// grants would defeat every confinement test at once while each looked like an unrelated failure.
    /// </summary>
    [Fact]
    public async Task ATeamKeysScopes_CarryTeamProvenanceAndNeverSystem()
    {
        var caller = await AuthenticateTeamKey("key-a", TeamA);

        Assert.Contains(caller.Claims, c => c.Type == TeamClaimTypes.Scope && c.Value == AuditScopes.Read);
        Assert.DoesNotContain(caller.Claims, c => c.Type == TeamClaimTypes.SystemScope);
        Assert.Empty(caller.FindAll(TeamClaimTypes.IsSystemKey));
    }

    /// <summary>
    /// The team a key acts for comes from the key record. Nothing in the request contributes to it, which
    /// is what makes naming another team futile rather than merely refused.
    /// </summary>
    [Fact]
    public async Task TheTeamComesFromTheKeyRecord()
    {
        var caller = await AuthenticateTeamKey("key-a", TeamA);

        var teamKeys = caller.FindAll(TeamClaimTypes.TeamKey).Select(c => c.Value).ToArray();

        Assert.Equal([TeamA], teamKeys);
    }

    /// <summary>
    /// The contrast that shows the two branches are genuinely exclusive: a system key carries system
    /// provenance and no team at all, so it can never satisfy a team-scoped check.
    /// </summary>
    [Fact]
    public async Task ASystemKey_CarriesSystemProvenanceAndNoTeam()
    {
        var caller = await AuthenticateSystemKey("key-s", AuditScopes.Read);

        Assert.Empty(caller.FindAll(TeamClaimTypes.TeamKey));
        Assert.Contains(caller.Claims, c => c.Type == TeamClaimTypes.SystemScope && c.Value == AuditScopes.Read);
        Assert.DoesNotContain(caller.Claims, c => c.Type == TeamClaimTypes.Scope);

        Assert.True(AuditAccess.CanRead(caller, teamKey: null));
    }

    /// <summary>
    /// Confinement is not the only gate. <c>audit:read</c> is registered at
    /// <see cref="AccessLevel.Administrator"/>, so a lower-level key is refused even for its own team —
    /// holding a team's credential is not the same as holding every grant within it.
    /// </summary>
    [Theory]
    [InlineData(AccessLevel.Viewer)]
    [InlineData(AccessLevel.User)]
    public async Task AKeyBelowAdministrator_CannotReadItsOwnTeam(AccessLevel accessLevel)
    {
        var caller = await AuthenticateTeamKey("key-a", TeamA, accessLevel);

        Assert.False(AuditAccess.CanRead(caller, TeamA));
    }

    [Theory]
    [InlineData(AccessLevel.Administrator)]
    [InlineData(AccessLevel.Owner)]
    public async Task AKeyAtAdministratorOrAbove_ReadsItsOwnTeam(AccessLevel accessLevel)
    {
        var caller = await AuthenticateTeamKey("key-a", TeamA, accessLevel);

        Assert.True(AuditAccess.CanRead(caller, TeamA));
    }

    private Task<ClaimsPrincipal> AuthenticateTeamKey(string rawKey, string teamKey, AccessLevel accessLevel = AccessLevel.Administrator)
    {
        var apiKey = Substitute.For<IApiKey>();
        apiKey.TeamKey.Returns(teamKey);
        apiKey.Name.Returns($"Key for {teamKey}");
        apiKey.AccessLevel.Returns(accessLevel);
        apiKey.Tags.Returns(Array.Empty<Tag>());

        return Authenticate(rawKey, apiKey);
    }

    private Task<ClaimsPrincipal> AuthenticateSystemKey(string rawKey, params string[] systemScopes)
    {
        var apiKey = Substitute.For<IApiKey>();
        apiKey.TeamKey.Returns((string)null);
        apiKey.Name.Returns("System key");
        apiKey.SystemScopes.Returns(systemScopes);
        apiKey.Tags.Returns(Array.Empty<Tag>());

        return Authenticate(rawKey, apiKey);
    }

    private async Task<ClaimsPrincipal> Authenticate(string rawKey, IApiKey apiKey)
    {
        _apiKeyService.GetByApiKeyAsync(rawKey).Returns(Task.FromResult(apiKey));

        var context = new DefaultHttpContext();
        context.Request.Headers[ApiKeyConstants.HeaderName] = rawKey;

        var handler = await CreateHandler(context);
        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded, "the fixture must authenticate, or the confinement assertions prove nothing");

        return result.Principal;
    }

    private async Task<ApiKeyAuthenticationHandler> CreateHandler(HttpContext httpContext)
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Get(ApiKeyConstants.SchemeName).Returns(new AuthenticationSchemeOptions());

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var handler = new ApiKeyAuthenticationHandler(
            optionsMonitor,
            loggerFactory,
            UrlEncoder.Default,
            _apiKeyService,
            ScopeRegistry());

        var scheme = new AuthenticationScheme(ApiKeyConstants.SchemeName, "API Key", typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);

        return handler;
    }

    /// <summary>Registers <c>audit:read</c> at the level production registers it.</summary>
    private static ScopeRegistry ScopeRegistry()
    {
        var registry = new ScopeRegistry();
        registry.Register(AuditScopes.Read, AccessLevel.Administrator, "View the audit log.");
        return registry;
    }
}
