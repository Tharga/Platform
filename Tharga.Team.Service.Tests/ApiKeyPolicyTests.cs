using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// What each of the three API-key policies actually admits, asserted against a running endpoint.
/// </summary>
/// <remarks>
/// <b>The pair was disjoint and read like a hierarchy.</b> <c>SystemApiKeyPolicy</c> is not
/// "<c>ApiKeyPolicy</c> plus more" — the first refuses a system key, the second refuses a team key, and
/// ASP.NET Core <i>combines</i> policies when several are required. So an endpoint asking for both
/// admits nothing, which is what led a consumer to hand-write their own policy and, in doing so, to stop
/// exercising the toolkit's own path entirely.
/// <para>
/// This is a matrix rather than four separate assertions because the interesting property is the shape
/// of the whole table: exactly one column accepts both rows.
/// </para>
/// </remarks>
public class ApiKeyPolicyTests
{
    private const string TeamKeyValue = "team-key";
    private const string SystemKeyValue = "system-key";
    private const string InteractiveScheme = "Interactive";

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
        public DateTime? DisabledAt => null;
        public string DisabledBy => null;
    }

    private sealed class FakeApiKeyStore : IApiKeyAdministrationService
    {
        public Task<IApiKey> GetByApiKeyAsync(string apiKey) => Task.FromResult<IApiKey>(apiKey switch
        {
            TeamKeyValue => new FakeApiKey("k1", "Team", "team-1", null),
            SystemKeyValue => new FakeApiKey("k2", "System", null, ["something"]),
            _ => null
        });

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
        public Task SetKeyDisabledAsync(string teamKey, string key, bool disabled, string actor = null) => NotUsed<Task>();
        public Task SetSystemKeyDisabledAsync(string key, bool disabled, string actor = null) => NotUsed<Task>();
    }

    private static async Task<IHost> StartHostAsync()
        => await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthentication(InteractiveScheme)
                        .AddCookie(InteractiveScheme)
                        .AddThargaApiKeyAuthentication<FakeApiKeyStore>();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/team", () => Results.Ok()).RequireAuthorization(ApiKeyConstants.PolicyName);
                        endpoints.MapGet("/system", () => Results.Ok()).RequireAuthorization(ApiKeyConstants.SystemPolicyName);
                        endpoints.MapGet("/any", () => Results.Ok()).RequireAuthorization(ApiKeyConstants.AnyKeyPolicyName);

                        // The trap, asserted rather than described: requiring both admits nothing.
                        endpoints.MapGet("/both", () => Results.Ok())
                            .RequireAuthorization(ApiKeyConstants.PolicyName, ApiKeyConstants.SystemPolicyName);
                    });
                });
            })
            .StartAsync();

    private static async Task<HttpStatusCode> CallAsync(IHost host, string path, string apiKey)
    {
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (apiKey != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static bool Admitted(HttpStatusCode status) => status == HttpStatusCode.OK;

    /// <summary>
    /// The whole table. `/any` is the only column admitting both key kinds, and `/both` admits neither —
    /// which is the defect this feature closes.
    /// </summary>
    [Theory]
    [InlineData("/team", TeamKeyValue, true)]
    [InlineData("/team", SystemKeyValue, false)]
    [InlineData("/system", TeamKeyValue, false)]
    [InlineData("/system", SystemKeyValue, true)]
    [InlineData("/any", TeamKeyValue, true)]
    [InlineData("/any", SystemKeyValue, true)]
    [InlineData("/both", TeamKeyValue, false)]
    [InlineData("/both", SystemKeyValue, false)]
    public async Task EachPolicy_AdmitsWhatItSays(string path, string apiKey, bool expectAdmitted)
    {
        using var host = await StartHostAsync();

        var status = await CallAsync(host, path, apiKey);

        Assert.Equal(expectAdmitted, Admitted(status));
    }

    [Theory]
    [InlineData("/team")]
    [InlineData("/system")]
    [InlineData("/any")]
    public async Task EveryPolicy_RefusesAnonymous(string path)
    {
        using var host = await StartHostAsync();

        var status = await CallAsync(host, path, apiKey: null);

        Assert.False(Admitted(status));
    }

    [Theory]
    [InlineData("/team")]
    [InlineData("/system")]
    [InlineData("/any")]
    public async Task EveryPolicy_RefusesAnUnknownKey(string path)
    {
        using var host = await StartHostAsync();

        var status = await CallAsync(host, path, "not-a-key");

        Assert.False(Admitted(status));
    }
}
