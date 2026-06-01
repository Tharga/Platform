namespace Tharga.Team;

/// <summary>
/// Defines a scope with its default minimum access level and an optional human-readable description
/// (shown as a tooltip in the scope picker).
/// </summary>
public record ScopeDefinition(string Name, AccessLevel DefaultMinimumLevel, string Description = null);
