namespace Tharga.Team;

/// <summary>
/// Pluggable connection to an external user directory (e.g. Microsoft Entra ID). Optional — registered
/// via <c>AddUserDirectoryService&lt;T&gt;()</c> on the platform options; when not registered, directory
/// features (verify, directory-only listing, directory delete) are unavailable and their UI is hidden.
/// </summary>
public interface IUserDirectoryService
{
    /// <summary>
    /// Verify that a local user still exists (and is enabled) in the directory. Resolves by the user's
    /// stored <see cref="IUser.DirectoryId"/> when set, otherwise falls back to matching by email.
    /// </summary>
    Task<DirectoryVerificationResult> VerifyUserAsync(IUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user from the directory. For Entra this is a soft delete: the user is restorable by an
    /// administrator for 30 days, but is immediately signed-out-of and removed org-wide.
    /// </summary>
    Task DeleteUserAsync(string directoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerate all users in the directory, streamed page by page.
    /// </summary>
    IAsyncEnumerable<DirectoryUser> GetUsersAsync(CancellationToken cancellationToken = default);
}
