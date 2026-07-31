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
public class AuditController(CompositeAuditLogger auditLogger) : ControllerBase
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
        // Forbid rather than 404: the caller is authenticated and the resource exists, they simply may not
        // read it. A 404 here would also leak whether a team key is real, by answering differently for one
        // that is not.
        if (!AuditAccess.CanRead(User, teamKey))
            return Forbid();

        var result = await auditLogger.QueryAsync(new AuditQuery
        {
            // Bound by the same value the authorization check used, so a caller authorized for one team
            // cannot widen the query past it.
            TeamKey = teamKey,
            From = from,
            To = to,
            Feature = feature,
            Action = action,
            Success = success,
            Skip = skip < 0 ? 0 : skip,
            Take = Math.Clamp(take, 1, MaxTake),
        });

        return Ok(result);
    }

    /// <summary>Ceiling on <c>take</c>, so one request cannot pull the whole collection.</summary>
    private const int MaxTake = 500;
}
