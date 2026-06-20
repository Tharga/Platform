using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the <c>LoginDisplay</c> team-menu role gating (Tharga/Platform#100). Mirrors the
/// reflection-based style of the other component parameter tests (no bUnit in this project).
/// </summary>
public class LoginDisplayTests
{
    [Fact]
    public void TeamMenuRoles_IsParameter_StringArray_DefaultsToNull()
    {
        var componentType = ResolveLoginDisplay();
        var prop = componentType.GetProperty("TeamMenuRoles");

        Assert.NotNull(prop);
        Assert.Equal(typeof(string[]), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());

        var instance = Activator.CreateInstance(componentType);
        Assert.Null(prop.GetValue(instance));
    }

    [Theory]
    // No role restriction -> base visibility decides (today's behavior).
    [InlineData(null, true, false, false, true)]    // team service registered, no roles configured
    [InlineData(null, false, false, false, false)]  // no team service, no override, no roles
    [InlineData(true, false, false, false, true)]   // ShowTeam=true overrides the default
    [InlineData(false, true, false, false, false)]  // ShowTeam=false hides even when default is true
    // Role restriction active -> also requires membership in at least one configured role.
    [InlineData(null, true, true, true, true)]       // visible and in a role
    [InlineData(null, true, true, false, false)]     // visible but not in any role -> hidden
    [InlineData(true, false, true, true, true)]      // ShowTeam=true and in a role
    [InlineData(true, false, true, false, false)]    // ShowTeam=true but not in any role -> hidden
    [InlineData(false, true, true, true, false)]     // ShowTeam=false hides regardless of role
    public void ShouldShowTeamMenuItem_ReturnsExpected(bool? showTeam, bool showTeamDefault, bool hasRoleRestriction, bool userInAnyRole, bool expected)
    {
        var method = ResolveLoginDisplay().GetMethod("ShouldShowTeamMenuItem", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (bool)method.Invoke(null, [showTeam, showTeamDefault, hasRoleRestriction, userInAnyRole]);

        Assert.Equal(expected, result);
    }

    private static Type ResolveLoginDisplay()
    {
        var blazorAssembly = typeof(Tharga.Team.Blazor.Framework.ThargaBlazorRegistration).Assembly;
        return blazorAssembly
            .GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.Authentication" && t.Name == "LoginDisplay");
    }
}
