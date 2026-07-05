using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Blazor.Framework;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Service;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Framework;

public static class ThargaBlazorRegistration
{
    /// <summary>
    /// Registers Tharga Team Blazor components on a host application builder, threading
    /// <see cref="IHostApplicationBuilder.Configuration"/> through automatically.
    /// </summary>
    public static void AddThargaTeamBlazor(this IHostApplicationBuilder builder, Action<ThargaBlazorOptions> options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddThargaTeamBlazor(options, builder.Configuration);
    }

    public static void AddThargaTeamBlazor(this IServiceCollection services, Action<ThargaBlazorOptions> options = null, IConfiguration configuration = null)
    {
        var o = new ThargaBlazorOptions();
        options?.Invoke(o);

        services.AddThargaBlazor(bo => bo.Title = o.Title, configuration);

        // UI string provider — a consumer-supplied provider (via AddTextProvider) localizes the strings;
        // otherwise the built-in default returns English.
        if (o._textProvider != null)
        {
            services.AddScoped(typeof(IThargaTextProvider), o._textProvider);
        }
        services.TryAddSingleton<IThargaTextProvider, DefaultThargaTextProvider>();

        if (o._teamService != null)
        {
            services.AddScoped<ITeamStateService, TeamStateService>();

            services.AddScoped(o._teamService);
            services.AddScoped(typeof(ITeamService), sp => sp.GetRequiredService(o._teamService));

            services.AddScoped(o._userService);
            services.AddScoped(typeof(IUserService), sp => sp.GetRequiredService(o._userService));

            if (o._memberType != null)
            {
                var managementServiceType = typeof(TeamManagementService<>).MakeGenericType(o._memberType);
                services.AddScoped(typeof(ITeamManagementService), managementServiceType);
            }

            if (o._apiKeyService != null)
            {
                services.AddAuditedApiKeyAdministrationService(o._apiKeyService);
            }

            // Register default team and API key scopes unless already registered
            if (!services.Any(d => d.ServiceType == typeof(IScopeRegistry)))
            {
                services.AddThargaScopes(scopes =>
                {
                    scopes.Register(TeamScopes.Read, AccessLevel.Viewer, "View team details and members.");
                    scopes.Register(TeamScopes.Manage, AccessLevel.Administrator, "Administer the team: rename, delete, and transfer ownership.");
                    scopes.Register(TeamScopes.MemberManage, AccessLevel.Administrator, "Manage team members — invite, remove, edit display names, and change access level, roles, and scope overrides.");
                    scopes.Register(ApiKeyScopes.Manage, AccessLevel.Administrator, "Create, refresh, lock, and delete API keys.");
                    scopes.Register(AuditScopes.Read, AccessLevel.Administrator, "View the audit log.");
                });
            }

            // Built-in system scope: teams:delete authorizes deleting any team (cross-team). Merge-safe with
            // any consumer ConfigureSystemScopes; grant it via ConfigureSystemRoles or a system API key.
            services.AddThargaSystemScopes(scopes =>
            {
                if (scopes.All.All(s => s.Name != SystemTeamScopes.Delete))
                    scopes.Register(SystemTeamScopes.Delete, "Delete any team (cross-team), regardless of membership or the AllowTeamCreation option.");
            });

            // Server-side claims enrichment — always registered, reads selected_team_id cookie
            services.AddHttpContextAccessor();
            services.AddTransient<IClaimsTransformation, TeamServerClaimsTransformation>();

            // Make scope/access-level proxies resolve the caller from the circuit too (not just HttpContext),
            // so [RequireScope]/[RequireAccessLevel] enforce in interactive Blazor Server as well as on the API.
            services.Replace(ServiceDescriptor.Scoped<ITeamPrincipalAccessor, BlazorTeamPrincipalAccessor>());

            // Custom claims enricher — runs before member lookup and consent evaluation
            if (o._claimsEnricher != null)
            {
                services.AddScoped(typeof(ITeamClaimsEnricher), o._claimsEnricher);
            }

            if (!o.SkipAuthStateDecoration)
            {
                // Client-side (WASM) claims enrichment via JS interop / LocalStorage.
                // Only needed for pure WASM apps. Server/SSR apps use the transformation above.
                var existing = services.LastOrDefault(d => d.ServiceType == typeof(AuthenticationStateProvider));
                if (existing != null)
                {
                    services.Remove(existing);

                    if (existing.ImplementationType != null)
                    {
                        services.AddKeyedScoped(typeof(AuthenticationStateProvider), "inner-auth-state", existing.ImplementationType);
                    }
                    else if (existing.ImplementationFactory != null)
                    {
                        var factory = existing.ImplementationFactory;
                        services.AddKeyedScoped("inner-auth-state", (sp, _) => (AuthenticationStateProvider)factory(sp));
                    }
                }

                services.AddScoped<AuthenticationStateProvider, TeamClaimsAuthenticationStateProvider>();
            }
        }

        // Audit decorator for ITeamService — wrap when audit logging is configured. Uses deferred
        // resolution so AddThargaAuditLogging() can be called after AddThargaTeamBlazor().
        // (IApiKeyAdministrationService audit is owned by AddAuditedApiKeyAdministrationService, applied
        //  at resolve time, so it is order-independent and not clobbered by AddThargaApiKeyAuthentication.)
        if (o._teamService != null)
        {
            DecorateWithAudit<ITeamService>(services,
                (inner, logger, http) => new AuditingTeamServiceDecorator(inner, logger, http));

            // Service-layer authorization — outermost (checks before audit/operation), so the same scope
            // rules protect the Blazor circuit and any consumer's REST controller calling ITeamService.
            services.TryAddScoped<TeamAuthorizer>();
            DecorateWithAuthorization(services, new TeamLifecycleOptions { AllowTeamCreation = o.AllowTeamCreation });
        }

        services.AddSingleton(Options.Create(o));
    }

    private static void DecorateWithAuthorization(IServiceCollection services, TeamLifecycleOptions lifecycle)
    {
        var existing = services.LastOrDefault(d => d.ServiceType == typeof(ITeamService));
        if (existing == null) return;

        services.Remove(existing);

        services.AddScoped<ITeamService>(sp =>
        {
            ITeamService inner;
            if (existing.ImplementationFactory != null)
                inner = (ITeamService)existing.ImplementationFactory(sp);
            else if (existing.ImplementationType != null)
                inner = (ITeamService)ActivatorUtilities.CreateInstance(sp, existing.ImplementationType);
            else
                throw new InvalidOperationException("Cannot resolve inner ITeamService for authorization decoration.");

            var authorizer = sp.GetRequiredService<TeamAuthorizer>();
            var scopeRegistry = sp.GetService<IScopeRegistry>();
            var tenantRoleRegistry = sp.GetService<ITenantRoleRegistry>();
            return new AuthorizationTeamServiceDecorator(inner, authorizer, lifecycle, scopeRegistry, tenantRoleRegistry);
        });
    }

    private static void DecorateWithAudit<TService>(
        IServiceCollection services,
        Func<TService, CompositeAuditLogger, IHttpContextAccessor, TService> factory)
        where TService : class
    {
        var existing = services.LastOrDefault(d => d.ServiceType == typeof(TService));
        if (existing == null) return;

        services.Remove(existing);

        services.AddScoped(sp =>
        {
            // Resolve the inner service from the original registration
            TService inner;
            if (existing.ImplementationFactory != null)
                inner = (TService)existing.ImplementationFactory(sp);
            else if (existing.ImplementationType != null)
                inner = (TService)ActivatorUtilities.CreateInstance(sp, existing.ImplementationType);
            else
                throw new InvalidOperationException($"Cannot resolve inner {typeof(TService).Name} — no factory or type.");

            var auditLogger = sp.GetService<CompositeAuditLogger>();
            if (auditLogger == null) return inner;

            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return factory(inner, auditLogger, httpContextAccessor);
        });
    }
}
