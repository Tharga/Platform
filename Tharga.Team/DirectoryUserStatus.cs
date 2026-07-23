namespace Tharga.Team;

/// <summary>
/// Outcome of verifying a local user against the external directory (<see cref="IUserDirectoryService.VerifyUserAsync"/>).
/// </summary>
public enum DirectoryUserStatus
{
    /// <summary>The user exists in the directory and the account is enabled.</summary>
    Found,

    /// <summary>The user does not exist in the directory (deleted or never existed).</summary>
    NotFound,

    /// <summary>The user exists in the directory but the account is disabled.</summary>
    Disabled,

    /// <summary>
    /// The local user could not be matched to a directory user: no directory id is stored and
    /// no directory user matches the user's email.
    /// </summary>
    NotLinked
}
