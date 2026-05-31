namespace Tharga.Team;

/// <summary>
/// Stores scope definitions and resolves effective scopes for a given access level.
/// Owner and Administrator get all registered scopes.
/// User gets scopes registered at User or Viewer level.
/// Viewer gets only scopes registered at Viewer level.
/// Custom gets no base scopes at all (exempt from the Owner/Administrator all-scopes rule);
/// its effective scopes come solely from roles and scope overrides.
/// Role scopes are unioned with access level scopes.
/// </summary>
public class ScopeRegistry : IScopeRegistry
{
    private readonly List<ScopeDefinition> _scopes = new();
    private ITenantRoleRegistry _roleRegistry;

    public void SetRoleRegistry(ITenantRoleRegistry roleRegistry)
    {
        _roleRegistry = roleRegistry;
    }

    public IReadOnlyList<ScopeDefinition> All => _scopes;

    public void Register(string scopeName, AccessLevel defaultMinimumLevel)
    {
        if (_scopes.Any(s => s.Name == scopeName))
            throw new InvalidOperationException($"Scope '{scopeName}' is already registered.");

        _scopes.Add(new ScopeDefinition(scopeName, defaultMinimumLevel));
    }

    public IReadOnlyList<string> GetScopesForAccessLevel(AccessLevel accessLevel)
    {
        // Custom grants no base scopes — effective scopes come solely from roles and overrides.
        // Explicit guard so the invariant holds even if a scope is ever registered at Custom level.
        if (accessLevel == AccessLevel.Custom)
            return Array.Empty<string>();

        if (accessLevel <= AccessLevel.Administrator)
            return _scopes.Select(s => s.Name).ToList();

        return _scopes
            .Where(s => s.DefaultMinimumLevel >= accessLevel)
            .Select(s => s.Name)
            .ToList();
    }

    public IReadOnlyList<string> GetEffectiveScopes(AccessLevel accessLevel, IEnumerable<string> roleNames, IEnumerable<string> scopeOverrides = null)
    {
        var accessLevelScopes = GetScopesForAccessLevel(accessLevel);

        var roleScopes = _roleRegistry != null && roleNames != null
            ? _roleRegistry.GetScopesForRoles(roleNames)
            : Array.Empty<string>();

        var overrides = scopeOverrides ?? Array.Empty<string>();

        return accessLevelScopes
            .Union(roleScopes)
            .Union(overrides)
            .Distinct()
            .ToList();
    }
}
