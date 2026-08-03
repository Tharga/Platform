namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// Scope constants for access simulation.
/// </summary>
public static class SimulationScopes
{
    /// <summary>
    /// Authorizes starting an access simulation — seeing the application as a less privileged user.
    /// </summary>
    /// <remarks>
    /// Registered at <see cref="AccessLevel.Administrator"/>, which yields "team Owner and Administrator"
    /// without naming them: <c>ScopeRegistry.GetScopesForAccessLevel</c> returns every registered scope at
    /// Administrator and above. Expressed as a scope rather than an access-level check so a host can widen
    /// it to a tenant role, or withhold it, without a toolkit change.
    /// <para>
    /// <b>Starting a simulation is gated; ending one is not.</b> A simulation can remove this very scope,
    /// so gating the exit would let a caller strand themselves. Returning to normal access only restores
    /// what the caller really holds, so there is nothing to authorize.
    /// </para>
    /// </remarks>
    public const string Simulate = "simulation:use";
}
