using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Smoke tests for the <c>ApiKeyView.ChipTagKeys</c> parameter added under
/// <see href="https://github.com/Tharga/Platform/issues/75">Tharga/Platform#75</see>.
/// </summary>
public class ApiKeyViewTagsTests
{
    [Fact]
    public void ApiKeyView_HasChipTagKeysParameter_DefaultsEmpty()
    {
        var componentType = ResolveApiKeyView();
        var prop = componentType.GetProperty("ChipTagKeys");

        Assert.NotNull(prop);
        Assert.Equal(typeof(string[]), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());

        var instance = Activator.CreateInstance(componentType);
        var value = (string[])prop.GetValue(instance);
        Assert.NotNull(value);
        Assert.Empty(value);
    }

    private static Type ResolveApiKeyView()
    {
        var blazorAssembly = typeof(Tharga.Team.Blazor.Framework.ThargaBlazorRegistration).Assembly;
        return blazorAssembly
            .GetTypes()
            .Single(t => t.Namespace == "Tharga.Team.Blazor.Features.Api" && t.Name == "ApiKeyView");
    }
}
