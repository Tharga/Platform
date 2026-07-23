using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Icon wiring by <c>AddThargaPlatform</c>: the resolver resolves, the built-in <c>StoredIconSource</c> is
/// registered first (so a platform-stored icon wins), a custom store/source registered via the options
/// takes effect, and <see cref="IconOptions"/> is bound from <c>o.Icon</c>.
/// </summary>
public class IconRegistrationTests
{
    private const string ValidAzureAdConfig = """
        { "AzureAd": { "Authority": "https://test.ciamlogin.com/test", "ClientId": "c", "TenantId": "t", "CallbackPath": "/signin-oidc" } }
        """;

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ValidAzureAdConfig));
        builder.Configuration.AddJsonStream(stream);
        return builder;
    }

    private sealed class FakeStore : IIconStore
    {
        public Task<string> SaveAsync(IconKind kind, string ownerKey, byte[] data, string contentType, CancellationToken cancellationToken = default) => Task.FromResult("x");
        public Task<IconContent> LoadAsync(string reference, CancellationToken cancellationToken = default) => Task.FromResult<IconContent>(null);
        public Task DeleteAsync(string reference, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeSource : IIconSource
    {
        public Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default) => Task.FromResult<IconImage>(null);
    }

    [Fact]
    public void Resolver_And_StoredSource_AreRegistered()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();
        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IIconResolver>());
        var sources = scope.ServiceProvider.GetServices<IIconSource>().ToList();
        Assert.NotEmpty(sources);
        Assert.IsType<StoredIconSource>(sources[0]);
    }

    [Fact]
    public void CustomSource_RegisteredAfterStoredSource()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.AddIconSource<FakeSource>());
        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var sources = scope.ServiceProvider.GetServices<IIconSource>().ToList();

        Assert.IsType<StoredIconSource>(sources[0]);
        Assert.Contains(sources, s => s is FakeSource);
        Assert.True(sources.IndexOf(sources.First(s => s is StoredIconSource)) < sources.IndexOf(sources.First(s => s is FakeSource)));
    }

    [Fact]
    public void CustomStore_Wins()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.AddIconStore<FakeStore>());
        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<FakeStore>(scope.ServiceProvider.GetService<IIconStore>());
    }

    [Fact]
    public void IconOptions_BoundFromPlatformOptions()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.Icon.MaxBytes = 12345);
        using var provider = builder.Services.BuildServiceProvider();

        Assert.Equal(12345, provider.GetRequiredService<IOptions<IconOptions>>().Value.MaxBytes);
    }
}
