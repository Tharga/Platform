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

    /// <summary>Authorization policy name. Use with [Authorize(Policy = ApiKeyConstants.PolicyName)].</summary>
    public const string PolicyName = "ApiKeyPolicy";

    /// <summary>Claim type for the team key.</summary>
    [Obsolete($"Use {nameof(TeamClaimTypes)}.{nameof(TeamClaimTypes.TeamKey)} instead.")]
    public const string TeamKeyClaim = TeamClaimTypes.TeamKey;

    /// <summary>Claim type for the access level.</summary>
    [Obsolete($"Use {nameof(TeamClaimTypes)}.{nameof(TeamClaimTypes.AccessLevel)} instead.")]
    public const string AccessLevelClaim = TeamClaimTypes.AccessLevel;

    /// <summary>OpenAPI security scheme identifier.</summary>
    public const string OpenApiSchemeId = "ApiKey";

    /// <summary>Authorization policy name for system-level API keys (keys not bound to a team).</summary>
    public const string SystemPolicyName = "SystemApiKeyPolicy";
}
