namespace Tharga.Team;

public interface ITeam
{
    string Key { get; }
    string Name { get; }
    string Icon { get; }

    /// <summary>
    /// Global roles that have been granted viewer access to this team via consent.
    /// Null or empty means no consent granted.
    /// </summary>
    string[] ConsentedRoles => null;
}

public interface ITeam<TMember> : ITeam
    where TMember : ITeamMember
{
    public TMember[] Members { get; init; }
}
