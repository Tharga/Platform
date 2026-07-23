using System.Runtime.CompilerServices;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Default <see cref="IUserManagementService"/> implementation. Directory operations require a registered
/// <see cref="IUserDirectoryService"/>; deletion composes the storage seams — remove from all teams, then
/// delete the user record — and only then attempts the (opt-in) directory delete, so a directory failure
/// never leaves the local store half-deleted. Authorization and audit are applied by decorators.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IUserService _userService;
    private readonly ITeamService _teamService;
    private readonly IUserDirectoryService _directoryService;

    public UserManagementService(IUserService userService, ITeamService teamService, IUserDirectoryService directoryService = null)
    {
        _userService = userService;
        _teamService = teamService;
        _directoryService = directoryService;
    }

    public async Task<DirectoryVerificationResult> VerifyUserAsync(string userKey, CancellationToken cancellationToken = default)
    {
        var directory = RequireDirectoryService();
        var user = await RequireUserAsync(userKey);

        var result = await directory.VerifyUserAsync(user, cancellationToken);
        await RelinkAsync(user, result);

        return result;
    }

    public async IAsyncEnumerable<UserVerificationResult> VerifyAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var directory = RequireDirectoryService();

        await foreach (var user in _userService.GetAsync().WithCancellation(cancellationToken))
        {
            var result = await directory.VerifyUserAsync(user, cancellationToken);
            await RelinkAsync(user, result);
            yield return new UserVerificationResult(user.Key, result);
        }
    }

    public async Task<UserDeleteResult> DeleteUserAsync(string userKey, bool deleteFromDirectory = false, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userKey);

        // The directory id must be resolved before the local record is gone (email fallback reads it).
        string directoryId = null;
        string directoryError = null;
        if (deleteFromDirectory)
        {
            if (_directoryService == null)
            {
                directoryError = "No user directory service is registered.";
            }
            else
            {
                directoryId = user.DirectoryId;
                if (string.IsNullOrEmpty(directoryId))
                {
                    var verification = await _directoryService.VerifyUserAsync(user, cancellationToken);
                    directoryId = verification?.DirectoryId;
                }

                if (string.IsNullOrEmpty(directoryId))
                {
                    directoryError = "The user could not be matched to a directory user.";
                }
            }
        }

        var removedTeamCount = await _teamService.RemoveUserFromAllTeamsAsync(user.Key);
        await _userService.DeleteUserAsync(user.Key);

        var directoryDeleted = false;
        if (deleteFromDirectory && directoryError == null)
        {
            try
            {
                await _directoryService.DeleteUserAsync(directoryId, cancellationToken);
                directoryDeleted = true;
            }
            catch (Exception ex)
            {
                directoryError = ex.Message;
            }
        }

        return new UserDeleteResult(directoryDeleted, directoryError, removedTeamCount);
    }

    public async IAsyncEnumerable<DirectoryUser> GetDirectoryOnlyUsersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var directory = RequireDirectoryService();

        var directoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var user in _userService.GetAsync().WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(user.DirectoryId)) directoryIds.Add(user.DirectoryId);
            if (!string.IsNullOrEmpty(user.EMail)) emails.Add(user.EMail);
        }

        await foreach (var directoryUser in directory.GetUsersAsync(cancellationToken))
        {
            if (!string.IsNullOrEmpty(directoryUser.DirectoryId) && directoryIds.Contains(directoryUser.DirectoryId)) continue;
            if (!string.IsNullOrEmpty(directoryUser.EMail) && emails.Contains(directoryUser.EMail)) continue;
            yield return directoryUser;
        }
    }

    private async Task RelinkAsync(IUser user, DirectoryVerificationResult result)
    {
        if (string.IsNullOrEmpty(result?.DirectoryId)) return;
        if (string.Equals(result.DirectoryId, user.DirectoryId, StringComparison.OrdinalIgnoreCase)) return;

        await _userService.SetUserDirectoryIdAsync(user.Key, result.DirectoryId);
    }

    private IUserDirectoryService RequireDirectoryService()
        => _directoryService ?? throw new NotSupportedException(
            $"No {nameof(IUserDirectoryService)} is registered. Register one to use directory features.");

    private async Task<IUser> RequireUserAsync(string userKey)
    {
        var user = await _userService.GetUserByKeyAsync(userKey);
        return user ?? throw new InvalidOperationException($"User '{userKey}' was not found.");
    }
}
