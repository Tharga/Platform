namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Localizable strings for the team profile menu (<c>LoginDisplay</c>) and <c>TeamSelector</c>.
/// Each entry carries its stable key and English default; pass them to <see cref="IThargaTextProvider.Get"/>.
/// </summary>
public static class TeamMenuText
{
    public static readonly TextKey User = new("team.menu.user", "User");
    public static readonly TextKey Team = new("team.menu.team", "Team");
    public static readonly TextKey Logout = new("team.menu.logout", "Logout");
    public static readonly TextKey Login = new("team.menu.login", "Login");
    public static readonly TextKey CreateTeam = new("team.menu.createTeam", "Create Team");
    public static readonly TextKey Loading = new("team.menu.loading", "Loading...");
}
