using Microsoft.Extensions.Logging;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Audit logger that writes to ILogger with structured logging.
/// Query is not supported — returns empty results.
/// </summary>
public class LoggerAuditLogger : IAuditLogger
{
    private readonly ILogger<LoggerAuditLogger> _logger;

    public LoggerAuditLogger(ILogger<LoggerAuditLogger> logger)
    {
        _logger = logger;
    }

    public void Log(AuditEntry entry)
    {
        var level = entry.Success ? LogLevel.Information : LogLevel.Warning;

        _logger.Log(level,
            "Audit: {EventType} {Feature}:{Action} by {CallerType}:{CallerIdentity} " +
            "team:{TeamKey} scope:{ScopeChecked} result:{ScopeResult} " +
            "duration:{DurationMs}ms success:{Success} correlationId:{CorrelationId} metadata:{Metadata}",
            entry.EventType, entry.Feature, entry.Action,
            entry.CallerType, entry.CallerIdentity,
            entry.TeamKey, entry.ScopeChecked, entry.ScopeResult,
            entry.DurationMs, entry.Success, entry.CorrelationId, FormatMetadata(entry.Metadata));
    }

    private static string FormatMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata is not { Count: > 0 }) return "-";
        return string.Join(", ", metadata.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}"));
    }

    public Task<AuditQueryResult> QueryAsync(AuditQuery query)
    {
        return Task.FromResult(new AuditQueryResult());
    }
}
