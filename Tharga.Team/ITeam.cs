namespace Tharga.Team;

public interface ITeam
{
    string Key { get; }
    string Name { get; }
    string Icon { get; }

    /// <summary>
    /// Global roles that have been granted access to this team via consent.
    /// Null or empty means no consent granted.
    /// </summary>
    string[] ConsentedRoles => null;

    /// <summary>
    /// Access level granted to consented roles. Null falls back to the configured default consent level.
    /// </summary>
    AccessLevel? ConsentAccessLevel => null;
}

public interface ITeam<TMember> : ITeam
    where TMember : ITeamMember
{
    public TMember[] Members { get; init; }
}
