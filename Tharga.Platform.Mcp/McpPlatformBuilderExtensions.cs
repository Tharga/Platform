using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tharga.Mcp;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Platform.Mcp;

/// <summary>
/// Extension methods on <see cref="IThargaMcpBuilder"/> for wiring the Tharga.Platform bridge
/// into the MCP pipeline.
/// </summary>
public static class McpPlatformBuilderExtensions
{
    /// <summary>
    /// Registers the Platform bridge: populates <see cref="IMcpContext"/> from the current <see cref="HttpContext"/>,
    /// enables <see cref="IMcpScopeChecker"/>, and registers built-in <c>mcp:*</c> scopes.
    /// </summary>
    public static IThargaMcpBuilder AddPlatform(this IThargaMcpBuilder builder, Action<McpPlatformOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new McpPlatformOptions();
        configure?.Invoke(options);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // Replace the default AsyncLocal accessor with an HttpContext-backed one.
        var existing = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IMcpContextAccessor));
        if (existing != null) builder.Services.Remove(existing);
        builder.Services.AddSingleton<IMcpContextAccessor, HttpContextMcpContextAccessor>();

        builder.Services.TryAddSingleton<IMcpScopeChecker, McpScopeChecker>();

        // Register built-in mcp:* scopes. Uses AddThargaScopes, which creates the registry if missing.
        builder.Services.AddThargaScopes(scopes =>
        {
            scopes.Register(McpScopes.Discover, AccessLevel.Viewer);
        });

        // Always-on user-scope and team-scope resource providers. They self-gate on the
        // principal's UserId / TeamKey claim, so anonymous and system-only callers see nothing.
        builder.AddResourceProvider<PlatformUserResourceProvider>();
        builder.AddResourceProvider<PlatformTeamResourceProvider>();

        // Opt-in system-scope resource providers (diagnostic data for Developers).
        if (options.ExposeSystemResources)
        {
            builder.AddResourceProvider<PlatformSystemResourceProvider>();
        }

        return builder;
    }

    /// <summary>
    /// Maps the MCP endpoint and applies Platform authentication policy.
    /// </summary>
    /// <remarks>
    /// Obsolete since Tharga.Mcp 0.1.2 — <c>UseThargaMcp()</c> now reads
    /// <see cref="ThargaMcpOptions.RequireAuth"/> and applies <c>RequireAuthorization()</c> itself.
    /// Consumers should call <c>app.UseThargaMcp()</c> directly.
    /// </remarks>
    [Obsolete("Use app.UseThargaMcp() directly — it honors ThargaMcpOptions.RequireAuth since Tharga.Mcp 0.1.2. Will be removed in a future release.")]
    public static IEndpointConventionBuilder MapMcpPlatform(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.UseThargaMcp();
    }

    /// <summary>Obsolete alias for <see cref="AddPlatform"/>.</summary>
    [Obsolete("Use AddPlatform() instead. Will be removed in a future release.")]
    public static IThargaMcpBuilder AddMcpPlatform(this IThargaMcpBuilder builder, Action<McpPlatformOptions> configure = null)
        => builder.AddPlatform(configure);
}
