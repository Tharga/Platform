using Tharga.Team.Service.Audit;

namespace Tharga.Team.Sample.Framework;

/// <summary>
/// Demonstrates auditing work that has no HTTP request behind it — the shape a hosted service, message
/// handler or scheduled job would use.
/// </summary>
/// <remarks>
/// Without the scope this job's entries would be attributed to <see cref="AuditCallerType.Unknown"/>,
/// because there is no principal to name. Before that, they were attributed to a <i>user</i> with a null
/// identity, which read as a person having done it.
/// <para>
/// Runs once shortly after startup so <c>/audit</c> has something to show without waiting. A real job
/// would push a fresh scope per unit of work, with that unit's own correlation id.
/// </para>
/// </remarks>
public class SampleBackgroundJob(
    IAuditContextAccessor auditContext,
    IAuditEntryFactory auditEntryFactory,
    IAuditLogger auditLogger,
    ILogger<SampleBackgroundJob> logger)
    : BackgroundService
{
    private const string JobName = "sample-background-job";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // One scope per unit of work. The correlation id groups every entry this run writes; without it
        // each entry gets its own and the grouping cannot be reconstructed afterwards.
        using var _ = auditContext.Push(new AuditActor(JobName, CorrelationId: Guid.NewGuid()));

        auditLogger.Log(auditEntryFactory.Create(
            feature: "sample",
            action: "background-run",
            methodName: nameof(ExecuteAsync),
            metadata: new Dictionary<string, string> { ["trigger"] = "startup" }));

        logger.LogInformation("Sample background job wrote an audit entry as '{JobName}'. See /audit.", JobName);
    }
}
