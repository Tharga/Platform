using System.Reflection;
using System.Security.Claims;

namespace Tharga.Team.Service;

/// <summary>
/// Shared invocation pipeline for the enforcement proxies (<see cref="ScopeProxy{T}"/>,
/// <see cref="AccessLevelProxy{T}"/>): resolve the caller (possibly asynchronously, e.g. from a Blazor
/// circuit), run <paramref name="enforce"/>, invoke the target, then <paramref name="audit"/> the outcome.
/// Handles sync, <see cref="Task"/> and <see cref="Task{TResult}"/> methods so the async principal source
/// is awaited rather than blocked for the common (async) service methods.
/// </summary>
internal static class ProxyInvoker
{
    /// <param name="enforce">Throws <see cref="UnauthorizedAccessException"/> if the principal is not allowed.</param>
    /// <param name="audit">Invoked with (principal, durationMs, success, exception-or-null) after the call.</param>
    public static object Invoke(
        MethodInfo method, object[] args, object target,
        ITeamPrincipalAccessor accessor,
        Action<ClaimsPrincipal> enforce,
        Action<ClaimsPrincipal, long, bool, Exception> audit)
    {
        var returnType = method.ReturnType;

        if (returnType == typeof(Task))
            return InvokeAsync(method, args, target, accessor, enforce, audit);

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var helper = typeof(ProxyInvoker)
                .GetMethod(nameof(InvokeAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(returnType.GetGenericArguments()[0]);
            return helper.Invoke(null, [method, args, target, accessor, enforce, audit]);
        }

        return InvokeSync(method, args, target, accessor, enforce, audit);
    }

    private static object InvokeSync(
        MethodInfo method, object[] args, object target,
        ITeamPrincipalAccessor accessor,
        Action<ClaimsPrincipal> enforce,
        Action<ClaimsPrincipal, long, bool, Exception> audit)
    {
        var principal = accessor.GetCurrentAsync().GetAwaiter().GetResult();
        var startedAt = StartTimestamp();
        try
        {
            enforce(principal);
            var result = Unwrap(() => method.Invoke(target, args));
            audit(principal, ElapsedMs(startedAt), true, null);
            return result;
        }
        catch (Exception ex)
        {
            audit(principal, ElapsedMs(startedAt), false, ex);
            throw;
        }
    }

    private static async Task InvokeAsync(
        MethodInfo method, object[] args, object target,
        ITeamPrincipalAccessor accessor,
        Action<ClaimsPrincipal> enforce,
        Action<ClaimsPrincipal, long, bool, Exception> audit)
    {
        var principal = await accessor.GetCurrentAsync();
        var startedAt = StartTimestamp();
        try
        {
            enforce(principal);
            await (Task)Unwrap(() => method.Invoke(target, args));
            audit(principal, ElapsedMs(startedAt), true, null);
        }
        catch (Exception ex)
        {
            audit(principal, ElapsedMs(startedAt), false, ex);
            throw;
        }
    }

    private static async Task<TResult> InvokeAsyncGeneric<TResult>(
        MethodInfo method, object[] args, object target,
        ITeamPrincipalAccessor accessor,
        Action<ClaimsPrincipal> enforce,
        Action<ClaimsPrincipal, long, bool, Exception> audit)
    {
        var principal = await accessor.GetCurrentAsync();
        var startedAt = StartTimestamp();
        try
        {
            enforce(principal);
            var result = await (Task<TResult>)Unwrap(() => method.Invoke(target, args));
            audit(principal, ElapsedMs(startedAt), true, null);
            return result;
        }
        catch (Exception ex)
        {
            audit(principal, ElapsedMs(startedAt), false, ex);
            throw;
        }
    }

    // Reflection wraps target exceptions in TargetInvocationException — surface the real one.
    private static object Unwrap(Func<object> invoke)
    {
        try { return invoke(); }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            throw; // unreachable
        }
    }

    private static long StartTimestamp() => System.Diagnostics.Stopwatch.GetTimestamp();
    private static long ElapsedMs(long startedAt) => (long)System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
}
