using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Tharga.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp;

/// <summary>
/// <see cref="IMcpContextAccessor"/> implementation that builds an <see cref="IMcpContext"/> from the current
/// <see cref="HttpContext"/> on demand. Replaces the default AsyncLocal-backed accessor when <c>AddPlatform</c>
/// is registered.
/// </summary>
/// <remarks>
/// The setter is a no-op: the context is derived from the HTTP request, not assigned.
/// </remarks>
public sealed class HttpContextMcpContextAccessor : IMcpContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly McpPlatformOptions _options;

    public HttpContextMcpContextAccessor(IHttpContextAccessor httpContextAccessor, IOptions<McpPlatformOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public IMcpContext Current
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return null;

            var user = ctx.User;
            // Derive scope from claims so the Tharga.Mcp dispatcher's hierarchy filter
            // (p.Scope <= current.Scope, since Tharga.Mcp 0.1.2) can see providers at the
            // caller's level and below.
            //
            // - Developer role (or system API key) → System scope sees everything
            // - TeamKey claim → Team scope sees Team + User providers
            // - Otherwise → User scope
            var scope =
                (user?.IsInRole(_options.DeveloperRole) ?? false)
                    || (user?.HasClaim(TeamClaimTypes.IsSystemKey, "true") ?? false)
                    ? McpScope.System
                : !string.IsNullOrEmpty(user?.FindFirst(TeamClaimTypes.TeamKey)?.Value)
                    ? McpScope.Team
                : McpScope.User;

            return new PlatformMcpContext(user, scope, _options.DeveloperRole);
        }
        set
        {
            // No-op: context is derived from HttpContext, not assigned.
        }
    }
}
