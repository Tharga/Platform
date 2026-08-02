using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Tharga.Team.Support.Slack;

namespace Tharga.Team.Support.Tests;

/// <summary>
/// The transport, against a stubbed HTTP handler.
/// </summary>
public class SlackClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; private set; }
        public string LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (SlackClient Client, StubHandler Handler) Build(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        string token = "xoxb-test")
    {
        var handler = new StubHandler(respond);
        var client = new SlackClient(new StubFactory(handler), Options.Create(new SlackOptions { BotToken = token }));
        return (client, handler);
    }

    [Fact]
    public async Task APostSlackAccepts_Succeeds()
    {
        var (client, handler) = Build(_ => Json(HttpStatusCode.OK, """{"ok":true}"""));

        var result = await client.PostAsync("#teams", "hello");

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Contains("\"channel\":\"#teams\"", handler.LastBody);
        Assert.Contains("\"text\":\"hello\"", handler.LastBody);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("xoxb-test", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Equal("https://slack.com/api/chat.postMessage", handler.LastRequest.RequestUri?.ToString());
    }

    /// <summary>
    /// The trap this transport exists to avoid. Slack reports a bad token, an uninvited bot and a rate
    /// limit as <c>200 OK</c> with <c>ok:false</c>, so a client that checked only the status code would
    /// call every one of those a successful post and no one would ever learn the channel was silent.
    /// </summary>
    [Theory]
    [InlineData("invalid_auth")]
    [InlineData("channel_not_found")]
    [InlineData("not_in_channel")]
    [InlineData("ratelimited")]
    public async Task A200ThatSaysNotOk_IsAFailure(string error)
    {
        var (client, _) = Build(_ => Json(HttpStatusCode.OK, $$"""{"ok":false,"error":"{{error}}"}"""));

        var result = await client.PostAsync("#teams", "hello");

        Assert.False(result.Success);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task A200ThatSaysNotOkWithNoReason_StillFails()
    {
        var (client, _) = Build(_ => Json(HttpStatusCode.OK, """{"ok":false}"""));

        Assert.False((await client.PostAsync("#teams", "hello")).Success);
    }

    [Fact]
    public async Task AnHttpError_IsAFailure()
    {
        var (client, _) = Build(_ => Json(HttpStatusCode.InternalServerError, "{}"));

        var result = await client.PostAsync("#teams", "hello");

        Assert.False(result.Success);
        Assert.Contains("500", result.Error);
    }

    /// <summary>An unconfigured host is the expected state, and it must not throw on every event.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WithNoToken_NothingIsSentAndNothingThrows(string token)
    {
        var (client, handler) = Build(_ => Json(HttpStatusCode.OK, """{"ok":true}"""), token);

        var result = await client.PostAsync("#teams", "hello");

        Assert.False(result.Success);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task ANetworkFailure_IsReportedRatherThanThrown()
    {
        var (client, _) = Build(_ => throw new HttpRequestException("no route to host"));

        var result = await client.PostAsync("#teams", "hello");

        Assert.False(result.Success);
        Assert.Contains("no route to host", result.Error);
    }

    [Theory]
    [InlineData(null, "hello")]
    [InlineData("", "hello")]
    [InlineData("#teams", null)]
    [InlineData("#teams", "")]
    public async Task AnIncompleteMessage_IsNotSent(string channel, string text)
    {
        var (client, handler) = Build(_ => Json(HttpStatusCode.OK, """{"ok":true}"""));

        Assert.False((await client.PostAsync(channel, text)).Success);
        Assert.Null(handler.LastRequest);
    }
}
