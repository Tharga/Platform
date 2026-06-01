namespace Tharga.Team;

/// <summary>
/// Defines a tenant role with its associated scopes and an optional human-readable description
/// (shown as a tooltip in the role picker, alongside the scopes the role grants).
/// </summary>
public record TenantRoleDefinition(string Name, IReadOnlyList<string> Scopes, string Description = null);
