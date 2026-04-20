using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class SystemApiKeyAuthenticationHandlerTests
{
    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();

    private async Task<ApiKeyAuthenticationHandler> CreateHandler(HttpContext httpContext)
    {
        var options = new AuthenticationSchemeOptions();
        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Get(ApiKeyConstants.SchemeName).Returns(options);

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var handler = new ApiKeyAuthenticationHandler(optionsMonitor, loggerFactory, UrlEncoder.Default, _apiKeyService);
        var scheme = new AuthenticationScheme(ApiKeyConstants.SchemeName, "API Key", typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);
        return handler;
    }

    private static HttpContext CreateHttpContext(string apiKey)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[ApiKeyConstants.HeaderName] = apiKey;
        return ctx;
    }

    private static IApiKey CreateSystemKey(string name, string[] scopes)
    {
        var key = Substitute.For<IApiKey>();
        key.TeamKey.Returns((string)null);
        key.Name.Returns(name);
        key.SystemScopes.Returns(scopes);
        key.Tags.Returns(new Dictionary<string, string>());
        return key;
    }

    [Fact]
    public async Task SystemKey_Populates_IsSystemKey_Claim_And_No_TeamKey()
    {
        var sysKey = CreateSystemKey("mcp-gate", new[] { "mcp:discover" });
        _apiKeyService.GetByApiKeyAsync("raw").Returns(sysKey);
        var ctx = CreateHttpContext("raw");
        var handler = await CreateHandler(ctx);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var principal = result.Principal!;
        Assert.True(principal.HasClaim(TeamClaimTypes.IsSystemKey, "true"));
        Assert.Null(principal.FindFirst(TeamClaimTypes.TeamKey)?.Value);
    }

    [Fact]
    public async Task SystemKey_Scopes_Come_From_SystemScopes_Not_Registry()
    {
        var sysKey = CreateSystemKey("mcp-gate", new[] { "mcp:discover", "mcp:mongodb:read" });
        _apiKeyService.GetByApiKeyAsync("raw").Returns(sysKey);
        var ctx = CreateHttpContext("raw");
        var handler = await CreateHandler(ctx);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var scopes = result.Principal!.Claims.Where(c => c.Type == TeamClaimTypes.Scope).Select(c => c.Value).ToArray();
        Assert.Contains("mcp:discover", scopes);
        Assert.Contains("mcp:mongodb:read", scopes);
    }

    [Fact]
    public async Task TeamKey_Does_Not_Get_IsSystemKey_Claim()
    {
        var teamKey = Substitute.For<IApiKey>();
        teamKey.TeamKey.Returns("team-1");
        teamKey.Name.Returns("team-bound");
        teamKey.Tags.Returns(new Dictionary<string, string>());
        _apiKeyService.GetByApiKeyAsync("raw").Returns(teamKey);

        var ctx = CreateHttpContext("raw");
        var handler = await CreateHandler(ctx);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.False(result.Principal!.HasClaim(TeamClaimTypes.IsSystemKey, "true"));
        Assert.Equal("team-1", result.Principal.FindFirst(TeamClaimTypes.TeamKey)?.Value);
    }

    [Fact]
    public async Task SystemKey_With_Null_Scopes_Is_Authenticated_With_No_Scope_Claims()
    {
        var sysKey = CreateSystemKey("minimal", null);
        _apiKeyService.GetByApiKeyAsync("raw").Returns(sysKey);
        var ctx = CreateHttpContext("raw");
        var handler = await CreateHandler(ctx);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Principal!.Claims.Where(c => c.Type == TeamClaimTypes.Scope));
    }
}
