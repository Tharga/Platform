using Microsoft.Extensions.DependencyInjection;

namespace Tharga.Team;

/// <summary>
/// Vestigial registration entry point that never registered anything.
/// </summary>
/// <remarks>
/// Superseded by <c>WebApplicationBuilder.AddThargaTeam</c> in <c>Tharga.Team.Blazor</c>, which is the real
/// entry point. This one has an empty body, so any caller was already getting nothing — the danger is that
/// it resolves silently and looks like it worked. Removed in 4.0.
/// </remarks>
[Obsolete("This never registered anything. Use builder.AddThargaTeam() from Tharga.Team.Blazor instead. Removed in 4.0.")]
public static class ThargaTeamRegistration
{
    /// <summary>No-op. Use <c>builder.AddThargaTeam()</c> from <c>Tharga.Team.Blazor</c>.</summary>
    [Obsolete("This never registered anything. Use builder.AddThargaTeam() from Tharga.Team.Blazor instead. Removed in 4.0.")]
    public static void AddThargaTeam(this IServiceCollection services)
    {
    }
}
