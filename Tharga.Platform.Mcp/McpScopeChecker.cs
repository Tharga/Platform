using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Platform.Mcp;

/// <summary>
/// Default <see cref="IMcpScopeChecker"/> implementation backed by the current <see cref="HttpContext"/>.
/// </summary>
public sealed class McpScopeChecker : IMcpScopeChecker
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public McpScopeChecker(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool Has(string scope)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.Claims.Any(c => c.Type == TeamClaimTypes.Scope && c.Value == scope) ?? false;
    }

    public void Require(string scope)
    {
        if (!Has(scope))
            throw new UnauthorizedAccessException($"Missing required scope '{scope}'.");
    }
}
