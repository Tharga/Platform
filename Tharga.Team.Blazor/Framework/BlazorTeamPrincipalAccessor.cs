using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// <see cref="ITeamPrincipalAccessor"/> for Blazor apps that also expose an HTTP/API surface. Uses the
/// current <c>HttpContext</c> when one exists (controllers, SSR) and falls back to
/// <see cref="AuthenticationStateProvider"/> in an interactive circuit (where <c>HttpContext</c> is null),
/// so a single <c>[RequireScope]</c>/<c>[RequireAccessLevel]</c> enforces both surfaces.
/// </summary>
public class BlazorTeamPrincipalAccessor(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider) : ITeamPrincipalAccessor
{
    public async ValueTask<ClaimsPrincipal> GetCurrentAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
            return httpContext.User;

        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User;
    }
}
