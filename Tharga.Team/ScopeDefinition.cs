namespace Tharga.Team;

/// <summary>
/// Defines a scope with its default minimum access level and an optional human-readable description
/// (shown as a tooltip in the scope picker). <see cref="Implies"/> lets a scope act as an umbrella that
/// subsumes other scopes: a principal granted this scope effectively holds the implied ones too.
/// </summary>
public record ScopeDefinition(string Name, AccessLevel DefaultMinimumLevel, string Description = null, IReadOnlyList<string> Implies = null);
