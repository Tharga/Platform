using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Scopes;

/// <summary>
/// One row of the scope reference: a configured scope, its description, and who grants it
/// (the access levels and tenant roles).
/// </summary>
public sealed record ScopeRow(
    string Name,
    string Description,
    IReadOnlyList<AccessLevel> AccessLevels,
    IReadOnlyList<string> Roles);

/// <summary>
/// Builds the read-only scope reference shown by <c>ScopeView</c> directly from the registries, so it
/// always reflects the live configuration. Pure (no DI, no rendering) to keep it unit-testable.
/// </summary>
public static class ScopeReference
{
    // Access levels that grant scopes by default. Custom grants nothing by default and is omitted.
    private static readonly AccessLevel[] Levels =
        [AccessLevel.Owner, AccessLevel.Administrator, AccessLevel.User, AccessLevel.Viewer];

    /// <summary>
    /// Projects every registered scope to a <see cref="ScopeRow"/>, resolving which access levels and
    /// tenant roles grant it. Returns an empty list when no scope registry is configured.
    /// </summary>
    /// <param name="scopes">The configured scope registry, or null when scopes are not configured.</param>
    /// <param name="roles">The configured tenant role registry, or null when roles are not configured.</param>
    public static IReadOnlyList<ScopeRow> Build(IScopeRegistry scopes, ITenantRoleRegistry roles)
    {
        if (scopes == null) return Array.Empty<ScopeRow>();

        // Resolve level -> scope-set via the registry's own logic rather than reimplementing the math.
        var byLevel = Levels.ToDictionary(l => l, l => new HashSet<string>(scopes.GetScopesForAccessLevel(l)));
        var roleList = roles?.All ?? Array.Empty<TenantRoleDefinition>();

        return scopes.All
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => new ScopeRow(
                s.Name,
                s.Description,
                Levels.Where(l => byLevel[l].Contains(s.Name)).ToList(),
                roleList.Where(r => r.Scopes != null && r.Scopes.Contains(s.Name)).Select(r => r.Name).ToList()))
            .ToList();
    }
}
