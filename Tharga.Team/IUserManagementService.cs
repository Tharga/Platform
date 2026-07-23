namespace Tharga.Team;

/// <summary>
/// User administration operations: directory verification and user deletion. Authorization is enforced
/// in the service layer by an authorization decorator; the <c>[RequireScope]</c> attributes here document
/// the scope each operation requires. All operations require the <see cref="SystemUserScopes.Manage"/>
/// system scope. Directory-backed operations require a registered <see cref="IUserDirectoryService"/>.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Verify a local user against the external directory. When the user resolves via email fallback,
    /// the found directory id is persisted on the user (relink).
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    Task<DirectoryVerificationResult> VerifyUserAsync(string userKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify all local users against the external directory, streamed as results arrive.
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    IAsyncEnumerable<UserVerificationResult> VerifyAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user: removes the user from every team and deletes the user record (audited).
    /// With <paramref name="deleteFromDirectory"/> the user is also deleted from the external directory;
    /// a directory failure does not roll back the local delete — it is reported on the result.
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    Task<UserDeleteResult> DeleteUserAsync(string userKey, bool deleteFromDirectory = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// List directory users that have no matching local user (matched by directory id, falling back to
    /// email), streamed as directory pages arrive.
    /// </summary>
    [RequireScope(SystemUserScopes.Manage)]
    IAsyncEnumerable<DirectoryUser> GetDirectoryOnlyUsersAsync(CancellationToken cancellationToken = default);
}
