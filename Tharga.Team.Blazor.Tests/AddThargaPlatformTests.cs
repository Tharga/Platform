using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Blazor.Features.BreadCrumbs;
using Tharga.Blazor.Framework;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.Service;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Tests;

public class AddThargaPlatformTests
{
    private const string ValidAzureAdConfig = """
        {
            "AzureAd": {
                "Authority": "https://test.ciamlogin.com/test",
                "ClientId": "test-client-id",
                "TenantId": "test-tenant-id",
                "CallbackPath": "/signin-oidc"
            }
        }
        """;

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ValidAzureAdConfig));
        builder.Configuration.AddJsonStream(stream);
        return builder;
    }

    [Fact]
    public void RegistersAuthenticationServices()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();
        var provider = builder.Services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAuthenticationService>());
    }

    [Fact]
    public void RegistersBreadCrumbService()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();
        var provider = builder.Services.BuildServiceProvider();

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(BreadCrumbService));
    }

    [Fact]
    public void RegistersBlazorOptions()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.Blazor.Title = "Test App");
        var provider = builder.Services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<BlazorOptions>>();
        Assert.Equal("Test App", options.Value.Title);
    }

    [Fact]
    public void RegistersApiKeyServices()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IApiKeyRepository));
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IApiKeyRepositoryCollection));
    }

    [Fact]
    public void RegistersControllerServices()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ThargaControllerOptions));
    }

    [Fact]
    public void SkipsControllers_WhenNull()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.Controllers = null);

        Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(ThargaControllerOptions));
    }

    [Fact]
    public void RegistersScopes_WhenConfigured()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.ConfigureScopes = scopes => scopes.Register("test:read", AccessLevel.Viewer);
        });
        var provider = builder.Services.BuildServiceProvider();

        var registry = provider.GetService<IScopeRegistry>();
        Assert.NotNull(registry);
        Assert.Single(registry.All);
    }

    [Fact]
    public void SkipsScopes_WhenNotConfigured()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();

        Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(IScopeRegistry));
    }

    [Fact]
    public void RegistersTenantRoles_WhenConfigured()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.ConfigureScopes = scopes => scopes.Register("test:read", AccessLevel.Viewer);
            o.ConfigureTenantRoles = roles => roles.Register("Editor", new[] { "test:read" });
        });
        var provider = builder.Services.BuildServiceProvider();

        var registry = provider.GetService<ITenantRoleRegistry>();
        Assert.NotNull(registry);
    }

    [Fact]
    public void SkipsTenantRoles_WhenNotConfigured()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();

        Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(ITenantRoleRegistry));
    }

    [Fact]
    public void RegistersAuditLogging_WhenConfigured()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.Audit = new AuditOptions());

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(CompositeAuditLogger));
    }

    [Fact]
    public void SkipsAuditLogging_WhenNotConfigured()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();

        Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(CompositeAuditLogger));
    }

    [Fact]
    public void SkipsApiKeyAuth_WhenNull()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.ApiKey = null);

        Assert.DoesNotContain(builder.Services, d =>
            d.ServiceType == typeof(IApiKeyRepository));
    }

    [Fact]
    public void RegistersHttpContextAccessor()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();
        var provider = builder.Services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IHttpContextAccessor>());
    }

    [Fact]
    public void UseThargaPlatform_MapsAuthEndpoints()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();
        var app = builder.Build();

        app.UseThargaPlatform();

        var endpoints = ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .ToList();

        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "/login");
        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "/logout");
    }
}
