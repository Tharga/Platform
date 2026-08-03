using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// Builds an <see cref="AccessSimulation"/> from each of the ways a target can be named.
/// </summary>
/// <remarks>
/// The four kinds are not four features. Each names a <b>target scope set</b>, and the simulation is
/// always the same operation afterwards: keep what the target has and the caller also has, remove the
/// rest. Keeping the construction in one place is what makes that true rather than aspirational.
/// <para>
/// <b>Every kind drops system scopes and application roles.</b> They were originally dropped only when
/// simulating a user — the reasoning being that another person's system access cannot be computed — but
/// that left the other three kinds keeping the caller's own system-wide grants, so simulating "Viewer"
/// still showed every team and the cross-team audit log. A simulation is for seeing <i>less</i>; a
/// system-wide grant surviving one defeats it whatever the target was named. Reported from the sample,
/// 2026-08-03.
/// </para>
/// <para>
/// The consequence is worth stating: a simulation shows access <b>within the selected team</b>, never a
/// faithful reproduction of someone's system-wide reach. <c>AccessSimulationDifference</c> says so for a
/// user target, where it is unknowable rather than merely dropped.
/// </para>
/// </remarks>
internal static class AccessSimulationTargets
{
    /// <summary>
    /// Another member of the selected team, from the grant they actually hold there.
    /// </summary>
    public static AccessSimulation FromUser(string label, AccessLevel accessLevel, IEnumerable<string> scopes)
        => new()
        {
            Kind = AccessSimulationKind.User,
            Label = label,
            Scopes = [.. scopes ?? []],
            AccessLevel = accessLevel,
            DropSystemScopes = true,
            DropAppRoles = true
        };

    /// <summary>
    /// A tenant role. Its scopes become the whole effective set — applying a role <b>replaces</b> rather
    /// than adds, which is what makes it a simulation rather than a grant.
    /// </summary>
    /// <remarks>
    /// No access level is set. A role says nothing about a level, and inventing one would be a second
    /// assumption on top of the one the caller made.
    /// </remarks>
    public static AccessSimulation FromRole(string roleName, IEnumerable<string> scopes)
        => new()
        {
            Kind = AccessSimulationKind.Role,
            Label = roleName,
            Scopes = [.. scopes ?? []],
            DropSystemScopes = true,
            DropAppRoles = true
        };

    /// <summary>An explicit set of scopes, exactly as given.</summary>
    public static AccessSimulation FromScopes(IEnumerable<string> scopes)
    {
        var list = (scopes ?? []).ToArray();

        return new AccessSimulation
        {
            Kind = AccessSimulationKind.Scopes,
            Label = list.Length == 0 ? "no scopes" : string.Join(", ", list),
            Scopes = list,
            DropSystemScopes = true,
            DropAppRoles = true
        };
    }

    /// <summary>An access level, with the scopes that level grants.</summary>
    public static AccessSimulation FromAccessLevel(AccessLevel accessLevel, IEnumerable<string> scopes)
        => new()
        {
            Kind = AccessSimulationKind.AccessLevel,
            Label = accessLevel.ToString(),
            Scopes = [.. scopes ?? []],
            AccessLevel = accessLevel,
            DropSystemScopes = true,
            DropAppRoles = true
        };
}
