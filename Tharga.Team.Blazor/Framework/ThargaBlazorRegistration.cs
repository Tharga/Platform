using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Blazor.Framework;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Service;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Framework;

public static class ThargaBlazorRegistration
{
    public static void AddThargaTeamBlazor(this IServiceCollection services, Action<ThargaBlazorOptions> options = null, IConfiguration configuration = null)
    {
        var o = new ThargaBlazorOptions();
        options?.Invoke(o);

        services.AddThargaBlazor(bo => bo.Title = o.Title, configuration);

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
                services.AddScoped(o._apiKeyService);
                services.AddScoped(typeof(IApiKeyAdministrationService), sp => sp.GetRequiredService(o._apiKeyService));
            }

            // Register default team and API key scopes unless already registered
            if (!services.Any(d => d.ServiceType == typeof(IScopeRegistry)))
            {
                services.AddThargaScopes(scopes =>
                {
                    scopes.Register(TeamScopes.Read, AccessLevel.Viewer);
                    scopes.Register(TeamScopes.Manage, AccessLevel.Administrator);
                    scopes.Register(TeamScopes.MemberInvite, AccessLevel.Administrator);
                    scopes.Register(TeamScopes.MemberRemove, AccessLevel.Administrator);
                    scopes.Register(TeamScopes.MemberRole, AccessLevel.Administrator);
                    scopes.Register(ApiKeyScopes.Manage, AccessLevel.Administrator);
                });
            }

            // Server-side claims enrichment — always registered, reads selected_team_id cookie
            services.AddHttpContextAccessor();
            services.AddTransient<IClaimsTransformation, TeamServerClaimsTransformation>();

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

        if (o._apiKeyService != null)
        {
            services.AddScoped(o._apiKeyService);
            services.AddScoped(typeof(IApiKeyAdministrationService), sp => sp.GetRequiredService(o._apiKeyService));
        }

        // Audit decorators — wrap ITeamService and IApiKeyAdministrationService when audit logging is configured.
        // Uses deferred resolution so AddThargaAuditLogging() can be called after AddThargaTeamBlazor().
        if (o._teamService != null)
        {
            DecorateWithAudit<ITeamService>(services,
                (inner, logger, http) => new AuditingTeamServiceDecorator(inner, logger, http));
        }

        if (o._apiKeyService != null)
        {
            DecorateWithAudit<IApiKeyAdministrationService>(services,
                (inner, logger, http) => new AuditingApiKeyServiceDecorator(inner, logger, http));
        }

        services.AddSingleton(Options.Create(o));
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
