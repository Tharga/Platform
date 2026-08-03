using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// Builds an <see cref="AccessSimulation"/> from each of the ways a target can be named.
/// </summary>
/// <remarks>
/// The four kinds are not four features. Each names a <b>target scope set</b>, and the simulation is
/// always the same operation afterwards: keep what the target has and the caller also has, remove the
/// rest. Keeping the construction in one place is what makes that true rather than aspirational.
/// </remarks>
internal static class AccessSimulationTargets
{
    /// <summary>
    /// Another member of the selected team, from the grant they actually hold there.
    /// </summary>
    /// <remarks>
    /// <b>Drops system scopes and app roles</b>, because they cannot be computed for anyone else:
    /// <c>ISystemRoleRegistry</c> maps app roles issued by the identity provider, which the toolkit does
    /// not store. Their system access is unknown rather than empty, so showing the caller's own would be
    /// a claim about the target that nothing supports.
    /// </remarks>
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
            Scopes = [.. scopes ?? []]
        };

    /// <summary>An explicit set of scopes, exactly as given.</summary>
    public static AccessSimulation FromScopes(IEnumerable<string> scopes)
    {
        var list = (scopes ?? []).ToArray();

        return new AccessSimulation
        {
            Kind = AccessSimulationKind.Scopes,
            Label = list.Length == 0 ? "no scopes" : string.Join(", ", list),
            Scopes = list
        };
    }

    /// <summary>An access level, with the scopes that level grants.</summary>
    public static AccessSimulation FromAccessLevel(AccessLevel accessLevel, IEnumerable<string> scopes)
        => new()
        {
            Kind = AccessSimulationKind.AccessLevel,
            Label = accessLevel.ToString(),
            Scopes = [.. scopes ?? []],
            AccessLevel = accessLevel
        };
}
