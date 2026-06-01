namespace Tharga.Team;

/// <summary>
/// Stores tenant role definitions and resolves scopes for assigned roles.
/// </summary>
public class TenantRoleRegistry : ITenantRoleRegistry
{
    private readonly List<TenantRoleDefinition> _roles = new();

    public IReadOnlyList<TenantRoleDefinition> All => _roles;

    public void Register(string roleName, params string[] scopes) => Register(roleName, scopes, null);

    /// <summary>Registers a tenant role with an optional human-readable description.</summary>
    public void Register(string roleName, string[] scopes, string description)
    {
        if (_roles.Any(r => r.Name == roleName))
            throw new InvalidOperationException($"Tenant role '{roleName}' is already registered.");

        _roles.Add(new TenantRoleDefinition(roleName, scopes, description));
    }

    public TenantRoleDefinition Get(string roleName)
    {
        return _roles.FirstOrDefault(r => r.Name == roleName)
               ?? throw new KeyNotFoundException($"Tenant role '{roleName}' is not registered.");
    }

    public IReadOnlyList<string> GetScopesForRoles(IEnumerable<string> roleNames)
    {
        if (roleNames == null) return Array.Empty<string>();

        return roleNames
            .SelectMany(name =>
            {
                var role = _roles.FirstOrDefault(r => r.Name == name);
                return role?.Scopes ?? Enumerable.Empty<string>();
            })
            .Distinct()
            .ToList();
    }
}
