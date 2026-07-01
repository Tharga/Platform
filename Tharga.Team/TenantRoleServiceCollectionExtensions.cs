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
}
