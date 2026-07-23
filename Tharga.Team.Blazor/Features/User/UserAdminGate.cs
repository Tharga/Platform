namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Gating decisions for the user administration surface. Viewing the admin lists and acting on users
/// (verify, delete) require the <c>users:manage</c> system scope — the service layer enforces the same
/// rule, so this gate is about rendering a friendly message instead of an exception. Directory features
/// additionally require a registered <see cref="IUserDirectoryService"/> — without one they are hidden
/// entirely, not disabled.
/// </summary>
public static class UserAdminGate
{
    public static bool CanAdministerUsers(bool hasUsersManageScope)
        => hasUsersManageScope;

    public static bool ShowDirectoryFeatures(bool hasUsersManageScope, bool directoryRegistered)
        => hasUsersManageScope && directoryRegistered;
}
