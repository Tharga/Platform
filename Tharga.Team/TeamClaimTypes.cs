namespace Tharga.Team;

/// <summary>
/// Claim type constants for team-based authorization.
/// </summary>
public static class TeamClaimTypes
{
    /// <summary>Claim type for the team key.</summary>
    public const string TeamKey = "TeamKey";

    /// <summary>Claim type for the access level.</summary>
    public const string AccessLevel = "AccessLevel";

    /// <summary>Claim type for scopes. Multiple scope claims may be present.</summary>
    public const string Scope = "Scope";

    /// <summary>Claim type that marks a principal as authenticated via a system API key (not bound to a team). Value: "true".</summary>
    public const string IsSystemKey = "IsSystemKey";

    /// <summary>Claim type carrying the stable identifier of the API key used to authenticate (the <c>IApiKey.Key</c> Guid string).</summary>
    public const string ApiKeyId = "ApiKeyId";
}
