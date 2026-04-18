namespace Tharga.Platform.Mcp;

/// <summary>
/// Configuration for the Tharga.Platform.Mcp bridge.
/// </summary>
public sealed class McpPlatformOptions
{
    /// <summary>
    /// Role that must be present on the caller for <see cref="Tharga.Mcp.McpScope.System"/> calls.
    /// Defaults to <c>"Developer"</c> to match Tharga.Platform conventions.
    /// </summary>
    public string DeveloperRole { get; set; } = "Developer";
}
