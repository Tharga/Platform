using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();

    private async Task<ApiKeyAuthenticationHandler> CreateHandler(HttpContext httpContext)
    {
        var options = new AuthenticationSchemeOptions();
        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Get(ApiKeyConstants.SchemeName).Returns(options);

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var handler = new ApiKeyAuthenticationHandler(
            optionsMonitor,
            loggerFactory,
            UrlEncoder.Default,
            _apiKeyService);

        var scheme = new AuthenticationScheme(ApiKeyConstants.SchemeName, "API Key", typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);

        return handler;
    }

    private static HttpContext CreateHttpContext(string apiKeyHeaderValue = null, string authorizationHeaderValue = null)
    {
        var context = new DefaultHttpContext();
        if (apiKeyHeaderValue != null)
        {
            context.Request.Headers[ApiKeyConstants.HeaderName] = apiKeyHeaderValue;
        }
        if (authorizationHeaderValue != null)
        {
            context.Request.Headers["Authorization"] = authorizationHeaderValue;
        }
        return context;
    }

    private static IApiKey CreateApiKey(string teamKey, string name = "Test Key", Dictionary<string, string> tags = null)
    {
        var apiKey = Substitute.For<IApiKey>();
        apiKey.TeamKey.Returns(teamKey);
        apiKey.Name.Returns(name);
        apiKey.Tags.Returns(tags ?? new Dictionary<string, string>());
        return apiKey;
    }

    [Fact]
    public async Task Without_Header_Returns_NoResult()
    {
        var context = CreateHttpContext();
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task With_Empty_Header_Returns_NoResult()
    {
        var context = CreateHttpContext("");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task With_Whitespace_Header_Returns_NoResult()
    {
        var context = CreateHttpContext("   ");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task With_Invalid_ApiKey_Returns_Fail()
    {
        _apiKeyService.GetByApiKeyAsync("invalid-key").Returns(Task.FromResult<IApiKey>(null));

        var context = CreateHttpContext("invalid-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.None);
        Assert.Contains("Invalid", result.Failure.Message);
    }

    [Fact]
    public async Task With_Valid_ApiKey_Returns_Success_With_Claims()
    {
        var apiKey = CreateApiKey("team-123");
        _apiKeyService.GetByApiKeyAsync("valid-key").Returns(Task.FromResult(apiKey));

        var context = CreateHttpContext("valid-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);

        var teamKeyClaim = result.Principal.FindFirst(TeamClaimTypes.TeamKey);
        Assert.NotNull(teamKeyClaim);
        Assert.Equal("team-123", teamKeyClaim.Value);

        var nameClaim = result.Principal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("Test Key", nameClaim.Value);
    }

    [Fact]
    public async Task With_Valid_ApiKey_And_Null_Name_Uses_TeamKey_As_Name()
    {
        var apiKey = CreateApiKey("team-456", name: null);
        _apiKeyService.GetByApiKeyAsync("valid-key").Returns(Task.FromResult(apiKey));

        var context = CreateHttpContext("valid-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var nameClaim = result.Principal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("team-456", nameClaim.Value);
    }

    [Fact]
    public async Task With_Valid_ApiKey_Without_AccessLevel_Defaults_To_Viewer()
    {
        var apiKey = CreateApiKey("team-123");
        _apiKeyService.GetByApiKeyAsync("valid-key").Returns(Task.FromResult(apiKey));

        var context = CreateHttpContext("valid-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var accessLevelClaim = result.Principal.FindFirst(TeamClaimTypes.AccessLevel);
        Assert.NotNull(accessLevelClaim);
        Assert.Equal("Viewer", accessLevelClaim.Value);
    }

    [Fact]
    public async Task With_Bearer_Token_Returns_Success_With_Claims()
    {
        var apiKey = CreateApiKey("team-bearer");
        _apiKeyService.GetByApiKeyAsync("bearer-key").Returns(Task.FromResult(apiKey));

        var context = CreateHttpContext(authorizationHeaderValue: "Bearer bearer-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var teamKeyClaim = result.Principal.FindFirst(TeamClaimTypes.TeamKey);
        Assert.NotNull(teamKeyClaim);
        Assert.Equal("team-bearer", teamKeyClaim.Value);
    }

    [Fact]
    public async Task With_Bearer_Token_Lowercase_Scheme_Returns_Success()
    {
        var apiKey = CreateApiKey("team-lower");
        _apiKeyService.GetByApiKeyAsync("lower-key").Returns(Task.FromResult(apiKey));

        var context = CreateHttpContext(authorizationHeaderValue: "bearer lower-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task With_Bearer_And_XApiKey_Bearer_Takes_Precedence()
    {
        var bearerKey = CreateApiKey("team-bearer-wins");
        _apiKeyService.GetByApiKeyAsync("bearer-token").Returns(Task.FromResult(bearerKey));
        _apiKeyService.GetByApiKeyAsync("xapikey-token").Returns(Task.FromResult<IApiKey>(null));

        var context = CreateHttpContext(
            apiKeyHeaderValue: "xapikey-token",
            authorizationHeaderValue: "Bearer bearer-token");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("team-bearer-wins", result.Principal.FindFirst(TeamClaimTypes.TeamKey)?.Value);
    }

    [Fact]
    public async Task With_Non_Bearer_Authorization_Falls_Back_To_XApiKey()
    {
        var apiKey = CreateApiKey("team-fallback");
        _apiKeyService.GetByApiKeyAsync("xapi-fallback").Returns(Task.FromResult(apiKey));

        // Basic auth (or anything not "Bearer ") should be ignored; X-API-KEY remains the source.
        var context = CreateHttpContext(
            apiKeyHeaderValue: "xapi-fallback",
            authorizationHeaderValue: "Basic some-other-credential");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("team-fallback", result.Principal.FindFirst(TeamClaimTypes.TeamKey)?.Value);
    }

    [Fact]
    public async Task With_Only_Non_Bearer_Authorization_Returns_NoResult()
    {
        var context = CreateHttpContext(authorizationHeaderValue: "Basic abc");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task With_Empty_Bearer_Token_Returns_NoResult()
    {
        var context = CreateHttpContext(authorizationHeaderValue: "Bearer ");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task With_NonEntity_ApiKey_Uses_Typed_AccessLevel()
    {
        // A custom IApiKey (not ApiKeyEntity) resolves access level from its typed property, not Tags.
        var apiKey = CreateApiKey("team-123");
        apiKey.AccessLevel.Returns(AccessLevel.User);
        _apiKeyService.GetByApiKeyAsync("typed-key").Returns(Task.FromResult(apiKey));

        var context = CreateHttpContext("typed-key");
        var handler = await CreateHandler(context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var accessLevelClaim = result.Principal.FindFirst(TeamClaimTypes.AccessLevel);
        Assert.NotNull(accessLevelClaim);
        Assert.Equal("User", accessLevelClaim.Value);
    }
}
