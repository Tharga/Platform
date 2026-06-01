using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team.Service.Audit;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Extension methods for registering scopes and scope-protected services.
/// </summary>
public static class ScopeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the scope registry and configures scopes.
    /// </summary>
    public static IServiceCollection AddThargaScopes(this IServiceCollection services, Action<ScopeRegistry> configure)
    {
        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<IScopeRegistry>() as ScopeRegistry;
        if (registry == null)
        {
            registry = new ScopeRegistry();
            services.AddSingleton<IScopeRegistry>(registry);
        }

        configure(registry);

        // Order-independent linkage: if the tenant-role registry is already registered, link it now so role
        // scopes resolve. (AddThargaTenantRoles performs the same link when it runs after scopes; this handles
        // the reverse order, where roles were registered first.)
        if (provider.GetService<ITenantRoleRegistry>() is { } roleRegistry)
            registry.SetRoleRegistry(roleRegistry);

        return services;
    }

    /// <summary>
    /// Registers a scoped service wrapped in a <see cref="ScopeProxy{T}"/>
    /// that enforces <see cref="RequireScopeAttribute"/> on every method call.
    /// </summary>
    public static IServiceCollection AddScopedWithScopes<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TService>(sp =>
        {
            var target = sp.GetRequiredService<TImplementation>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var auditLogger = sp.GetService<CompositeAuditLogger>();
            return ScopeProxy<TService>.Create(target, httpContextAccessor, auditLogger);
        });
        return services;
    }
}
