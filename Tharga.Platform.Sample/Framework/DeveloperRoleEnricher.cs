using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Platform.Sample.Framework;

/// <summary>
/// Demo enricher that grants the <c>Developer</c> role to a hardcoded allow-list
/// of emails. In a real app, back this with a database lookup, config, or group
/// claim from the identity provider.
/// </summary>
public class DeveloperRoleEnricher : ITeamClaimsEnricher
{
    private static readonly HashSet<string> _developerEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "daniel.bohlin",
    };

    public Task EnrichAsync(ClaimsIdentity identity)
    {
        var email = identity.FindFirst("preferred_username")?.Value
                    ?? identity.FindFirst(ClaimTypes.Email)?.Value;

        if (email != null && _developerEmails.Any(x => email.Contains(x, StringComparison.InvariantCulture)))
        {
            if (!identity.HasClaim(ClaimTypes.Role, Roles.Developer))
                identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Developer));
        }

        return Task.CompletedTask;
    }
}
