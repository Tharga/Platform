using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tharga.Mcp;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Mcp;

/// <summary>
/// Extension methods on <see cref="IThargaMcpBuilder"/> for wiring the Tharga.Team bridge
/// into the MCP pipeline.
/// </summary>
public static class McpTeamBuilderExtensions
{
    /// <summary>
    /// Registers the Team bridge: populates <see cref="IMcpContext"/> from the current <see cref="HttpContext"/>,
    /// enables <see cref="IMcpScopeChecker"/>, and registers built-in <c>mcp:*</c> scopes.
    /// </summary>
    public static IThargaMcpBuilder AddTeam(this IThargaMcpBuilder builder, Action<McpTeamOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new McpTeamOptions();
        configure?.Invoke(options);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // Replace the default AsyncLocal accessor with an HttpContext-backed one.
        var existing = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IMcpContextAccessor));
        if (existing != null) builder.Services.Remove(existing);
        builder.Services.AddSingleton<IMcpContextAccessor, HttpContextMcpContextAccessor>();

        builder.Services.TryAddSingleton<IMcpScopeChecker, McpScopeChecker>();

        // Register built-in mcp:* scopes into both registries, because both routes to holding one are
        // legitimate: an access level grants it inside a team, while an app role or a system API key
        // grants it system-wide. Registering only as a team scope left it grantable but unsatisfiable —
        // the checker read system claims alone. Each extension creates its registry if missing.
        // Both registries throw on a duplicate, and a host may already have registered these by hand —
        // registering mcp:discover as a system scope was the documented workaround while the checker read
        // system claims only, so the consumers this fix is for are exactly the ones most likely to have
        // it. Skipping a name already present keeps their startup working; the same merge-safe shape
        // AddThargaTeamBlazor uses for teams:delete and users:manage.
        builder.Services.AddThargaScopes(scopes =>
        {
            if (scopes.All.All(s => s.Name != McpScopes.Discover))
                scopes.Register(McpScopes.Discover, AccessLevel.Viewer, "Discover and list available MCP tools and resources.");
        });

        builder.Services.AddThargaSystemScopes(scopes =>
        {
            if (scopes.All.All(s => s.Name != McpScopes.Discover))
                scopes.Register(McpScopes.Discover, "Discover and list available MCP tools and resources.");
        });

        // Always-on user-scope and team-scope resource providers. They self-gate on the
        // principal's UserId / TeamKey claim, so anonymous and system-only callers see nothing.
        builder.AddResourceProvider<TeamUserResourceProvider>();
        builder.AddResourceProvider<TeamResourceProvider>();

        // Opt-in system-scope resource providers (diagnostic data for Developers).
        if (options.ExposeSystemResources)
        {
            builder.AddResourceProvider<TeamSystemResourceProvider>();
        }

        return builder;
    }
}
