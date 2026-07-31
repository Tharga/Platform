namespace Tharga.Team.Service.Audit;

/// <summary>
/// Declares the actor for audited work that has no authenticated HTTP caller.
/// </summary>
public interface IAuditContextAccessor
{
    /// <summary>The actor in effect for the current async flow, or null.</summary>
    AuditActor Current { get; }

    /// <summary>
    /// Makes <paramref name="actor"/> the actor for the current async flow until the returned scope is
    /// disposed, then restores whatever was in effect before.
    /// </summary>
    IDisposable Push(AuditActor actor);
}

/// <summary>
/// <see cref="AsyncLocal{T}"/>-backed implementation. Registered as a singleton — the flow, not the
/// instance, carries the value.
/// </summary>
/// <remarks>
/// An audited call reaches <c>AuditHelper.BuildEntry</c> through several layers (the enforcement proxies,
/// each auditing decorator), and threading an actor through every signature would put a parameter nobody
/// on the HTTP path needs into all of them. Ambient context is what <see cref="AsyncLocal{T}"/> exists
/// for, and it flows across <c>await</c> without cooperation from the code in between.
/// <para>
/// The value is static because the flow is the scope. A leaked or wrongly-restored scope would attribute
/// one job's actions to another — the same class of false attribution this feature removes, so
/// <see cref="Push"/> always restores the previous value rather than clearing it.
/// </para>
/// </remarks>
public sealed class AuditContextAccessor : IAuditContextAccessor
{
    private static readonly AsyncLocal<AuditActor> _current = new();

    /// <summary>The ambient actor, for the entry builder — which is static and has no accessor injected.</summary>
    internal static AuditActor Ambient => _current.Value;

    public AuditActor Current => _current.Value;

    public IDisposable Push(AuditActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var previous = _current.Value;
        _current.Value = actor;
        return new Scope(previous);
    }

    private sealed class Scope(AuditActor previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            // Restore rather than clear, so a nested scope hands back to its parent instead of dropping
            // the outer job's actor. Guarded because a double dispose would otherwise restore a stale
            // value over whatever is legitimately in effect by then.
            if (_disposed) return;
            _disposed = true;
            _current.Value = previous;
        }
    }
}
