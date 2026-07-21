using Microsoft.Extensions.Logging;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Guards that <see cref="LoggerAuditLogger"/> no longer silently discards <see cref="AuditEntry.Metadata"/>
/// (#129). This is the default <c>StorageMode</c>, so it is where most consumers would otherwise lose
/// everything the metadata work adds.
/// </summary>
public class LoggerAuditLoggerMetadataTests
{
    private sealed class CapturingLogger : ILogger<LoggerAuditLogger>
    {
        public readonly List<string> Messages = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private static AuditEntry Entry(Dictionary<string, string> metadata) => new()
    {
        Timestamp = DateTime.UtcNow,
        EventType = AuditEventType.ServiceCall,
        Action = "rename",
        Success = true,
        Metadata = metadata
    };

    [Fact]
    public void Log_IncludesMetadataPairs()
    {
        var logger = new CapturingLogger();
        new LoggerAuditLogger(logger).Log(Entry(new Dictionary<string, string>
        {
            [AuditMetadataKeys.TeamNameOld] = "Acme Ltd",
            [AuditMetadataKeys.TeamNameNew] = "Acme Group"
        }));

        var message = Assert.Single(logger.Messages);
        Assert.Contains("team.name.old=Acme Ltd", message);
        Assert.Contains("team.name.new=Acme Group", message);
    }

    [Fact]
    public void Log_WithNoMetadata_RendersDash()
    {
        var logger = new CapturingLogger();
        new LoggerAuditLogger(logger).Log(Entry(null));

        Assert.Contains("metadata:-", Assert.Single(logger.Messages));
    }
}
