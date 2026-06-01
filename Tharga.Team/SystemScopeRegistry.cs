namespace Tharga.Team;

/// <summary>
/// Stores system-level scope definitions. A flat set (no access level) — system keys carry an explicit list,
/// and privileged roles map to a subset.
/// </summary>
public class SystemScopeRegistry : ISystemScopeRegistry
{
    private readonly List<SystemScopeDefinition> _scopes = new();

    public IReadOnlyList<SystemScopeDefinition> All => _scopes;

    public void Register(string scopeName, string description = null)
    {
        if (_scopes.Any(s => s.Name == scopeName))
            throw new InvalidOperationException($"System scope '{scopeName}' is already registered.");

        _scopes.Add(new SystemScopeDefinition(scopeName, description));
    }
}
