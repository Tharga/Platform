using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class ApiKeyIdClaimTests
{
    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();

    private async Task<ApiKeyAuthenticationHandler> CreateHandler(HttpContext httpContext)
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Get(ApiKeyConstants.SchemeName).Returns(new AuthenticationSchemeOptions());

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

    [Fact]
    public async Task TeamKey_Authentication_Emits_ApiKeyId_Claim()
    {
        var key = Substitute.For<IApiKey>();
        key.Key.Returns("11111111-1111-1111-1111-111111111111");
        key.TeamKey.Returns("team-1");
        key.Name.Returns("team-key");
        key.Tags.Returns(new Dictionary<string, string>());
        _apiKeyService.GetByApiKeyAsync("raw").Returns(key);

        var handler = await CreateHandler(CreateHttpContext("raw"));
        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var claim = result.Principal!.FindFirst(TeamClaimTypes.ApiKeyId);
        Assert.NotNull(claim);
        Assert.Equal("11111111-1111-1111-1111-111111111111", claim.Value);
    }

    [Fact]
    public async Task SystemKey_Authentication_Emits_ApiKeyId_Claim()
    {
        var key = Substitute.For<IApiKey>();
        key.Key.Returns("22222222-2222-2222-2222-222222222222");
        key.TeamKey.Returns((string)null);
        key.Name.Returns("system-key");
        key.SystemScopes.Returns(new[] { "mcp:discover" });
        key.Tags.Returns(new Dictionary<string, string>());
        _apiKeyService.GetByApiKeyAsync("raw").Returns(key);

        var handler = await CreateHandler(CreateHttpContext("raw"));
        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        var claim = result.Principal!.FindFirst(TeamClaimTypes.ApiKeyId);
        Assert.NotNull(claim);
        Assert.Equal("22222222-2222-2222-2222-222222222222", claim.Value);
    }
}
