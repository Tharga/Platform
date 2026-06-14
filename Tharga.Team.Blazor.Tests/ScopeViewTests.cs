using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Smoke tests for the <c>ScopeView</c> component parameters. Mirrors the reflection-based style of the
/// other component parameter tests (no bUnit in this project).
/// </summary>
public class ScopeViewTests
{
    [Theory]
    [InlineData("ShowDescription", true)]
    [InlineData("ShowRoles", true)]
    [InlineData("AllowGridSorting", true)]
    [InlineData("AllowGridFiltering", false)]
    public void BoolParameter_HasExpectedDefault_AndParameterAttribute(string propertyName, bool expectedDefault)
    {
        var componentType = ResolveScopeView();
        var prop = componentType.GetProperty(propertyName);

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());

        var instance = Activator.CreateInstance(componentType);
        Assert.Equal(expectedDefault, (bool)prop.GetValue(instance));
    }

    private static Type ResolveScopeView()
    {
        var blazorAssembly = typeof(Tharga.Team.Blazor.Framework.ThargaBlazorRegistration).Assembly;
        return blazorAssembly
            .GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.Scopes" && t.Name == "ScopeView");
    }
}
