using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Reads the audit log over HTTP, authorized identically to the Blazor view and the MCP surface.
/// </summary>
/// <remarks>
/// Read-only by design: nothing about exposing the audit log justifies an endpoint that changes it.
/// </remarks>
[ApiController]
[Route("api/audit")]
[Authorize(Policy = ApiKeyConstants.ThargaApiPolicyName)]
public class AuditController(IAuditReadService auditReadService, IAuditOversightService auditOversightService) : ControllerBase
{
    /// <summary>Audit entries, newest first.</summary>
    /// <remarks>
    /// <b>There is no team parameter.</b> Which team the call is about comes from the credential: a team
    /// API key is bound to one team and can be nothing else, and a system API key names a team in the
    /// <c>X-Team-Key</c> header when it wants to act on behalf of one. A parameter beside a team-bound
    /// credential would be a second source of truth for one question — they can disagree, and an API
    /// shaped to allow that is wrong even though the disagreement is refused.
    /// <para>
    /// A system key with no header reads <b>system audit</b>: every team, narrowed by the filters below —
    /// including <paramref name="team"/>, which narrows data the caller is already authorized for rather
    /// than deciding what to authorize against.
    /// </para>
    /// </remarks>
    /// <param name="team">
    /// Narrows a system-audit read to one team. A <i>filter</i>, not an authorization input — it is
    /// refused if the caller is already bound to a different team, because that is the same contradiction
    /// the header check refuses.
    /// </param>
    /// <param name="from">Earliest timestamp, inclusive.</param>
    /// <param name="to">Latest timestamp, inclusive.</param>
    /// <param name="feature">Restrict to one feature — the left half of a scope, e.g. <c>apikey</c>.</param>
    /// <param name="action">Restrict to one action — the right half, e.g. <c>manage</c>.</param>
    /// <param name="success">Restrict to successful or failed entries.</param>
    /// <param name="skip">Entries to skip, for paging.</param>
    /// <param name="take">Maximum entries to return. Capped at 500.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditQueryResult>> GetAsync(
        [FromQuery] string team = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string feature = null,
        [FromQuery] string action = null,
        [FromQuery] bool? success = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        // The team the caller is acting on: its own if it is a team key, or the one named in the header
        // and resolved by TeamContextMiddleware, which put it here as a claim. Never a parameter.
        var contextTeamKey = User.FindFirst(TeamClaimTypes.TeamKey)?.Value;

        // A filter contradicting the caller's own team is refused rather than ignored, for the same
        // reason the header check refuses one: the request has said two incompatible things.
        if (!string.IsNullOrEmpty(contextTeamKey) && !string.IsNullOrEmpty(team) &&
            !string.Equals(contextTeamKey, team, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        // No authorization here. Both services carry [RequireScope] and are enforced by ScopeProxy --
        // the team-bound one against the team it is given, the oversight one against the system grant.
        // A check in this method would be a second copy of a rule the service already owns, and a second
        // copy is how the three surfaces came to disagree in the first place.
        var query = new AuditQuery
        {
            From = from,
            To = to,
            Feature = feature,
            Action = action,
            Success = success,
            Skip = skip < 0 ? 0 : skip,
            Take = Math.Clamp(take, 1, MaxTake),
        };

        try
        {
            // The credential decides which service applies, so there is nothing to guess. This replaced a
            // try-one-then-fall-back-to-the-other branch that existed only because the endpoint could not
            // tell a team caller from a system one -- it took a parameter instead of reading who called.
            return string.IsNullOrEmpty(contextTeamKey)
                ? Ok(await auditOversightService.QueryAllAsync(query with { TeamKey = team }))
                : Ok(await auditReadService.QueryAsync(contextTeamKey, query));
        }
        catch (UnauthorizedAccessException)
        {
            // Forbid rather than 404: the caller is authenticated and the resource exists, they simply
            // may not read it. A 404 would also leak whether a team key is real, by answering differently
            // for one that is not.
            return Forbid();
        }
    }

    /// <summary>Ceiling on <c>take</c>, so one request cannot pull the whole collection.</summary>
    private const int MaxTake = 500;
}
