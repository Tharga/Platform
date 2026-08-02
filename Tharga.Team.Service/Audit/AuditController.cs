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
    /// <param name="teamKey">
    /// Restrict to one team. Omit to query across every team, which requires a <b>system</b>
    /// <c>audit:read</c> grant — a team grant authorizes only its own team.
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
        [FromQuery] string teamKey = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string feature = null,
        [FromQuery] string action = null,
        [FromQuery] bool? success = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        // No authorization here. Both services carry [RequireScope] and are enforced by ScopeProxy --
        // the team-bound one against the team named below, the oversight one against the system grant.
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
            // Two ways to be allowed to read one team: a grant on that team, or a system grant that
            // covers every team. ScopeProxy's team check does not accept a system grant -- that
            // provenance split is deliberate and must not be loosened globally -- so the two are asked
            // separately, and both services do their own deciding. The controller only chooses which
            // doors to try; it never decides whether one opens.
            if (!string.IsNullOrEmpty(teamKey))
            {
                try
                {
                    return Ok(await auditReadService.QueryAsync(teamKey, query));
                }
                catch (UnauthorizedAccessException)
                {
                    // No grant on that team. A system grant may still cover it, narrowed by the filter.
                }
            }

            return Ok(await auditOversightService.QueryAllAsync(query with { TeamKey = teamKey }));
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
