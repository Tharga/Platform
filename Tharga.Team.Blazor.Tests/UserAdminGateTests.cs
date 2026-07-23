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
