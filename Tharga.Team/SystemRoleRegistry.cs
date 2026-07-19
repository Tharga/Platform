namespace Tharga.Team;

/// <summary>
/// Maps app/global role names to system scopes. Role names are matched case-insensitively.
/// </summary>
public class SystemRoleRegistry : ISystemRoleRegistry
{
    private readonly Dictionary<string, string[]> _map = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string[]> All => _map;

    public void Map(string roleName, params string[] systemScopes)
    {
        if (_map.ContainsKey(roleName))
            throw new InvalidOperationException($"System role '{roleName}' is already mapped.");

        _map[roleName] = systemScopes ?? Array.Empty<string>();
    }

    /// <summary>
    /// Grants additional system scopes to a role, merging with any existing mapping. Unlike
    /// <see cref="Map"/> this never throws on an already-mapped role, so toolkit-side grants can
    /// compose on top of consumer configuration. <see cref="Map"/> stays strict, so a duplicate
    /// mapping in consumer configuration is still reported as the mistake it is.
    /// </summary>
    public void Grant(string roleName, params string[] systemScopes)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return;
        if (systemScopes == null || systemScopes.Length == 0) return;

        _map[roleName] = _map.TryGetValue(roleName, out var existing)
            ? existing.Union(systemScopes, StringComparer.Ordinal).ToArray()
            : systemScopes;
    }

    public IReadOnlyList<string> GetScopesForRoles(IEnumerable<string> roleNames)
    {
        if (roleNames == null) return Array.Empty<string>();

        return roleNames
            .Where(r => _map.ContainsKey(r))
            .SelectMany(r => _map[r])
            .Distinct()
            .ToList();
    }
}
