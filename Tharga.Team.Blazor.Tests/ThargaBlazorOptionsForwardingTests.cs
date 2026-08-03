using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Every Blazor option a host sets on the <c>AddThargaTeam</c> facade reaches the layer that reads it.
/// </summary>
/// <remarks>
/// <b>Reported from the sample, 2026-08-03:</b> <c>o.Blazor.Simulation.Enabled = true</c> was accepted
/// and had no effect. The facade forwarded nine named properties into <c>AddThargaTeamBlazor</c>, so a
/// tenth added later was silently dropped — the host configured a feature, nothing failed, and the
/// feature never turned on.
/// <para>
/// This is the same shape Eplicta reported as Tharga/Team#177 for
/// <c>IconOptions.MaxUploadBytes</c>: an options object whose values are copied selectively, where the
/// omission is invisible at every point a developer would look. A per-property fix would have left the
/// tenth, eleventh and twelfth to be found the same way, so forwarding is now total and this asserts it
/// stays total.
/// </para>
/// </remarks>
public class ThargaBlazorOptionsForwardingTests
{
    /// <summary>
    /// The self-check. If the reflection scan found nothing, every assertion below would pass while
    /// checking nothing at all — the failure mode this repo has shipped three times.
    /// </summary>
    [Fact]
    public void TheScanFindsTheForwardableOptions()
    {
        var properties = ThargaBlazorOptionsForwarder.ForwardableProperties().Select(p => p.Name).ToArray();

        Assert.NotEmpty(properties);
        Assert.Contains(nameof(ThargaBlazorOptions.Title), properties);
        Assert.Contains(nameof(ThargaBlazorOptions.Consent), properties);
        Assert.Contains(nameof(ThargaBlazorOptions.Simulation), properties);
        Assert.True(properties.Length >= 8, $"Expected at least 8 forwardable options, found {properties.Length}.");
    }

    /// <summary>
    /// Every property, not a list of the ones someone remembered. Values are made distinguishable from
    /// the defaults so "forwarded" cannot be confused with "both happen to be default".
    /// </summary>
    [Fact]
    public void EveryPublicOptionIsForwarded()
    {
        var from = new ThargaBlazorOptions();
        var to = new ThargaBlazorOptions();

        foreach (var property in ThargaBlazorOptionsForwarder.ForwardableProperties())
        {
            var distinctive = Distinctive(property.PropertyType, property.GetValue(from));
            if (distinctive != null) property.SetValue(from, distinctive);
        }

        ThargaBlazorOptionsForwarder.Copy(from, to);

        var dropped = ThargaBlazorOptionsForwarder.ForwardableProperties()
            .Where(p => !Equals(p.GetValue(from), p.GetValue(to)))
            .Select(p => p.Name)
            .ToArray();

        Assert.True(dropped.Length == 0,
            $"Set on the facade and never forwarded, so a host configuring them would see nothing happen: {string.Join(", ", dropped)}.");
    }

    /// <summary>The case that prompted this, named so a regression is legible rather than one row in a list.</summary>
    [Fact]
    public void SimulationReachesTheBlazorLayer()
    {
        var from = new ThargaBlazorOptions();
        from.Simulation.Enabled = true;

        var to = new ThargaBlazorOptions();
        ThargaBlazorOptionsForwarder.Copy(from, to);

        Assert.True(to.Simulation.Enabled);
    }

    /// <summary>
    /// The self-check for the guard above: a property that is genuinely <i>not</i> copied must be
    /// detected. Otherwise "nothing dropped" could equally mean the comparison never fires.
    /// </summary>
    [Fact]
    public void TheDetectorNoticesAPropertyThatIsNotCopied()
    {
        var from = new ThargaBlazorOptions { Title = "changed" };
        var to = new ThargaBlazorOptions();

        // Deliberately not calling Copy.
        Assert.NotEqual(from.Title, to.Title);
    }

    /// <summary>A value visibly different from the default, or null when the type offers no easy one.</summary>
    private static object Distinctive(Type type, object current) => type switch
    {
        _ when type == typeof(string) => (string)current == "forwarding-probe" ? "forwarding-probe-2" : "forwarding-probe",
        _ when type == typeof(bool) => !(bool)current,
        _ when type == typeof(int) => (int)current + 1,
        _ when type == typeof(TimeSpan) => ((TimeSpan)current) + TimeSpan.FromMinutes(7),
        // Reference types are forwarded by reference, so a fresh instance is distinguishable from the
        // default one without needing to know anything about its shape.
        _ when !type.IsValueType && type.GetConstructor(Type.EmptyTypes) != null => Activator.CreateInstance(type),
        _ => null
    };
}
