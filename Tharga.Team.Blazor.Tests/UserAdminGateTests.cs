using Tharga.Team;
using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Gating for the user administration surface: actions require the <c>users:manage</c> scope, and
/// directory features (verify, badge column, directory-only tab, the Entra delete opt-in)
/// additionally require a registered directory service. Pure-function tests to match the other
/// gating tests in this project (no bUnit, so razor markup cannot be asserted directly).
/// </summary>
public class UserAdminGateTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanAdministerUsers_RequiresScope(bool hasScope, bool expected)
    {
        Assert.Equal(expected, UserAdminGate.CanAdministerUsers(hasScope));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void ShowDirectoryFeatures_RequiresScopeAndRegisteredDirectory(bool hasScope, bool directoryRegistered, bool expected)
    {
        Assert.Equal(expected, UserAdminGate.ShowDirectoryFeatures(hasScope, directoryRegistered));
    }

    /// <summary>
    /// Presentational only. The same gate must never be reused to offer consent or custom-role editing —
    /// the system scope does not reach them and the service refuses.
    /// </summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanManageTeams_RequiresTheSystemScope(bool hasScope, bool expected)
    {
        Assert.Equal(expected, UserAdminGate.CanManageTeams(hasScope));
    }

    /// <summary>The repair case: the scope, on a team that has actually lost its owner.</summary>
    [Fact]
    public void CanAssignOwner_ScopedCallerOnAnOwnerlessTeam_IsAllowed()
    {
        Assert.True(UserAdminGate.CanAssignOwner(hasAssignOwnerScope: true, teamIsOwnerless: true));
    }

    /// <summary>
    /// The service refuses on a team that already has an owner, so offering the action there would be a
    /// control that throws when clicked — the defect per-team action gating already had to fix once.
    /// </summary>
    [Fact]
    public void CanAssignOwner_TeamThatHasAnOwner_IsHidden()
    {
        Assert.False(UserAdminGate.CanAssignOwner(hasAssignOwnerScope: true, teamIsOwnerless: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanAssignOwner_WithoutTheScope_IsHidden(bool teamIsOwnerless)
    {
        Assert.False(UserAdminGate.CanAssignOwner(hasAssignOwnerScope: false, teamIsOwnerless));
    }

    [Fact]
    public void CanDeleteUser_AnotherUser_IsAllowed()
    {
        Assert.True(UserAdminGate.CanDeleteUser("user-1", "user-2"));
    }

    /// <summary>The case the guard exists for: the caller's own row is never deletable.</summary>
    [Fact]
    public void CanDeleteUser_OwnRow_IsRefused()
    {
        Assert.False(UserAdminGate.CanDeleteUser("user-1", "user-1"));
    }

    /// <summary>
    /// Stricter than <c>MemberHighlight.IsCurrentMember</c> on purpose. That drives a highlight, where a
    /// false positive is cosmetic; here a false negative deletes an account, so a key differing only in
    /// case must still read as "you".
    /// </summary>
    [Theory]
    [InlineData("USER-1", "user-1")]
    [InlineData("user-1", "User-1")]
    public void CanDeleteUser_OwnRowDifferingOnlyInCase_IsRefused(string rowUserKey, string currentUserKey)
    {
        Assert.False(UserAdminGate.CanDeleteUser(rowUserKey, currentUserKey));
    }

    /// <summary>
    /// Fails closed. A null current-user key means the caller could not be established — not a state in
    /// which to offer an irreversible action, so every row refuses rather than every row allowing.
    /// </summary>
    [Theory]
    [InlineData(null, "user-1")]
    [InlineData("user-1", null)]
    [InlineData("", "user-1")]
    [InlineData("user-1", "")]
    [InlineData(null, null)]
    public void CanDeleteUser_UnknownIdentity_IsRefused(string rowUserKey, string currentUserKey)
    {
        Assert.False(UserAdminGate.CanDeleteUser(rowUserKey, currentUserKey));
    }

    [Theory]
    [InlineData(DirectoryUserStatus.Found, "Found", "Success")]
    [InlineData(DirectoryUserStatus.NotFound, "Not found", "Danger")]
    [InlineData(DirectoryUserStatus.Disabled, "Disabled", "Warning")]
    [InlineData(DirectoryUserStatus.NotLinked, "Not linked", "Secondary")]
    public void DirectoryStatusBadge_MapsEveryStatus(DirectoryUserStatus status, string text, string style)
    {
        Assert.Equal(text, DirectoryStatusBadge.Text(status));
        Assert.Equal(style, DirectoryStatusBadge.Style(status));
    }
}
