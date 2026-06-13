using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// DispatchProxy that intercepts service method calls and enforces
/// <see cref="RequireScopeAttribute"/> by checking scope claims on the current principal (resolved via
/// <see cref="ITeamPrincipalAccessor"/>, so it works for both HTTP and interactive Blazor callers).
/// Methods without the attribute throw InvalidOperationException (fail-closed). Also verifies a TeamKey
/// claim is present. Logs audit entries when IAuditLogger is available.
/// </summary>
public class ScopeProxy<T> : DispatchProxy where T : class
{
    private T _target;
    private ITeamPrincipalAccessor _principalAccessor;
    private IAuditLogger _auditLogger;

    public static T Create(T target, ITeamPrincipalAccessor principalAccessor, IAuditLogger auditLogger = null)
    {
        var proxy = Create<T, ScopeProxy<T>>() as ScopeProxy<T>;
        proxy._target = target;
        proxy._principalAccessor = principalAccessor;
        proxy._auditLogger = auditLogger;
        return proxy as T;
    }

    /// <summary>Back-compat overload — adapts an <see cref="IHttpContextAccessor"/> to the default accessor.</summary>
    public static T Create(T target, IHttpContextAccessor httpContextAccessor, IAuditLogger auditLogger = null)
        => Create(target, new HttpContextTeamPrincipalAccessor(httpContextAccessor), auditLogger);

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var attribute = GetAttribute(targetMethod);
        if (attribute == null)
            throw new InvalidOperationException(
                $"Method '{typeof(T).Name}.{targetMethod.Name}' is missing the [RequireScope] attribute. " +
                $"All methods on services registered with AddScopedWithScopes must declare their required scope.");

        var (feature, action) = AuditEntry.ParseScope(attribute.Scope);

        return ProxyInvoker.Invoke(targetMethod, args, _target, _principalAccessor,
            enforce: principal => CheckScope(principal, attribute.Scope),
            audit: (principal, ms, success, ex) =>
            {
                var scopeResult = !success && ex is UnauthorizedAccessException uae && uae.Message.Contains("Missing required scope")
                    ? AuditScopeResult.Denied
                    : AuditScopeResult.Allowed;
                LogAudit(principal, attribute.Scope, feature, action, targetMethod.Name, ms, success, scopeResult, success ? null : ex?.Message);
            });
    }

    private void LogAudit(ClaimsPrincipal user, string scope, string feature, string action, string methodName, long durationMs, bool success, AuditScopeResult scopeResult, string errorMessage = null)
    {
        if (_auditLogger == null) return;

        var callerSource = user?.Identity?.AuthenticationType switch
        {
            ApiKeyConstants.SchemeName => AuditCallerSource.Api,
            "Cookies" or "AuthenticationTypes.Federation" => AuditCallerSource.Web,
            _ => AuditCallerSource.Unknown
        };

        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = scopeResult == AuditScopeResult.Denied ? AuditEventType.ScopeDenial : AuditEventType.ServiceCall,
            Feature = feature,
            Action = action,
            MethodName = methodName,
            DurationMs = durationMs,
            Success = success,
            ErrorMessage = errorMessage,
            CallerType = callerSource == AuditCallerSource.Api ? AuditCallerType.ApiKey : AuditCallerType.User,
            CorrelationId = Guid.NewGuid(),
            CallerIdentity = user?.FindFirst(ClaimTypes.Name)?.Value
                ?? user?.FindFirst("preferred_username")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("name")?.Value,
            TeamKey = user?.FindFirst(TeamClaimTypes.TeamKey)?.Value,
            AccessLevel = user?.FindFirst(TeamClaimTypes.AccessLevel)?.Value,
            CallerSource = callerSource,
            ScopeChecked = scope,
            ScopeResult = scopeResult,
        };

        _auditLogger.Log(entry);
    }

    private RequireScopeAttribute GetAttribute(MethodInfo methodInfo)
    {
        var interfaceMethod = typeof(T).GetMethod(
            methodInfo.Name,
            methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
        return interfaceMethod?.GetCustomAttribute<RequireScopeAttribute>()
               ?? methodInfo.GetCustomAttribute<RequireScopeAttribute>();
    }

    private static void CheckScope(ClaimsPrincipal user, string requiredScope)
    {
        var teamKey = user?.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        if (string.IsNullOrEmpty(teamKey))
            throw new UnauthorizedAccessException("No team selected.");

        var hasScope = user?.Claims
            .Where(c => c.Type == TeamClaimTypes.Scope)
            .Any(c => c.Value == requiredScope) ?? false;

        if (!hasScope)
            throw new UnauthorizedAccessException(
                $"Missing required scope '{requiredScope}'.");
    }
}
