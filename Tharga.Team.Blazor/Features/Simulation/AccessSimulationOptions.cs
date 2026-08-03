namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// Whether an administrator may view the application as a less privileged user.
/// </summary>
public class AccessSimulationOptions
{
    /// <summary>
    /// Turns the feature on. Off by default.
    /// </summary>
    /// <remarks>
    /// Opt-in because it adds a visible control and a session cookie to every page for the callers who
    /// hold <see cref="SimulationScopes.Simulate"/>, and a host that does not want it should not have to
    /// hide anything. Nothing changes for a host that leaves this alone: the cookie is never read, the
    /// filter is never reached, and the scope is still registered but grants nothing anyone can use.
    /// </remarks>
    public bool Enabled { get; set; }
}
