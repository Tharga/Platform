namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// View model for a user row in the users list view.
/// </summary>
public record UserViewModel
{
    public string Key { get; init; }

    /// <summary>The name to display — the stored name, or one resolved from the email when none is set.</summary>
    public string Name { get; init; }

    /// <summary>
    /// The stored <see cref="IUser.Name"/> exactly as persisted, null when the user has never set one.
    /// Editing must bind to this rather than <see cref="Name"/>, so that a resolved fallback is not
    /// silently promoted into a real stored name.
    /// </summary>
    public string StoredName { get; init; }

    public string EMail { get; init; }
    public string Icon { get; init; }
    public int TeamCount { get; init; }
    public UserTeamInfo[] Teams { get; init; }
    public DateTime? LastSeen { get; init; }

    /// <summary>Set when the row has been verified against the external directory; mutable so a verify updates the badge in place.</summary>
    public DirectoryUserStatus? DirectoryStatus { get; set; }
}
