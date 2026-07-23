namespace Tharga.Team;

/// <summary>
/// Per-user item in a bulk verification stream (<see cref="IUserManagementService.VerifyAllAsync"/>).
/// </summary>
/// <param name="UserKey">The local user's key.</param>
/// <param name="Result">The user's directory verification result.</param>
public sealed record UserVerificationResult(string UserKey, DirectoryVerificationResult Result);
