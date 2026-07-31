using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Builds an audit entry with the caller already resolved — an HTTP principal when there is one, the
/// declared <see cref="AuditActor"/> when there is not.
/// </summary>
/// <remarks>
/// Without this, a consumer writing its own entry constructs <see cref="AuditEntry"/> by hand and passes
/// it to <see cref="IAuditLogger.Log"/>, which never consults the ambient actor — so background work
/// could declare an actor and still write entries attributed to nobody. Build entries here and hand the
/// result to the logger.
/// </remarks>
public interface IAuditEntryFactory
{
    /// <summary>Builds an entry for a consumer-defined operation, with the caller filled in.</summary>
    /// <param name="feature">The area acted on — the left half of a scope, e.g. <c>"job"</c>.</param>
    /// <param name="action">What was done — the right half, e.g. <c>"claim"</c>.</param>
    /// <param name="methodName">Optional method or step name, for the log's Method column.</param>
    /// <param name="durationMs">How long the operation took, if measured.</param>
    /// <param name="success">Whether it succeeded. False routes it to the failure styling in the log view.</param>
    /// <param name="errorMessage">Why it failed, shown in the failure tooltip and the exports.</param>
    /// <param name="teamKey">The team acted on. Supply it for background work — there is no selected team to infer.</param>
    /// <param name="metadata">What changed, surfaced in the log's detail row.</param>
    AuditEntry Create(
        string feature,
        string action,
        string methodName = null,
        long durationMs = 0,
        bool success = true,
        string errorMessage = null,
        string teamKey = null,
        IReadOnlyDictionary<string, string> metadata = null);
}

/// <inheritdoc />
public sealed class AuditEntryFactory(IHttpContextAccessor httpContextAccessor) : IAuditEntryFactory
{
    public AuditEntry Create(
        string feature,
        string action,
        string methodName = null,
        long durationMs = 0,
        bool success = true,
        string errorMessage = null,
        string teamKey = null,
        IReadOnlyDictionary<string, string> metadata = null)
        => AuditHelper.BuildEntry(httpContextAccessor, feature, action, methodName, durationMs, success, errorMessage, teamKey, metadata);
}
