using System.Security.Claims;

namespace Tharga.Team;

/// <summary>
/// Claim types carrying the external-directory object id (Microsoft Entra ID's <c>oid</c>), in both the
/// raw JWT form and the .NET-mapped form.
/// </summary>
public static class DirectoryClaimTypes
{
    /// <summary>The raw <c>oid</c> claim, present when inbound claim mapping is disabled.</summary>
    public const string ObjectId = "oid";

    /// <summary>The .NET-mapped object-identifier claim (the default with Microsoft.Identity.Web).</summary>
    public const string ObjectIdentifier = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// The principal's directory object id, from either claim form; null when the identity provider
    /// did not issue one.
    /// </summary>
    public static string GetDirectoryId(this ClaimsPrincipal principal)
        => principal?.FindFirst(ObjectIdentifier)?.Value
           ?? principal?.FindFirst(ObjectId)?.Value;
}
