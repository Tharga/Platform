using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Authentication;
using Tharga.Team.Service;
using Tharga.Team.Service.Audit;
using Tharga.Team.Service.Email;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Single entry point for registering all Tharga Platform services.
/// </summary>
public static class ThargaPlatformRegistration
{
    /// <summary>
    /// Registers all Tharga Platform services with sensible defaults.
    /// Call <see cref="UseThargaPlatform"/> on the built WebApplication to configure middleware.
    /// </summary>
    public static void AddThargaPlatform(this WebApplicationBuilder builder, Action<ThargaPlatformOptions> configure = null)
    {
        var options = new ThargaPlatformOptions();
        configure?.Invoke(options);

        // Auth (Azure AD + OIDC)
        builder.AddThargaAuth(o =>
        {
            o.LoginPath = options.Auth.LoginPath;
            o.LogoutPath = options.Auth.LogoutPath;
            o.ValidateConfiguration = options.Auth.ValidateConfiguration;
        });

        // API key authentication scheme
        if (options.ApiKey != null)
        {
            builder.Services
                .AddAuthentication()
                .AddThargaApiKeyAuthentication(o =>
                {
                    o.AdvancedMode = options.ApiKey.AdvancedMode;
                    o.AutoKeyCount = options.ApiKey.AutoKeyCount;
                    o.AutoLockKeys = options.ApiKey.AutoLockKeys;
                    o.MaxExpiryDays = options.ApiKey.MaxExpiryDays;
                });

            builder.Services.AddThargaApiKeys();
        }

        // Blazor UI layer — pass the pre-configured options object directly
        builder.Services.AddThargaTeamBlazor(o =>
        {
            o.Title = options.Blazor.Title;
            o.AutoCreateFirstTeam = options.Blazor.AutoCreateFirstTeam;
            o.AllowTeamCreation = options.Blazor.AllowTeamCreation;
            o.ShowMemberRoles = options.Blazor.ShowMemberRoles;
            o.ShowScopeOverrides = options.Blazor.ShowScopeOverrides;
            o.SkipAuthStateDecoration = options.Blazor.SkipAuthStateDecoration;
            o._teamService = options.Blazor._teamService;
            o._userService = options.Blazor._userService;
            o._memberType = options.Blazor._memberType;
            o._apiKeyService = options.Blazor._apiKeyService;
        }, builder.Configuration);

        // Controllers + Swagger
        if (options.Controllers != null)
        {
            builder.Services.AddThargaControllers(o =>
            {
                o.SwaggerTitle = options.Controllers.SwaggerTitle;
                o.SwaggerRoutePrefix = options.Controllers.SwaggerRoutePrefix;
            });
        }

        // Scopes (opt-in)
        if (options.ConfigureScopes != null)
        {
            builder.Services.AddThargaScopes(options.ConfigureScopes);
        }

        // Tenant roles (opt-in, requires scopes)
        if (options.ConfigureTenantRoles != null)
        {
            builder.Services.AddThargaTenantRoles(options.ConfigureTenantRoles);
        }

        // Audit logging (opt-in)
        if (options.Audit != null)
        {
            builder.Services.AddThargaAuditLogging(o =>
            {
                o.StorageMode = options.Audit.StorageMode;
                o.CallerFilter = options.Audit.CallerFilter;
                o.EventFilter = options.Audit.EventFilter;
                o.ExcludedActions = options.Audit.ExcludedActions;
                o.ExcludedEndpoints = options.Audit.ExcludedEndpoints;
                o.RetentionDays = options.Audit.RetentionDays;
                o.BatchSize = options.Audit.BatchSize;
                o.FlushIntervalSeconds = options.Audit.FlushIntervalSeconds;
            });
        }

        // Email sender: custom type > SMTP (if EmailOptions set) > nothing
        if (options._emailSenderType != null)
        {
            builder.Services.AddScoped(typeof(ITeamEmailSender), options._emailSenderType);
        }
        else if (options.Email != null)
        {
            var fromName = options.Email.FromName ?? options.Blazor.Title;
            builder.Services.Configure<EmailOptions>(o =>
            {
                o.SmtpHost = options.Email.SmtpHost;
                o.SmtpPort = options.Email.SmtpPort;
                o.UseSsl = options.Email.UseSsl;
                o.Username = options.Email.Username;
                o.Password = options.Email.Password;
                o.FromAddress = options.Email.FromAddress;
                o.FromName = fromName;
            });
            builder.Services.AddScoped<ITeamEmailSender, SmtpTeamEmailSender>();
        }
    }

    /// <summary>
    /// Configures Tharga Platform middleware (auth endpoints, controllers, Swagger).
    /// </summary>
    public static void UseThargaPlatform(this WebApplication app)
    {
        app.UseThargaAuth();

        var controllerOptions = app.Services.GetService<ThargaControllerOptions>();
        if (controllerOptions != null)
        {
            app.UseThargaControllers();
        }
    }
}
