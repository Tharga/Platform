using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Mcp;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Mcp.Tests;

/// <summary>
/// End-to-end proof that <c>AddTeam()</c> contributing the API-key scheme actually lets an agent
/// authenticate — the half of PR #169 that no test covered.
/// </summary>
/// <remarks>
/// <b>Why this exists.</b> <c>AddTeamTests</c> asserts the scheme lands in
/// <c>ThargaMcpOptions.AuthenticationSchemes</c>. That proves the wiring exists, not that it works:
/// the contribution only matters if <c>UseThargaMcp()</c> then builds its policy from that list and the
/// handler authenticates against it. Nothing tested the second half, and the one consumer on 3.8.2+ sets
/// <c>RequireAuth = false</c> and applies its own policy, so it never executes this path either.
/// <para>
/// <b>The verb matters.</b> These assert <c>POST</c>. MCP is JSON-RPC over POST, and a <c>GET</c> returns
/// 404 from routing finding no verb match — which is exactly the false alarm that was once escalated as a
/// toolkit defect. A test using the wrong verb would reproduce that confusion rather than test anything.
/// </para>
/// </remarks>
public class McpApiKeyAuthenticationTests
{
    /// <summary>Stands in for a host's interactive sign-in — cookies here, OIDC in a real one.</summary>
    private const string InteractiveScheme = "Interactive";

    private const string ValidTeamKey = "valid-team-key";
    private const string ValidSystemKey = "valid-system-key";

    private sealed record FakeApiKey(string Key, string Name, string TeamKey, string[] SystemScopes) : IApiKey
    {
        public string ApiKey => null;
        public string CreatedBy => null;
        public string OwnerMemberKey => null;
        public IReadOnlyList<Tag> Tags => [];
        public AccessLevel? AccessLevel => Tharga.Team.AccessLevel.Administrator;
        public string[] Roles => [];
        public string[] ScopeOverrides => [];
        public DateTime? ExpiryDate => null;
        public DateTime? CreatedAt => DateTime.UtcNow;
        public DateTime? LastUsedAt => null;
    }

    /// <summary>
    /// Stands in for the key store. The handler takes <see cref="IApiKeyAdministrationService"/> as an
    /// interface, which is what makes this testable without a database.
    /// </summary>
    private sealed class FakeApiKeyStore : IApiKeyAdministrationService
    {
        public Task<IApiKey> GetByApiKeyAsync(string apiKey) => Task.FromResult<IApiKey>(apiKey switch
        {
            ValidTeamKey => new FakeApiKey("key-1", "Team key", "team-1", null),
            ValidSystemKey => new FakeApiKey("key-2", "System key", null, ["mcp:discover"]),
            _ => null
        });

        // Authentication reads one member. The rest throw rather than returning empty, so a test that
        // starts depending on them fails loudly instead of quietly asserting against nothing.
        private static T NotUsed<T>() => throw new NotSupportedException("Not part of the authentication path.");

        public IAsyncEnumerable<IApiKey> GetKeysAsync(string teamKey) => NotUsed<IAsyncEnumerable<IApiKey>>();
        public Task<IApiKey> CreateKeyAsync(string teamKey, string name, AccessLevel accessLevel, string[] roles = null, string[] scopeOverrides = null, DateTime? expiryDate = null, IReadOnlyList<Tag> tags = null, string createdBy = null, string ownerMemberKey = null) => NotUsed<Task<IApiKey>>();
        public Task<IApiKey> RefreshKeyAsync(string teamKey, string key) => NotUsed<Task<IApiKey>>();
        public Task LockKeyAsync(string teamKey, string key) => NotUsed<Task>();
        public Task DeleteKeyAsync(string teamKey, string key) => NotUsed<Task>();
        public Task SetScopeOverridesAsync(string teamKey, string key, string[] scopes) => NotUsed<Task>();
        public Task SetRolesAsync(string teamKey, string key, string[] roles) => NotUsed<Task>();
        public IAsyncEnumerable<IApiKey> GetSystemKeysAsync() => NotUsed<IAsyncEnumerable<IApiKey>>();
        public Task<IApiKey> CreateSystemKeyAsync(string name, string[] scopes, DateTime? expiryDate = null, string createdBy = null) => NotUsed<Task<IApiKey>>();
        public Task<IApiKey> RefreshSystemKeyAsync(string key) => NotUsed<Task<IApiKey>>();
        public Task LockSystemKeyAsync(string key) => NotUsed<Task>();
        public Task DeleteSystemKeyAsync(string key) => NotUsed<Task>();
    }

    private static async Task<IHost> StartHostAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();

                    // The default scheme is deliberately NOT the API-key one. That is the condition the
                    // fix addresses and the only one under which it can be observed: in a real host the
                    // default is interactive sign-in, so a bare RequireAuthorization() resolves to that
                    // and answers an agent with a redirect. Making the API-key scheme the default here
                    // would make the test pass with or without the contribution — it did, on the first
                    // attempt, which is why this comment exists.
                    services.AddAuthentication(InteractiveScheme)
                        .AddCookie(InteractiveScheme)
                        .AddThargaApiKeyAuthentication<FakeApiKeyStore>();

                    // RequireAuth left at its default — the whole point. A host that overrides it with its
                    // own policy never executes the path under test.
                    services.AddThargaMcp(mcp => mcp.AddTeam());
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.UseThargaMcp());
                });
            })
            .StartAsync();

        return host;
    }

    private static HttpRequestMessage McpRequest(string apiKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""",
                System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (apiKey != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    /// <summary>Anonymous is refused — the endpoint is gated at all.</summary>
    [Fact]
    public async Task Anonymous_IsRejected()
    {
        using var host = await StartHostAsync();
        using var client = host.GetTestClient();

        using var response = await client.SendAsync(McpRequest());

        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Found,
            $"Expected the endpoint to refuse an anonymous caller, got {(int)response.StatusCode}.");
    }

    /// <summary>
    /// The assertion PR #169 exists for: a team API key gets through the default <c>RequireAuth</c>
    /// policy. Before the scheme contribution the policy fell back to the application's default scheme,
    /// and an agent was answered with a redirect or a 401 it could do nothing about.
    /// </summary>
    [Fact]
    public async Task TeamApiKey_IsAccepted()
    {
        using var host = await StartHostAsync();
        using var client = host.GetTestClient();

        using var response = await client.SendAsync(McpRequest(ValidTeamKey));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
    }

    /// <summary>
    /// A system key is accepted on the same endpoint. Neither named policy admits both kinds — that is
    /// what forces a host to hand-roll one — but the <c>RequireAuth</c> policy asserts nothing about
    /// <c>IsSystemKey</c>, so both get through.
    /// </summary>
    [Fact]
    public async Task SystemApiKey_IsAccepted()
    {
        using var host = await StartHostAsync();
        using var client = host.GetTestClient();

        using var response = await client.SendAsync(McpRequest(ValidSystemKey));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
    }

    /// <summary>An unknown key is refused rather than treated as anonymous-but-allowed.</summary>
    [Fact]
    public async Task UnknownApiKey_IsRejected()
    {
        using var host = await StartHostAsync();
        using var client = host.GetTestClient();

        using var response = await client.SendAsync(McpRequest("not-a-key"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
