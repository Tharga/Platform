using System.Security.Claims;
using Tharga.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp;

/// <summary>
/// <see cref="IMcpContext"/> implementation backed by a <see cref="ClaimsPrincipal"/>.
/// Reads UserId, TeamId, and the Developer role from standard Platform claim types.
/// </summary>
public sealed class PlatformMcpContext : IMcpContext
{
    /// <param name="principal">The authenticated user, or null for anonymous.</param>
    /// <param name="scope">The MCP scope this call belongs to.</param>
    /// <param name="developerRole">Role name that gates <see cref="McpScope.System"/> calls.</param>
    public PlatformMcpContext(ClaimsPrincipal principal, McpScope scope, string developerRole)
    {
        Scope = scope;
        UserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? principal?.FindFirst("sub")?.Value;
        TeamId = principal?.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        IsDeveloper = principal?.IsInRole(developerRole) ?? false;
    }

    public string UserId { get; }
    public string TeamId { get; }
    public bool IsDeveloper { get; }
    public McpScope Scope { get; }
}
