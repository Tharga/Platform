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
    public DateTime? LastSeen { get; init; }

    /// <summary>Set when the row has been verified against the external directory; mutable so a verify updates the badge in place.</summary>
    public DirectoryUserStatus? DirectoryStatus { get; set; }
}
