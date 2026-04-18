namespace Tharga.Platform.Mcp;

/// <summary>
/// Helper for MCP tool implementations to enforce fine-grained scopes imperatively.
/// Use when a tool is pure infrastructure (no Platform service behind it) — when a tool wraps
/// a Platform service method, rely on <c>[RequireScope]</c> and the existing <c>ScopeProxy</c> instead.
/// </summary>
public interface IMcpScopeChecker
{
    /// <summary>
    /// Throws <see cref="UnauthorizedAccessException"/> if the current caller lacks <paramref name="scope"/>.
    /// </summary>
    void Require(string scope);

    /// <summary>
    /// Returns true if the current caller has <paramref name="scope"/>.
    /// </summary>
    bool Has(string scope);
}
