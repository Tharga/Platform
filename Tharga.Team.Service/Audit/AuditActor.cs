namespace Tharga.Team.Service.Audit;

/// <summary>
/// A declared actor for work that has no authenticated HTTP caller behind it — a hosted service, a
/// message handler, a scheduled job.
/// </summary>
/// <remarks>
/// Declared rather than inferred, on purpose. The application knows it is running a nightly retention
/// sweep; nothing about the call stack does. Push one of these around the unit of work and every audit
/// entry written inside it is attributed to that actor instead of falling through to
/// <see cref="AuditCallerType.Unknown"/>.
/// </remarks>
/// <param name="Identity">
/// Who to record — a service or job name (<c>"retention-sweep"</c>, <c>"fortdocs-worker"</c>). This lands
/// in <see cref="AuditEntry.CallerIdentity"/>, so make it something a reader can act on.
/// </param>
/// <param name="CallerType">Defaults to <see cref="AuditCallerType.System"/>.</param>
/// <param name="CallerSource">Defaults to <see cref="AuditCallerSource.Background"/>.</param>
/// <param name="CorrelationId">
/// Optional. Set it per unit of work — one value per claimed job — so every entry that job writes can be
/// pulled back together. Without it each entry gets its own generated id and the grouping is lost.
/// </param>
public sealed record AuditActor(
    string Identity,
    AuditCallerType CallerType = AuditCallerType.System,
    AuditCallerSource CallerSource = AuditCallerSource.Background,
    Guid? CorrelationId = null);
