using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tharga.Team;

/// <summary>
/// Extension methods for registering tenant roles.
/// </summary>
public static class TenantRoleServiceCollectionExtensions
{
    /// <summary>
    /// Registers the tenant role registry and configures roles.
    /// Links to the scope registry so effective scopes include role scopes.
    /// </summary>
    public static IServiceCollection AddThargaTenantRoles(this IServiceCollection services, Action<TenantRoleRegistry> configure)
    {
        var roleRegistry = new TenantRoleRegistry();
        configure(roleRegistry);
        services.AddSingleton<ITenantRoleRegistry>(roleRegistry);

        services.TryAddSingleton<ITenantRoleVisibilityProvider, AllRolesVisibleTenantRoleVisibilityProvider>();

        var scopeRegistry = services.BuildServiceProvider().GetService<IScopeRegistry>() as ScopeRegistry;
        scopeRegistry?.SetRoleRegistry(roleRegistry);

        return services;
    }

    /// <summary>
    /// Registers the team-aware <see cref="ITenantRoleService"/> that resolves a member's effective scopes
    /// from code-registered roles plus the team's runtime-defined custom roles. Enables dynamic tenant roles:
    /// without it, only code-registered roles are resolved into claims.
    /// </summary>
    public static IServiceCollection AddThargaDynamicTenantRoles(this IServiceCollection services)
    {
        services.TryAddScoped<ITenantRoleService, TenantRoleService>();
        return services;
    }
}
