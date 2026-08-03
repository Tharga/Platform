using System.Reflection;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Copies the Blazor options from the <c>AddThargaTeam</c> facade into the layer that consumes them.
/// </summary>
/// <remarks>
/// <b>Everything, by reflection, rather than a hand-written list.</b> The list is how the bug happened:
/// <c>AddThargaTeam</c> assigned nine named properties, so a tenth added later was accepted from the
/// caller and silently discarded — the host configured a feature, nothing failed, and the feature simply
/// never turned on. That is the same failure Eplicta reported for <c>IconOptions.MaxUploadBytes</c>
/// (Tharga/Team#177), on the same path, and it will keep recurring for as long as forwarding is a list
/// somebody has to remember to extend.
/// <para>
/// Copying by default and naming the exceptions inverts that: a new option works without anyone
/// remembering, and deliberately <i>not</i> forwarding one is a decision written down here.
/// </para>
/// <para>
/// Public settable properties only. The internal registration fields (<c>_teamService</c> and the rest)
/// are not properties and are still assigned explicitly by the caller, as are the icon settings, which
/// come from the facade's own options rather than from its <c>Blazor</c> section.
/// </para>
/// </remarks>
internal static class ThargaBlazorOptionsForwarder
{
    /// <summary>
    /// Properties deliberately not copied, with the reason. Empty today — kept so an exception has a
    /// place to be recorded rather than becoming an unexplained omission in a copy loop.
    /// </summary>
    private static readonly string[] NotForwarded = [];

    /// <summary>Copies every public settable property from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static void Copy(ThargaBlazorOptions from, ThargaBlazorOptions to)
    {
        if (from == null || to == null) return;

        foreach (var property in ForwardableProperties())
        {
            property.SetValue(to, property.GetValue(from));
        }
    }

    /// <summary>The properties <see cref="Copy"/> is responsible for. Public so a test can enumerate them.</summary>
    public static IEnumerable<PropertyInfo> ForwardableProperties()
        => typeof(ThargaBlazorOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.SetMethod?.IsPublic == true)
            .Where(p => !NotForwarded.Contains(p.Name));
}
