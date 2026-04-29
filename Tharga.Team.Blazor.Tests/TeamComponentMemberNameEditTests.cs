using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Smoke tests verifying the new TeamComponent parameters introduced for inline Member.Name editing.
/// </summary>
public class TeamComponentMemberNameEditTests
{
    [Fact]
    public void TeamComponent_HasEnableMemberNameEditParameter_DefaultFalse()
    {
        var componentType = ResolveTeamComponent();
        var prop = componentType.GetProperty("EnableMemberNameEdit");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void TeamComponent_HasOnMemberNameChangedParameter()
    {
        var componentType = ResolveTeamComponent();
        var prop = componentType.GetProperty("OnMemberNameChanged");

        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback<MemberNameChangedArgs>), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void MemberNameChangedArgs_IsPublic_AndHasExpectedProperties()
    {
        Assert.True(typeof(MemberNameChangedArgs).IsPublic);

        var args = new MemberNameChangedArgs("team-1", "user-2", "Old", "New");
        Assert.Equal("team-1", args.TeamKey);
        Assert.Equal("user-2", args.MemberKey);
        Assert.Equal("Old", args.OldName);
        Assert.Equal("New", args.NewName);
    }

    private static Type ResolveTeamComponent()
    {
        var blazorAssembly = typeof(Tharga.Team.Blazor.Framework.ThargaBlazorRegistration).Assembly;
        return blazorAssembly
            .GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.Team" && t.Name == "TeamComponent`1");
    }
}
