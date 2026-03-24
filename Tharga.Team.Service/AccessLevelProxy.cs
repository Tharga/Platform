using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Service;

/// <summary>
/// DispatchProxy that intercepts service method calls and enforces
/// <see cref="RequireAccessLevelAttribute"/> by reading TeamKey and AccessLevel
/// claims from HttpContext. Methods without the attribute are blocked (fail-closed).
/// </summary>
public class AccessLevelProxy<T> : DispatchProxy where T : class
{
    private T _target;
    private IHttpContextAccessor _httpContextAccessor;

    public static T Create(T target, IHttpContextAccessor httpContextAccessor)
    {
        var proxy = Create<T, AccessLevelProxy<T>>() as AccessLevelProxy<T>;
        proxy._target = target;
        proxy._httpContextAccessor = httpContextAccessor;
        return proxy as T;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var attribute = GetAttribute(targetMethod);
        if (attribute == null)
            throw new InvalidOperationException(
                $"Method '{typeof(T).Name}.{targetMethod.Name}' is missing the [RequireAccessLevel] attribute. " +
                $"All methods on services registered with AddScopedWithAccessLevel must declare their required access level.");

        CheckAccessLevel(attribute.MinimumLevel);

        return targetMethod.Invoke(_target, args);
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
