namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Presentation mapping for a user's directory verification status. Returns Radzen
/// <c>BadgeStyle</c> names as strings (parsed in markup), mirroring <c>TeamVisibility.BadgeStyle</c>.
/// </summary>
public static class DirectoryStatusBadge
{
    public static string Text(DirectoryUserStatus status) => status switch
    {
        DirectoryUserStatus.Found => "Found",
        DirectoryUserStatus.NotFound => "Not found",
        DirectoryUserStatus.Disabled => "Disabled",
        DirectoryUserStatus.NotLinked => "Not linked",
        _ => status.ToString()
    };

    public static string Style(DirectoryUserStatus status) => status switch
    {
        DirectoryUserStatus.Found => "Success",
        DirectoryUserStatus.NotFound => "Danger",
        DirectoryUserStatus.Disabled => "Warning",
        DirectoryUserStatus.NotLinked => "Secondary",
        _ => "Info"
    };
}
