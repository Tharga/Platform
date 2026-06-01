namespace Tharga.Team;

/// <summary>
/// Registry of system-level (global) scopes — the capabilities a system API key (or a privileged role) may
/// hold. Separate from the team <see cref="IScopeRegistry"/>, which is access-level based and team-scoped.
/// </summary>
public interface ISystemScopeRegistry
{
    IReadOnlyList<SystemScopeDefinition> All { get; }
}
