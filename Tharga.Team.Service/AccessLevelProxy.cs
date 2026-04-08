using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service;

/// <summary>
/// DispatchProxy that intercepts service method calls and enforces
/// <see cref="RequireAccessLevelAttribute"/> by reading TeamKey and AccessLevel
/// claims from HttpContext. Methods without the attribute are blocked (fail-closed).
/// Logs audit entries when IAuditLogger is available.
/// </summary>
public class AccessLevelProxy<T> : DispatchProxy where T : class
{
    private T _target;
    private IHttpContextAccessor _httpContextAccessor;
    private IAuditLogger _auditLogger;

    public static T Create(T target, IHttpContextAccessor httpContextAccessor, IAuditLogger auditLogger = null)
    {
        var proxy = Create<T, AccessLevelProxy<T>>() as AccessLevelProxy<T>;
        proxy._target = target;
        proxy._httpContextAccessor = httpContextAccessor;
        proxy._auditLogger = auditLogger;
        return proxy as T;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var attribute = GetAttribute(targetMethod);
        if (attribute == null)
            throw new InvalidOperationException(
                $"Method '{typeof(T).Name}.{targetMethod.Name}' is missing the [RequireAccessLevel] attribute. " +
                $"All methods on services registered with AddScopedWithAccessLevel must declare their required access level.");

        var sw = Stopwatch.StartNew();

        try
        {
            CheckAccessLevel(attribute.MinimumLevel);

            var result = targetMethod.Invoke(_target, args);
            sw.Stop();

            LogAudit(attribute.MinimumLevel, targetMethod.Name, sw.ElapsedMilliseconds, true);

            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            sw.Stop();
            LogAudit(attribute.MinimumLevel, targetMethod.Name, sw.ElapsedMilliseconds, false,
                AuditEventType.AccessLevelDenial, ex.Message);
            throw;
        }
        catch (TargetInvocationException tie)
        {
            sw.Stop();
            LogAudit(attribute.MinimumLevel, targetMethod.Name, sw.ElapsedMilliseconds, false,
                errorMessage: tie.InnerException?.Message);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogAudit(attribute.MinimumLevel, targetMethod.Name, sw.ElapsedMilliseconds, false,
                errorMessage: ex.Message);
            throw;
        }
    }

    private void LogAudit(AccessLevel minimumLevel, string methodName, long durationMs, bool success,
        AuditEventType? eventType = null, string errorMessage = null)
    {
        if (_auditLogger == null) return;

        var user = _httpContextAccessor.HttpContext?.User;
        var identity = user?.Identity;

        var callerSource = identity?.AuthenticationType switch
        {
            ApiKeyConstants.SchemeName => AuditCallerSource.Api,
            "Cookies" or "AuthenticationTypes.Federation" => AuditCallerSource.Web,
            _ => AuditCallerSource.Unknown
        };

        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType ?? AuditEventType.ServiceCall,
            Feature = typeof(T).Name,
            Action = methodName,
            MethodName = methodName,
            DurationMs = durationMs,
            Success = success,
            ErrorMessage = errorMessage,
            CallerType = callerSource == AuditCallerSource.Api ? AuditCallerType.ApiKey : AuditCallerType.User,
            CorrelationId = Guid.TryParse(_httpContextAccessor.HttpContext?.TraceIdentifier, out var traceId) ? traceId : Guid.NewGuid(),
            CallerIdentity = user?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? user?.FindFirst("preferred_username")?.Value
                ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("name")?.Value,
            TeamKey = user?.FindFirst(TeamClaimTypes.TeamKey)?.Value,
            AccessLevel = user?.FindFirst(TeamClaimTypes.AccessLevel)?.Value,
            CallerSource = callerSource,
            ScopeChecked = $"AccessLevel>={minimumLevel}",
            ScopeResult = success ? AuditScopeResult.Allowed : AuditScopeResult.Denied,
        };

        _auditLogger.Log(entry);
    }

    private RequireAccessLevelAttribute GetAttribute(MethodInfo methodInfo)
    {
        var interfaceMethod = typeof(T).GetMethod(
            methodInfo.Name,
            methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
        return interfaceMethod?.GetCustomAttribute<RequireAccessLevelAttribute>()
               ?? methodInfo.GetCustomAttribute<RequireAccessLevelAttribute>();
    }

    private void CheckAccessLevel(AccessLevel minimumLevel)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var teamKey = user?.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        if (string.IsNullOrEmpty(teamKey))
            throw new UnauthorizedAccessException("No team selected.");

        var accessLevelValue = user?.FindFirst(TeamClaimTypes.AccessLevel)?.Value;
        if (accessLevelValue == null || !Enum.TryParse<AccessLevel>(accessLevelValue, out var accessLevel))
            throw new UnauthorizedAccessException("Access level claim not found.");

        if (accessLevel > minimumLevel)
            throw new UnauthorizedAccessException(
                $"Access level '{accessLevel}' is insufficient. Minimum required: {minimumLevel}.");
    }
}
