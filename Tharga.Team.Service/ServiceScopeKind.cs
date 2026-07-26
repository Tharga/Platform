namespace Tharga.Team.Service;

/// <summary>
/// Whether a service's operations act on one team or across the whole system. Declared once per service
/// at registration rather than per method, so a method added later inherits the rule instead of needing
/// an annotation somebody has to remember.
/// </summary>
public enum ServiceScopeKind
{
    /// <summary>
    /// Every operation acts on a team, named by the call's first argument, and the caller's scope must
    /// have been issued for that same team.
    /// </summary>
    Team,

    /// <summary>
    /// No operation acts on a team. The scope is a system scope, granted from app roles, and no team need
    /// be selected.
    /// </summary>
    System
}
