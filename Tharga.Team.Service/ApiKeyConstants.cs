using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Well-known constants for API key authentication.
/// </summary>
public static class ApiKeyConstants
{
    /// <summary>HTTP header name for the API key.</summary>
    public const string HeaderName = "X-API-KEY";

    /// <summary>Authentication scheme name.</summary>
    public const string SchemeName = "ApiKeyScheme";

    /// <summary>
    /// Authorization policy name for <b>team</b> API keys. Use with
    /// <c>[Authorize(Policy = ApiKeyConstants.PolicyName)]</c>.
    /// </summary>
    /// <remarks>
    /// <b>Rejects system keys.</b> This and <see cref="SystemPolicyName"/> are <i>disjoint</i>, not a
    /// hierarchy — see <see cref="AnyKeyPolicyName"/>.
    /// </remarks>
    public const string PolicyName = "ApiKeyPolicy";

    /// <summary>Claim type for the team key.</summary>
    [Obsolete($"Use {nameof(TeamClaimTypes)}.{nameof(TeamClaimTypes.TeamKey)} instead.")]
    public const string TeamKeyClaim = TeamClaimTypes.TeamKey;

    /// <summary>Claim type for the access level.</summary>
    [Obsolete($"Use {nameof(TeamClaimTypes)}.{nameof(TeamClaimTypes.AccessLevel)} instead.")]
    public const string AccessLevelClaim = TeamClaimTypes.AccessLevel;

    /// <summary>OpenAPI security scheme identifier.</summary>
    public const string OpenApiSchemeId = "ApiKey";

    /// <summary>
    /// Authorization policy for the toolkit's own HTTP endpoints. Requires an authenticated caller against
    /// <c>ThargaControllerOptions.AuthenticationSchemes</c> — the API-key scheme by default.
    /// </summary>
    public const string ThargaApiPolicyName = "ThargaApiPolicy";

    /// <summary>Authorization policy name for <b>system</b> API keys (keys not bound to a team).</summary>
    /// <remarks><b>Rejects team keys.</b> See <see cref="AnyKeyPolicyName"/>.</remarks>
    public const string SystemPolicyName = "SystemApiKeyPolicy";

    /// <summary>
    /// Authorization policy name accepting <b>any</b> valid API key, team or system.
    /// </summary>
    /// <remarks>
    /// <b><see cref="PolicyName"/> and <see cref="SystemPolicyName"/> are disjoint, not a hierarchy.</b>
    /// The first refuses a system key; the second refuses a team key. ASP.NET Core <i>combines</i>
    /// policies when several are required, so <c>RequireAuthorization(PolicyName, SystemPolicyName)</c>
    /// admits <b>nothing</b> — a trap the naming invites, since "system" reads like "team plus more".
    /// <para>
    /// Use this where an endpoint should be reachable by either kind, which was previously only possible
    /// by hand-writing a policy. It asserts nothing about <c>IsSystemKey</c> in either direction, which is
    /// also what <c>Tharga.Mcp</c>'s <c>RequireAuth</c> policy does — so an MCP endpoint already behaves
    /// this way and needs no policy named here.
    /// </para>
    /// </remarks>
    public const string AnyKeyPolicyName = "AnyApiKeyPolicy";
}
