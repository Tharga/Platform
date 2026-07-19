using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the opt-in <c>Consent.GrantTeamsRead</c> flag, which grants the configured consent roles
/// the <see cref="SystemTeamScopes.Read"/> system scope so they can enumerate every team. Default-off
/// deliberately: deriving a global privilege from the per-team consent list automatically would widen
/// access for existing hosts on upgrade.
/// </summary>
public class GrantTeamsReadTests
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

    private static IReadOnlyList<string> ScopesFor(WebApplicationBuilder builder, params string[] roles)
    {
        var registry = builder.Services.BuildServiceProvider().GetService<ISystemRoleRegistry>();
        return registry?.GetScopesForRoles(roles) ?? Array.Empty<string>();
    }

    [Fact]
    public void Default_DoesNotGrantTeamsRead()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform();

        Assert.DoesNotContain(SystemTeamScopes.Read, ScopesFor(builder, "Developer"));
    }

    [Fact]
    public void Enabled_GrantsTeamsReadToTheConsentRoles()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.Blazor.Consent.Roles = ["Developer", "Administrator"];
            o.Blazor.Consent.GrantTeamsRead = true;
        });

        Assert.Contains(SystemTeamScopes.Read, ScopesFor(builder, "Developer"));
        Assert.Contains(SystemTeamScopes.Read, ScopesFor(builder, "Administrator"));
    }

    [Fact]
    public void Enabled_DoesNotGrantToUnlistedRoles()
    {
        var builder = CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.Blazor.Consent.Roles = ["Developer"];
            o.Blazor.Consent.GrantTeamsRead = true;
        });

        Assert.DoesNotContain(SystemTeamScopes.Read, ScopesFor(builder, "Support"));
    }

    /// <summary>
    /// The common real-world shape: the host already maps the same role via ConfigureSystemRoles.
    /// SystemRoleRegistry.Map throws on a duplicate role, so the grant must merge (Grant) rather than
    /// map, or startup would crash for exactly the hosts most likely to enable this.
    /// </summary>
    [Fact]
    public void Enabled_ComposesWithAnExistingConfigureSystemRolesMapping()
    {
        var builder = CreateBuilder();
        var exception = Record.Exception(() => builder.AddThargaPlatform(o =>
        {
            o.ConfigureSystemRoles = roles => roles.Map("Developer", "audit:read");
            o.Blazor.Consent.Roles = ["Developer"];
            o.Blazor.Consent.GrantTeamsRead = true;
        }));

        Assert.Null(exception);

        var scopes = ScopesFor(builder, "Developer");
        Assert.Contains("audit:read", scopes);
        Assert.Contains(SystemTeamScopes.Read, scopes);
    }

    [Fact]
    public void Enabled_WithNoConsentRoles_IsSafe()
    {
        var builder = CreateBuilder();
        var exception = Record.Exception(() => builder.AddThargaPlatform(o =>
        {
            o.Blazor.Consent.Roles = [];
            o.Blazor.Consent.GrantTeamsRead = true;
        }));

        Assert.Null(exception);
    }
}
