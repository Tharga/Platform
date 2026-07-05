using Tharga.Team;

namespace Tharga.Team.Blazor.Features.Roles;

/// <summary>
/// Mutable working copy of a custom role while it is being edited in <c>TenantRoleManager</c>.
/// </summary>
public sealed class EditableTenantRole
{
    public string Name { get; set; } = "";
    public string Description { get; set; }
    public List<string> Scopes { get; set; } = [];

    public EditableTenantRole() { }

    public EditableTenantRole(TenantRoleDefinition role)
    {
        Name = role.Name;
        Description = role.Description;
        Scopes = role.Scopes?.ToList() ?? [];
    }

    public TenantRoleDefinition ToDefinition() => new(
        Name?.Trim() ?? "",
        Scopes?.ToArray() ?? [],
        string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());
}

/// <summary>
/// The editing working-set behind <c>TenantRoleManager</c>: load current custom roles, add/remove them,
/// validate client-side (mirroring the server's privilege-escalation/collision guard for immediate
/// feedback), and project back to <see cref="TenantRoleDefinition"/> for persistence.
/// </summary>
public sealed class TenantRoleManagerModel
{
    public List<EditableTenantRole> Roles { get; private set; } = [];

    public void Load(IEnumerable<TenantRoleDefinition> roles)
        => Roles = (roles ?? []).Select(r => new EditableTenantRole(r)).ToList();

    public EditableTenantRole AddNew()
    {
        var role = new EditableTenantRole();
        Roles.Add(role);
        return role;
    }

    public void Remove(EditableTenantRole role) => Roles.Remove(role);

    public IReadOnlyList<TenantRoleDefinition> ToDefinitions()
        => Roles.Select(r => r.ToDefinition()).ToList();

    /// <summary>
    /// Client-side validation for immediate feedback: names must be non-empty, unique, and must not collide
    /// with a code-registered role name. Scope selection is constrained to registered scopes by the UI, so
    /// the server remains the authority on the privilege-escalation guard.
    /// </summary>
    public IReadOnlyList<string> Validate(IEnumerable<string> codeRoleNames)
    {
        var errors = new List<string>();
        var codeNames = new HashSet<string>(codeRoleNames ?? [], StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in Roles)
        {
            var name = role.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("A role name must not be empty.");
                continue;
            }
            if (!seen.Add(name))
                errors.Add($"Duplicate role name '{name}'.");
            if (codeNames.Contains(name))
                errors.Add($"Role '{name}' collides with a built-in role of the same name.");
        }

        return errors;
    }
}
