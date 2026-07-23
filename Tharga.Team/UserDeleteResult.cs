namespace Tharga.Team;

/// <summary>
/// Result of <see cref="IUserManagementService.DeleteUserAsync"/>. The local delete (all team memberships
/// and the user record) has always completed when this is returned — a failure there throws instead.
/// </summary>
/// <param name="DirectoryDeleted">True when the user was also deleted from the external directory.</param>
/// <param name="DirectoryError">
/// When directory deletion was requested but failed (or the user was not linked to a directory user),
/// the reason — the local delete is not rolled back.
/// </param>
/// <param name="RemovedTeamCount">The number of teams the user was removed from.</param>
public sealed record UserDeleteResult(bool DirectoryDeleted = false, string DirectoryError = null, int RemovedTeamCount = 0);
