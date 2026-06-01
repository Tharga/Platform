namespace Tharga.Team;

/// <summary>
/// Defines a system-level scope (a global capability granted to system API keys and, via role mapping, to
/// privileged users). Unlike team scopes, system scopes have no access-level hierarchy — they are a flat set.
/// </summary>
public record SystemScopeDefinition(string Name, string Description = null);
