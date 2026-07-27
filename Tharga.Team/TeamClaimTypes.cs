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

    /// <summary>Claim type carrying the caller's team-member key (<c>ITeamMember.Key</c>) for the selected team. Used to scope owner-private API keys.</summary>
    public const string MemberKey = "MemberKey";

    /// <summary>
    /// Claim type for scopes granted by the caller's access level, roles or overrides <b>on the selected
    /// team</b>. Multiple scope claims may be present.
    /// </summary>
    /// <remarks>
    /// A scope here authorizes the selected team only. System-wide grants use <see cref="SystemScope"/> —
    /// the two are separate claim types so that a grant's origin survives into the claim. Emitting both as
    /// one type meant a system role silently satisfied a check that asked for the scope on a specific team.
    /// </remarks>
    public const string Scope = "Scope";

    /// <summary>
    /// Claim type for scopes granted system-wide — from an app role via <c>ConfigureSystemRoles</c>, or from
    /// a system API key. Not bound to any team. Multiple claims may be present.
    /// </summary>
    public const string SystemScope = "SystemScope";

    /// <summary>Claim type that marks a principal as authenticated via a system API key (not bound to a team). Value: "true".</summary>
    public const string IsSystemKey = "IsSystemKey";

    /// <summary>Claim type carrying the stable identifier of the API key used to authenticate (the <c>IApiKey.Key</c> Guid string).</summary>
    public const string ApiKeyId = "ApiKeyId";

    /// <summary>Prefix for per-tag claims emitted from an API key's <c>Tags</c>. A tag with key <c>K</c> becomes a claim of type <c>tag.K</c>. Multiple claims with the same type may be present.</summary>
    public const string TagPrefix = "tag.";
}
