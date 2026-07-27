using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Blazor.Features.BreadCrumbs;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The deprecated <c>AddThargaPlatform</c> entry point must keep working until 4.0, and must produce the
/// same registrations as <c>AddThargaTeam</c> — a forwarding alias that quietly did less would be worse
/// than no alias at all. Deleted alongside the shim in 4.0.
/// </summary>
#pragma warning disable CS0618 // Testing the obsolete surface is the entire point of this file.
public class ThargaPlatformCompatibilityTests
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
    public void AddThargaPlatform_StillRegistersAuthentication()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();
        var provider = builder.Services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAuthenticationService>());
    }

    [Fact]
    public void AddThargaPlatform_StillHonoursOptions()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o => o.EnableDynamicRoles = true);

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ITenantRoleService));
    }

    [Fact]
    public void AddThargaPlatform_RegistersTheSameServicesAsAddThargaTeam()
    {
        var viaObsolete = CreateBuilder();
        viaObsolete.AddThargaPlatform();

        var viaCurrent = CreateBuilder();
        viaCurrent.AddThargaTeam();

        var obsoleteTypes = viaObsolete.Services.Select(d => d.ServiceType.FullName).OrderBy(x => x);
        var currentTypes = viaCurrent.Services.Select(d => d.ServiceType.FullName).OrderBy(x => x);

        Assert.Equal(currentTypes, obsoleteTypes);
    }

    [Fact]
    public void ThargaPlatformOptions_IsAThargaTeamOptions()
    {
        Assert.IsAssignableFrom<ThargaTeamOptions>(new ThargaPlatformOptions());
    }
}
#pragma warning restore CS0618
