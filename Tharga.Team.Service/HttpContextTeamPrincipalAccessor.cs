using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Service;

/// <summary>
/// Default <see cref="ITeamPrincipalAccessor"/> that resolves the caller from the current HTTP request
/// (<see cref="IHttpContextAccessor"/>). Used for controller/API callers; returns null outside a request.
/// </summary>
public class HttpContextTeamPrincipalAccessor(IHttpContextAccessor httpContextAccessor) : ITeamPrincipalAccessor
{
    public ValueTask<ClaimsPrincipal> GetCurrentAsync()
        => new(httpContextAccessor.HttpContext?.User);
}
