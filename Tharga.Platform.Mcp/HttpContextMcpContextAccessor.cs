using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Tharga.Mcp;

namespace Tharga.Platform.Mcp;

/// <summary>
/// <see cref="IMcpContextAccessor"/> implementation that builds an <see cref="IMcpContext"/> from the current
/// <see cref="HttpContext"/> on demand. Replaces the default AsyncLocal-backed accessor when <c>AddMcpPlatform</c>
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

            // The Phase 0 single-endpoint design doesn't carry per-call scope through the transport.
            // Callers can still read user/team/developer claims. Scope is User by default and providers
            // should enforce their own declared scope via context.IsDeveloper / context.TeamId.
            return new PlatformMcpContext(ctx.User, McpScope.User, _options.DeveloperRole);
        }
        set
        {
            // No-op: context is derived from HttpContext, not assigned.
        }
    }
}
