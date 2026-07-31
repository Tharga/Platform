using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Shared helper for building audit entries from HTTP context.
/// Used by both ScopeProxy and audit decorators.
/// </summary>
internal static class AuditHelper
{
    public static AuditEntry BuildEntry(
        IHttpContextAccessor httpContextAccessor,
        string feature,
        string action,
        string methodName,
        long durationMs,
        bool success,
        string errorMessage = null,
        string teamKey = null,
        IReadOnlyDictionary<string, string> metadata = null)
    {
        var user = httpContextAccessor?.HttpContext?.User;
        var identity = user?.Identity;

        var callerSource = identity?.AuthenticationType switch
        {
            ApiKeyConstants.SchemeName => AuditCallerSource.Api,
            "Cookies" or "AuthenticationTypes.Federation" => AuditCallerSource.Web,
            _ => AuditCallerSource.Unknown
        };

        // Only positive evidence names an actor. This used to fall through to User for anything that was
        // not an API key, so a caller with no HttpContext at all — a hosted service, a message handler —
        // was recorded as a person with a null identity (Tharga/Team#163). An authenticated principal
        // under an unrecognised scheme is still a person; the absence of one is not.
        var callerType = callerSource switch
        {
            AuditCallerSource.Api => AuditCallerType.ApiKey,
            AuditCallerSource.Web => AuditCallerType.User,
            _ => identity?.IsAuthenticated == true ? AuditCallerType.User : AuditCallerType.Unknown
        };

        return new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.ServiceCall,
            Feature = feature,
            Action = action,
            MethodName = methodName,
            DurationMs = durationMs,
            Success = success,
            ErrorMessage = errorMessage,
            CallerType = callerType,
            CorrelationId = Guid.TryParse(httpContextAccessor?.HttpContext?.TraceIdentifier, out var traceId) ? traceId : Guid.NewGuid(),
            CallerIdentity = user?.FindFirst(ClaimTypes.Name)?.Value
                ?? user?.FindFirst("preferred_username")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("name")?.Value,
            CallerKeyId = user?.FindFirst(TeamClaimTypes.ApiKeyId)?.Value,
            TeamKey = teamKey ?? user?.FindFirst(TeamClaimTypes.TeamKey)?.Value,
            AccessLevel = user?.FindFirst(TeamClaimTypes.AccessLevel)?.Value,
            CallerSource = callerSource,
            Metadata = metadata is { Count: > 0 } ? new Dictionary<string, string>(metadata) : null,
        };
    }
}
