using Microsoft.AspNetCore.Builder;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Options type under its former name. Identical to <see cref="ThargaTeamOptions"/> in every respect —
/// it derives from it and adds nothing.
/// </summary>
/// <remarks>
/// The "Platform" concept was removed in 3.6; the product is Team. This exists so code written against
/// the old name keeps compiling. Removed in 4.0.
/// </remarks>
[Obsolete($"Renamed to {nameof(ThargaTeamOptions)}. This alias is removed in 4.0.")]
public class ThargaPlatformOptions : ThargaTeamOptions;

/// <summary>
/// Registration entry points under their former names, forwarding to <see cref="ThargaTeamRegistration"/>.
/// </summary>
/// <remarks>
/// Every member here delegates to the Team-named equivalent rather than reimplementing it, so the two
/// entry points cannot drift. Removed in 4.0.
/// </remarks>
[Obsolete($"Renamed to {nameof(ThargaTeamRegistration)}. This alias is removed in 4.0.")]
public static class ThargaPlatformRegistration
{
    /// <inheritdoc cref="ThargaTeamRegistration.AddThargaTeam"/>
    [Obsolete($"Renamed to {nameof(ThargaTeamRegistration.AddThargaTeam)}. This alias is removed in 4.0.")]
    public static void AddThargaPlatform(this WebApplicationBuilder builder, Action<ThargaPlatformOptions> configure = null)
    {
        var options = new ThargaPlatformOptions();
        configure?.Invoke(options);
        ThargaTeamRegistration.AddThargaTeamCore(builder, options);
    }

    /// <inheritdoc cref="ThargaTeamRegistration.UseThargaTeam"/>
    [Obsolete($"Renamed to {nameof(ThargaTeamRegistration.UseThargaTeam)}. This alias is removed in 4.0.")]
    public static void UseThargaPlatform(this WebApplication app) => app.UseThargaTeam();
}
