using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Internal registration for opt-in API key lifecycle hooks (<see cref="IApiKeyLifecycleHandler"/>).
/// Consumers register handlers through <c>ThargaPlatformOptions.AddApiKeyLifecycleHandler&lt;T&gt;()</c>,
/// not by calling these methods directly.
/// </summary>
internal static class ApiKeyLifecycleRegistration
{
    /// <summary>
    /// Registers an <see cref="IApiKeyLifecycleHandler"/> and decorates <see cref="IApiKeyAdministrationService"/>
    /// so the handler is invoked on key create / recycle / delete. Called after the API key services are
    /// registered. May be called multiple times; the decoration is applied once and all handlers are invoked.
    /// </summary>
    internal static IServiceCollection AddThargaApiKeyLifecycleHandler<THandler>(this IServiceCollection services)
        where THandler : class, IApiKeyLifecycleHandler
        => services.AddThargaApiKeyLifecycleHandler(typeof(THandler));

    /// <summary>
    /// Non-generic overload of <see cref="AddThargaApiKeyLifecycleHandler{THandler}"/>.
    /// </summary>
    internal static IServiceCollection AddThargaApiKeyLifecycleHandler(this IServiceCollection services, Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        if (!typeof(IApiKeyLifecycleHandler).IsAssignableFrom(handlerType))
            throw new ArgumentException($"{handlerType.Name} must implement {nameof(IApiKeyLifecycleHandler)}.", nameof(handlerType));

        services.AddScoped(typeof(IApiKeyLifecycleHandler), handlerType);
        EnsureDecorated(services);
        return services;
    }

    private sealed class LifecycleDecorationMarker;

    private static void EnsureDecorated(IServiceCollection services)
    {
        // Apply the decoration exactly once, regardless of how many handlers are registered —
        // the single decorator resolves all IApiKeyLifecycleHandler instances at runtime.
        if (services.Any(d => d.ServiceType == typeof(LifecycleDecorationMarker))) return;
        services.AddSingleton<LifecycleDecorationMarker>();

        var existing = services.LastOrDefault(d => d.ServiceType == typeof(IApiKeyAdministrationService))
            ?? throw new InvalidOperationException(
                "AddThargaApiKeyLifecycleHandler must be called after the API key services are registered " +
                "(e.g. after AddThargaPlatform / AddThargaApiKeys).");

        services.Remove(existing);
        services.AddScoped<IApiKeyAdministrationService>(sp =>
        {
            var inner = ResolveInner(sp, existing);
            var handlers = sp.GetServices<IApiKeyLifecycleHandler>();
            return new ApiKeyLifecycleDecorator(inner, handlers);
        });
    }

    private static IApiKeyAdministrationService ResolveInner(IServiceProvider sp, ServiceDescriptor existing)
    {
        if (existing.ImplementationFactory != null)
            return (IApiKeyAdministrationService)existing.ImplementationFactory(sp);
        if (existing.ImplementationInstance != null)
            return (IApiKeyAdministrationService)existing.ImplementationInstance;
        if (existing.ImplementationType != null)
            return (IApiKeyAdministrationService)ActivatorUtilities.CreateInstance(sp, existing.ImplementationType);

        throw new InvalidOperationException("Cannot resolve inner IApiKeyAdministrationService — no factory, instance, or type.");
    }
}
