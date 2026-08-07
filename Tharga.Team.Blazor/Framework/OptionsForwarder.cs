using System.Reflection;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Copies every public settable property from one options object onto another.
/// </summary>
/// <remarks>
/// <b>Copy by default; name the exceptions.</b> A hand-written list of assignments is how options quietly stop
/// working: the list is written once against the properties that exist that day, a property is added later,
/// and it is accepted from the caller and silently discarded — the host configures a feature, nothing fails,
/// and the feature never turns on. That has now happened twice on the same path, for
/// <c>ThargaBlazorOptions</c> and for <c>IconOptions.MaxUploadBytes</c> / <c>MaxDimension</c>
/// (Tharga/Team#177).
/// <para>
/// Reflection is the cheap part; the durable part is that a property added tomorrow is forwarded without
/// anyone remembering, and deliberately <i>not</i> forwarding one becomes a decision written down at the call
/// site rather than an unexplained omission in a copy loop.
/// </para>
/// </remarks>
internal static class OptionsForwarder
{
    /// <summary>
    /// Copies <paramref name="from"/> onto <paramref name="to"/>, skipping any property named in
    /// <paramref name="notForwarded"/>.
    /// </summary>
    public static void Copy<T>(T from, T to, params string[] notForwarded) where T : class
    {
        if (from == null || to == null) return;

        foreach (var property in ForwardableProperties<T>(notForwarded))
        {
            property.SetValue(to, property.GetValue(from));
        }
    }

    /// <summary>The properties <see cref="Copy"/> is responsible for. Public so a test can enumerate them.</summary>
    public static IEnumerable<PropertyInfo> ForwardableProperties<T>(params string[] notForwarded)
        => typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.SetMethod?.IsPublic == true)
            .Where(p => !notForwarded.Contains(p.Name));
}
