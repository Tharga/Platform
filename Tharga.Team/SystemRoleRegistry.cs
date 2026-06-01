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
