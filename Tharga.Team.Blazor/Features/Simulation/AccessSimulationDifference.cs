using System.Security.Claims;

namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// What a simulation will not be able to show.
/// </summary>
/// <param name="Scopes">
/// Scopes the target holds and the caller does not. Read through <see cref="UnreachableScopes"/>.
/// </param>
/// <param name="SystemAccessNotReproduced">
/// Whether the target's system-wide access is unknown rather than empty.
/// </param>
/// <remarks>
/// <b><c>default</c> is a valid, meaningful value: "nothing compared yet, nothing to report".</b> A
/// struct's default state is always reachable — a field declared and not assigned is one — and the first
/// version dereferenced the list from it, so opening the picker and switching target kind threw a
/// <see cref="NullReferenceException"/> straight into the error boundary. Members are null-safe rather
/// than relying on every caller to avoid the default.
/// </remarks>
public readonly record struct AccessSimulationGap(
    IReadOnlyList<string> Scopes,
    bool SystemAccessNotReproduced)
{
    /// <summary>
    /// Scopes the target holds and the caller does not, so the simulation shows <i>less</i> than the
    /// target really sees. Never null.
    /// </summary>
    public IReadOnlyList<string> UnreachableScopes => Scopes ?? [];

    /// <summary>Whether there is anything the caller needs to be told.</summary>
    public bool IsFaithful => UnreachableScopes.Count == 0 && !SystemAccessNotReproduced;
}

/// <summary>
/// Computes what a simulation cannot reproduce, so the caller is told before they draw a conclusion
/// from it.
/// </summary>
/// <remarks>
/// <b>This is not a nicety.</b> The feature exists so an administrator can set a user's access
/// correctly. If the caller lacks a scope the target holds, the simulation shows the intersection —
/// <i>less</i> than the target really sees — and an administrator who is not told concludes "they cannot
/// do X" about something they can. That error points towards **granting more access than necessary**,
/// which is precisely the outcome the feature is meant to prevent.
/// <para>
/// Restricting simulation to team Owner/Administrator makes the scope half of this rare rather than
/// common: <c>ScopeRegistry.GetScopesForAccessLevel</c> returns every registered scope at
/// <c>Administrator</c> and above, so an administrator holds all of them. It does not make it
/// impossible — <c>GetEffectiveScopes</c> unions in a member's <c>ScopeOverrides</c> without validating
/// them against the registry, so a member can carry a scope that was never registered and that no
/// access level therefore grants.
/// </para>
/// <para>
/// Rarity is an argument for showing it prominently, not for leaving it out. A warning that fires
/// constantly gets ignored; one that fires almost never is trusted the first time it appears — and is
/// most dangerous when absent.
/// </para>
/// </remarks>
internal static class AccessSimulationDifference
{
    /// <summary>
    /// Compares a target's access against what the caller actually holds.
    /// </summary>
    /// <param name="principal">The caller, before any simulation is applied.</param>
    /// <param name="simulation">The simulation about to be applied.</param>
    public static AccessSimulationGap Compare(ClaimsPrincipal principal, AccessSimulation simulation)
    {
        if (simulation == null) return new AccessSimulationGap([], false);

        var held = new HashSet<string>(
            principal?.FindAll(TeamClaimTypes.Scope).Select(c => c.Value) ?? [],
            StringComparer.OrdinalIgnoreCase);

        var unreachable = (simulation.Scopes ?? [])
            .Where(scope => !held.Contains(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();

        // Only a user has system access we cannot see. A role, a scope list and an access level are all
        // team-scoped by definition, so there is nothing unknown about them.
        var systemUnknown = simulation.Kind == AccessSimulationKind.User;

        return new AccessSimulationGap(unreachable, systemUnknown);
    }
}
