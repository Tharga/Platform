using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Every <see cref="IconOptions"/> property configured on the options object must reach the container
/// (Tharga/Team#177). <c>RegisterIcons</c> copied only <c>MaxBytes</c> and <c>AllowedContentTypes</c>, so
/// setting <c>MaxUploadBytes</c> or <c>MaxDimension</c> compiled, read naturally, and did nothing.
/// </summary>
/// <remarks>
/// <b>The durability test is the point of this file.</b> Asserting the two missing properties would fix today
/// and leave the next added property to fail the same silent way, which is exactly how this defect and #157
/// both happened. <see cref="EveryProperty_ReachesTheContainer"/> drives itself from
/// <see cref="IconOptions"/>'s own shape, so a property added tomorrow is covered without anyone remembering.
/// </remarks>
public class IconOptionsForwardingTests
{
    private const string ValidAzureAdConfig = """
        { "AzureAd": { "Authority": "https://test.ciamlogin.com/test", "ClientId": "c", "TenantId": "t", "CallbackPath": "/signin-oidc" } }
        """;

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ValidAzureAdConfig));
        builder.Configuration.AddJsonStream(stream);
        return builder;
    }

    private static readonly string[] CustomContentTypes = ["image/png", "image/avif"];

    private static void ConfigureAll(IconOptions icon)
    {
        icon.MaxBytes = 12_345;
        icon.MaxUploadBytes = 24 * 1024 * 1024;
        icon.MaxDimension = 512;
        icon.AllowedContentTypes = CustomContentTypes;
    }

    private static void AssertAll(IconOptions resolved)
    {
        Assert.Equal(12_345, resolved.MaxBytes);
        Assert.Equal(24 * 1024 * 1024, resolved.MaxUploadBytes);
        Assert.Equal(512, resolved.MaxDimension);
        Assert.Equal(CustomContentTypes, resolved.AllowedContentTypes);
    }

    /// <summary>The reported path: the facade's own options object.</summary>
    [Fact]
    public void TheFacadePath_ForwardsEveryProperty()
    {
        var builder = CreateBuilder();
        builder.AddThargaTeam(o => ConfigureAll(o.Icon));

        using var provider = builder.Services.BuildServiceProvider();

        AssertAll(provider.GetRequiredService<IOptions<IconOptions>>().Value);
    }

    /// <summary>
    /// And the granular path, which is where the copy actually lives — both entry points converge on it
    /// because the facade forwards its whole <c>Icon</c> instance down.
    /// </summary>
    [Fact]
    public void TheGranularPath_ForwardsEveryProperty()
    {
        var services = new ServiceCollection();
        services.AddThargaTeamBlazor(o => ConfigureAll(o.Icon));

        using var provider = services.BuildServiceProvider();

        AssertAll(provider.GetRequiredService<IOptions<IconOptions>>().Value);
    }

    /// <summary>
    /// Drives itself from <see cref="IconOptions"/>'s own properties: sets each to a value distinct from its
    /// default and asserts it survives into the container. A property added later is covered automatically —
    /// and a property whose type this test cannot set fails loudly rather than being skipped, which is the
    /// behaviour that keeps the coverage honest.
    /// </summary>
    [Fact]
    public void EveryProperty_ReachesTheContainer()
    {
        var properties = OptionsForwarder.ForwardableProperties<IconOptions>().ToArray();

        // A source scan matching nothing passes forever while reading as "everything checked".
        Assert.True(properties.Length >= 4, $"Expected at least the four known IconOptions properties, found {properties.Length}.");

        var expected = new Dictionary<string, object>();
        var services = new ServiceCollection();
        services.AddThargaTeamBlazor(o =>
        {
            foreach (var property in properties)
            {
                var value = DistinctValueFor(property.PropertyType, property.Name);
                property.SetValue(o.Icon, value);
                expected[property.Name] = value;
            }
        });

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<IconOptions>>().Value;

        foreach (var property in properties)
        {
            Assert.Equal(expected[property.Name], property.GetValue(resolved));
        }
    }

    private static object DistinctValueFor(Type type, string propertyName)
    {
        if (type == typeof(int)) return 4_242;
        if (type == typeof(long)) return 4_242L;
        if (type == typeof(bool)) return true;
        if (type == typeof(string)) return $"forwarded-{propertyName}";
        if (type == typeof(IReadOnlyCollection<string>)) return CustomContentTypes;

        throw new NotSupportedException(
            $"IconOptions.{propertyName} is a '{type.Name}', which this test does not know how to set. Teach it " +
            "how, rather than excluding the property — the point of this test is that a new property cannot " +
            "silently stop being forwarded (Tharga/Team#177).");
    }
}
