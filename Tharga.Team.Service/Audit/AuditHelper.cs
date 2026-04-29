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
        string teamKey = null)
    {
        var user = httpContextAccessor?.HttpContext?.User;
        var identity = user?.Identity;

        var callerSource = identity?.AuthenticationType switch
        {
            ApiKeyConstants.SchemeName => AuditCallerSource.Api,
            "Cookies" or "AuthenticationTypes.Federation" => AuditCallerSource.Web,
            _ => AuditCallerSource.Unknown
        };

        var callerType = callerSource == AuditCallerSource.Api
            ? AuditCallerType.ApiKey
            : AuditCallerType.User;

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
        };
    }
}
