namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Gating decisions for the user administration surface. User actions (verify, delete) require the
/// <c>users:manage</c> system scope; directory features additionally require a registered
/// <see cref="IUserDirectoryService"/> — without one they are hidden entirely, not disabled.
/// </summary>
public static class UserAdminGate
{
    public static bool ShowUserActions(bool hasUsersManageScope)
        => hasUsersManageScope;

    public static bool ShowDirectoryFeatures(bool hasUsersManageScope, bool directoryRegistered)
        => hasUsersManageScope && directoryRegistered;
}
