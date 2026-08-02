namespace Tharga.Team.Mcp;

/// <summary>
/// Configuration for the Tharga.Team.Mcp bridge.
/// </summary>
public sealed class McpTeamOptions
{
    /// <summary>
    /// Role that must be present on the caller for <see cref="Tharga.Mcp.McpScope.System"/> calls.
    /// Defaults to <c>"Developer"</c> to match Tharga.Team conventions.
    /// </summary>
    public string DeveloperRole { get; set; } = "Developer";

    /// <summary>
    /// Header a caller names the target team in, per call. Default <c>X-Team-Key</c>.
    /// </summary>
    /// <remarks>
    /// <b>Per call, not per session.</b> <c>ModelContextProtocol</c> 2.0.0 is stateless by default, so
    /// there is no session to hold a selection in — and over HTTP, per-request is per-call.
    /// <para>
    /// A header rather than a tool argument: an argument would have to be threaded through every
    /// <c>IMcpResourceProvider</c> and <c>IMcpToolProvider</c> signature, including the host's own, and a
    /// provider that forgot it would silently address the wrong team. Here the selection is resolved once,
    /// and every provider keeps reading <c>context.TeamId</c> exactly as before.
    /// </para>
    /// </remarks>
    public string TeamKeyHeader { get; set; } = "X-Team-Key";

    /// <summary>
    /// Access level a team's consent grants when the consent itself carries no level. Default
    /// <see cref="AccessLevel.Viewer"/>.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>ThargaBlazorOptions.Consent.AccessLevel</c>, which lives in the Blazor package and is
    /// not reachable from here. <b>A host that changes one should change the other</b>, or the same
    /// caller reaches the same team at different levels over MCP and over the UI.
    /// </remarks>
    public AccessLevel ConsentAccessLevel { get; set; } = AccessLevel.Viewer;

    /// <summary>
    /// When true, registers read-only system-scope resource providers that expose cross-tenant
    /// team, API-key, role, and audit-log data for diagnostic use by Developers.
    /// Default false — opt in only if you want diagnostic data surfaced over MCP.
    /// </summary>
    public bool ExposeSystemResources { get; set; }
}
