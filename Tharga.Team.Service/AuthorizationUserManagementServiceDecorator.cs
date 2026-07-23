using System.Runtime.CompilerServices;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Decorator over <see cref="IUserManagementService"/> that requires the
/// <see cref="SystemUserScopes.Manage"/> system scope on every operation — verification, directory-only
/// listing, and deletion are all cross-team administration.
/// </summary>
public sealed class AuthorizationUserManagementServiceDecorator : IUserManagementService
{
    private readonly IUserManagementService _inner;
    private readonly TeamAuthorizer _authorizer;

    public AuthorizationUserManagementServiceDecorator(IUserManagementService inner, TeamAuthorizer authorizer)
    {
        _inner = inner;
        _authorizer = authorizer;
    }

    public async Task<DirectoryVerificationResult> VerifyUserAsync(string userKey, CancellationToken cancellationToken = default)
    {
        await RequireUsersManageAsync(nameof(VerifyUserAsync));
        return await _inner.VerifyUserAsync(userKey, cancellationToken);
    }

    public async IAsyncEnumerable<UserVerificationResult> VerifyAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await RequireUsersManageAsync(nameof(VerifyAllAsync));
        await foreach (var result in _inner.VerifyAllAsync(cancellationToken))
        {
            yield return result;
        }
    }

    public async Task<UserDeleteResult> DeleteUserAsync(string userKey, bool deleteFromDirectory = false, CancellationToken cancellationToken = default)
    {
        await RequireUsersManageAsync(nameof(DeleteUserAsync));
        return await _inner.DeleteUserAsync(userKey, deleteFromDirectory, cancellationToken);
    }

    public async IAsyncEnumerable<DirectoryUser> GetDirectoryOnlyUsersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await RequireUsersManageAsync(nameof(GetDirectoryOnlyUsersAsync));
        await foreach (var directoryUser in _inner.GetDirectoryOnlyUsersAsync(cancellationToken))
        {
            yield return directoryUser;
        }
    }

    private async Task RequireUsersManageAsync(string operation)
    {
        if (await _authorizer.HasSystemScopeAsync(SystemUserScopes.Manage)) return;
        throw new UnauthorizedAccessException($"{operation} requires the '{SystemUserScopes.Manage}' system scope.");
    }
}
