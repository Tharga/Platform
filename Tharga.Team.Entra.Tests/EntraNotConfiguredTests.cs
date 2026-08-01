using Microsoft.Extensions.Options;
using Tharga.Team.Entra;

namespace Tharga.Team.Entra.Tests;

/// <summary>
/// A half-configured directory has to say so before it is used. Previously it registered cleanly and
/// threw <c>InvalidOperationException</c> on the first Graph call — so the UI offered Verify and the
/// failure surfaced from a place that named neither the registration nor the missing setting.
/// </summary>
public class EntraNotConfiguredTests
{
    private static CredentialEntraTokenProvider Provider(EntraDirectoryOptions options)
        => new(Options.Create(options));

    [Fact]
    public void IsConfigured_AllThreeSecretFields_IsTrue()
    {
        var sut = Provider(new EntraDirectoryOptions { TenantId = "t", ClientId = "c", ClientSecret = "s" });

        Assert.True(sut.IsConfigured);
    }

    /// <summary>A custom credential replaces secret-based authentication entirely.</summary>
    [Fact]
    public void IsConfigured_CustomCredentialWithoutSecretFields_IsTrue()
    {
        var sut = Provider(new EntraDirectoryOptions { Credential = new FakeCredential() });

        Assert.True(sut.IsConfigured);
    }

    /// <summary>
    /// Every partial combination. The B2C case that prompted this is the missing-TenantId row: a B2C app
    /// registration has no TenantId key at all, so binding the <c>AzureAd</c> section leaves it null.
    /// </summary>
    [Theory]
    [InlineData(null, "c", "s")]
    [InlineData("t", null, "s")]
    [InlineData("t", "c", null)]
    [InlineData("", "c", "s")]
    [InlineData("t", "", "s")]
    [InlineData("t", "c", "")]
    [InlineData(null, null, null)]
    public void IsConfigured_IncompleteCredentials_IsFalse(string tenantId, string clientId, string clientSecret)
    {
        var sut = Provider(new EntraDirectoryOptions { TenantId = tenantId, ClientId = clientId, ClientSecret = clientSecret });

        Assert.False(sut.IsConfigured);
    }

    /// <summary>
    /// The two must agree. A provider reporting configured must not then throw, and one reporting
    /// unconfigured must not quietly work — they read the same fields through one helper, and this pins
    /// that they cannot drift apart.
    /// </summary>
    [Theory]
    [InlineData(null, "c", "s")]
    [InlineData("t", null, "s")]
    [InlineData("t", "c", null)]
    public async Task NotConfigured_StillThrowsIfCalledAnyway(string tenantId, string clientId, string clientSecret)
    {
        var sut = Provider(new EntraDirectoryOptions { TenantId = tenantId, ClientId = clientId, ClientSecret = clientSecret });

        Assert.False(sut.IsConfigured);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.GetTokenAsync());
        Assert.Contains("not configured", ex.Message);
    }

    /// <summary>
    /// The service reports what its token provider reports — that is the whole of its own configuration
    /// story, since the Graph address and scope both have working defaults.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DirectoryService_ReportsWhatTheTokenProviderReports(bool configured)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://graph.test/v1.0/") };
        var sut = new EntraUserDirectoryService(httpClient, new StubTokenProvider(configured));

        Assert.Equal(configured, sut.IsConfigured);
    }

    /// <summary>
    /// A provider written before <c>IsConfigured</c> existed must keep working. The default interface
    /// member is what makes adding it non-breaking for hosts with a custom token provider.
    /// </summary>
    [Fact]
    public void CustomProviderThatDoesNotImplementIsConfigured_DefaultsToConfigured()
    {
        IEntraTokenProvider sut = new LegacyTokenProvider();

        Assert.True(sut.IsConfigured);
    }

    private sealed class StubTokenProvider(bool isConfigured) : IEntraTokenProvider
    {
        public bool IsConfigured { get; } = isConfigured;
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("token");
    }

    private sealed class LegacyTokenProvider : IEntraTokenProvider
    {
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("token");
    }

    private sealed class FakeCredential : Azure.Core.TokenCredential
    {
        public override Azure.Core.AccessToken GetToken(Azure.Core.TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new("token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<Azure.Core.AccessToken> GetTokenAsync(Azure.Core.TokenRequestContext requestContext, CancellationToken cancellationToken)
            => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }
}
