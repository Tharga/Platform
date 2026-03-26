using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Blazor.Framework;
using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Service;

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

        services.AddSingleton(Options.Create(o));
    }
}
