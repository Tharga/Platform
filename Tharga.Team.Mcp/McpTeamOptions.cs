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
    /// When true, registers read-only system-scope resource providers that expose cross-tenant
    /// team, API-key, role, and audit-log data for diagnostic use by Developers.
    /// Default false — opt in only if you want diagnostic data surfaced over MCP.
    /// </summary>
    public bool ExposeSystemResources { get; set; }
}
