namespace Tharga.Team;

/// <summary>
/// The selection a <c>SelectedTeamChangedEvent</c> is reporting.
/// </summary>
/// <remarks>
/// These arguments carry the team itself, so a handler has everything it needs without asking for the
/// selection again. Resolving it again from inside a handler is the shape to avoid — the resolve is not a
/// plain read.
/// </remarks>
public class SelectedTeamChangedEventArgs : EventArgs
{
    public SelectedTeamChangedEventArgs(ITeam selectedTeam)
    {
        SelectedTeam = selectedTeam;
    }

    /// <summary>
    /// The team now selected, or <c>null</c> when the caller has no team.
    /// </summary>
    public ITeam SelectedTeam { get; }
}
