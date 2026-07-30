using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The public surface the admin views gained: opt-in audit buttons, the focus/selection pair that makes
/// cross-tab navigation work, and the team row facts. Reflection rather than rendering — the project has
/// no bUnit, so a `[Parameter]` removed or renamed would otherwise only break at a consumer's build.
/// </summary>
public class AdminViewSurfaceTests
{
    private static Type Component(string mangledName)
        => typeof(UserViewModel).Assembly.GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.User" && t.Name == mangledName);

    private static void AssertParameter(Type type, string name, Type expected)
    {
        var prop = type.GetProperty(name);
        Assert.NotNull(prop);
        Assert.Equal(expected, prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Theory]
    [InlineData("UsersListView`1")]
    [InlineData("TeamsListView`1")]
    [InlineData("UsersView`1")]
    public void AuditLogButton_IsOptIn(string component)
    {
        AssertParameter(Component(component), "ShowAuditLogButton", typeof(bool));
    }

    [Fact]
    public void UsersListView_HasCrossNavigationPair()
    {
        var type = Component("UsersListView`1");
        AssertParameter(type, "FocusUserKey", typeof(string));
        AssertParameter(type, "TeamSelected", typeof(EventCallback<string>));
    }

    [Fact]
    public void TeamsListView_HasCrossNavigationPair()
    {
        var type = Component("TeamsListView`1");
        AssertParameter(type, "FocusTeamKey", typeof(string));
        AssertParameter(type, "MemberSelected", typeof(EventCallback<string>));
    }

    [Fact]
    public void TeamViewModel_CarriesTheNewRowFacts()
    {
        Assert.NotNull(typeof(TeamViewModel).GetProperty("Icon"));
        Assert.NotNull(typeof(TeamViewModel).GetProperty("OwnerName"));
        Assert.NotNull(typeof(TeamViewModel).GetProperty("LastUsed"));
        Assert.NotNull(typeof(TeamViewModel).GetProperty("ActiveMemberCount"));
        Assert.NotNull(typeof(TeamViewModel).GetProperty("InvitedCount"));
    }

    /// <summary>
    /// `MemberCount` keeps meaning "every member row, accepted or not". It is public API and existing
    /// consumers bind to it, so the invited split had to be added alongside rather than folded into it.
    /// </summary>
    [Fact]
    public void TeamViewModel_MemberCount_IsUnchanged()
    {
        var prop = typeof(TeamViewModel).GetProperty("MemberCount");
        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop.PropertyType);
    }
}
