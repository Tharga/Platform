namespace Tharga.Team;

/// <summary>
/// Pluggable connection to an external user directory (e.g. Microsoft Entra ID). Optional — registered
/// via <c>AddUserDirectoryService&lt;T&gt;()</c> on the platform options; when not registered, directory
/// features (verify, directory-only listing, directory delete) are unavailable and their UI is hidden.
/// </summary>
public interface IUserDirectoryService
{
    /// <summary>
    /// Whether this directory has everything it needs to answer. A registration that is missing
    /// configuration reports <c>false</c>, and callers treat it exactly as they treat no registration at
    /// all — directory features stay hidden rather than being offered and then failing.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>, so an implementation with nothing to configure — or one written before
    /// this member existed — needs no change.
    /// <para>
    /// The alternative was to let the first call fail, which is what the Entra provider used to do: it
    /// threw <c>InvalidOperationException</c> on the first Graph request, long after registration
    /// appeared to succeed and from a place that named neither the registration nor the missing setting.
    /// An unmet prerequisite should be reported where it can be acted on, not where it happens to be
    /// noticed.
    /// </para>
    /// </remarks>
    bool IsConfigured => true;

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
