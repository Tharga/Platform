namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// View model for a user row in the users list view.
/// </summary>
public record UserViewModel
{
    public string Key { get; init; }
    public string Name { get; init; }
    public string EMail { get; init; }
    public int TeamCount { get; init; }
    public UserTeamInfo[] Teams { get; init; }
}
