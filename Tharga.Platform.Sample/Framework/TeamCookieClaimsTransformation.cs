using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Tharga.Platform.Sample.Framework;

public class TeamCookieClaimsTransformation : IClaimsTransformation
{
    private const string TeamKeyClaim = "team_id";
    private const string SelectedTeamCookie = "selected_team_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TeamCookieClaimsTransformation(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return Task.FromResult(principal);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return Task.FromResult(principal);

        if (httpContext.Request.Cookies.TryGetValue(SelectedTeamCookie, out var teamKey)
            && !string.IsNullOrEmpty(teamKey))
        {
            if (principal.Identity is ClaimsIdentity identity
                && !principal.HasClaim(c => c.Type == TeamKeyClaim))
            {
                identity.AddClaim(new Claim(TeamKeyClaim, teamKey));
            }
        }

        return Task.FromResult(principal);
    }
}
