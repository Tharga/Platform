namespace Tharga.Team.Mcp;

/// <summary>
/// Built-in MCP scope constants registered by <c>AddTeam</c>.
/// Provider packages (e.g. <c>Tharga.MongoDB.Mcp</c>) register their own scopes in the same <c>mcp:*</c> namespace.
/// </summary>
public static class McpScopes
{
    /// <summary>
    /// Allows listing MCP tools and resources visible to the caller.
    /// </summary>
    /// <remarks>
    /// Registered in both scope registries by <c>AddTeam</c>, so it can be held three ways: by
    /// <see cref="AccessLevel.Viewer"/> or above inside a team, by an app role mapped through
    /// <c>ConfigureSystemRoles</c>, or by a system API key granted it. A team grant authorizes within the
    /// caller's selected team; a system grant authorizes with no team selected.
    /// </remarks>
    public const string Discover = "mcp:discover";
}
