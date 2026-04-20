namespace Tharga.Platform.Mcp;

/// <summary>
/// Built-in MCP scope constants registered by <c>AddPlatform</c>.
/// Provider packages (e.g. <c>Tharga.MongoDB.Mcp</c>) register their own scopes in the same <c>mcp:*</c> namespace.
/// </summary>
public static class McpScopes
{
    /// <summary>Allows listing MCP tools and resources visible to the caller. Default access level: Viewer.</summary>
    public const string Discover = "mcp:discover";
}
