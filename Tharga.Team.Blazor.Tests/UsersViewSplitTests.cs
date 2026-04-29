using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Smoke tests verifying the public surface introduced when <see cref="UsersView{TMember}"/>
/// was split into <c>UsersListView</c> + <c>TeamsListView</c>.
/// </summary>
public class UsersViewSplitTests
{
    [Fact]
    public void UserViewModel_IsPublic()
    {
        Assert.True(typeof(UserViewModel).IsPublic);
    }

    [Fact]
    public void UserTeamInfo_IsPublic()
    {
        Assert.True(typeof(UserTeamInfo).IsPublic);
    }

    [Fact]
    public void TeamViewModel_IsPublic()
    {
        Assert.True(typeof(TeamViewModel).IsPublic);
    }

    [Fact]
    public void TeamMemberInfo_IsPublic()
    {
        Assert.True(typeof(TeamMemberInfo).IsPublic);
    }

    [Fact]
    public void UsersListView_HasActionsTemplateParameter()
    {
        var listType = ResolveGenericComponent("UsersListView`1");
        var prop = listType.GetProperty("ActionsTemplate");
        Assert.NotNull(prop);
        Assert.Equal(typeof(RenderFragment<UserViewModel>), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void TeamsListView_HasTeamActionsAndMemberActionsParameters()
    {
        var listType = ResolveGenericComponent("TeamsListView`1");

        var teamProp = listType.GetProperty("TeamActionsTemplate");
        Assert.NotNull(teamProp);
        Assert.Equal(typeof(RenderFragment<TeamViewModel>), teamProp.PropertyType);
        Assert.NotNull(teamProp.GetCustomAttribute<ParameterAttribute>());

        var memberProp = listType.GetProperty("MemberActionsTemplate");
        Assert.NotNull(memberProp);
        Assert.Equal(typeof(RenderFragment<TeamMemberInfo>), memberProp.PropertyType);
        Assert.NotNull(memberProp.GetCustomAttribute<ParameterAttribute>());
    }

    private static Type ResolveGenericComponent(string mangledName)
    {
        return typeof(UserViewModel).Assembly.GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.User" && t.Name == mangledName);
    }
}
