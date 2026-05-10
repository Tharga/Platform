using Microsoft.Extensions.Logging;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

public class TeamMemberResolverTests
{
    [Fact]
    public void Resolve_NoMatch_ReturnsNull()
    {
        var logger = new RecordingLogger();
        var members = new[]
        {
            new FakeMember("a"),
            new FakeMember("b")
        };

        var result = TeamMemberResolver.Resolve(members, m => m.Key == "x", logger, "team-1", "x");

        Assert.Null(result);
        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public void Resolve_SingleMatch_ReturnsIt_NoWarning()
    {
        var logger = new RecordingLogger();
        var target = new FakeMember("b");
        var members = new[]
        {
            new FakeMember("a"),
            target,
            new FakeMember("c")
        };

        var result = TeamMemberResolver.Resolve(members, m => m.Key == "b", logger, "team-1", "b");

        Assert.Same(target, result);
        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public void Resolve_TwoMatches_ReturnsFirst_LogsWarning()
    {
        var logger = new RecordingLogger();
        var first = new FakeMember("dup");
        var second = new FakeMember("dup");
        var members = new[]
        {
            new FakeMember("a"),
            first,
            second
        };

        var result = TeamMemberResolver.Resolve(members, m => m.Key == "dup", logger, "team-42", "dup");

        Assert.Same(first, result);
        var warning = Assert.Single(logger.Warnings);
        Assert.Contains("team-42", warning);
        Assert.Contains("dup", warning);
        Assert.Contains("2", warning);
    }

    [Fact]
    public void Resolve_ThreeMatches_ReturnsFirst_LogsWarningWithCount()
    {
        var logger = new RecordingLogger();
        var first = new FakeMember("dup");
        var members = new[]
        {
            first,
            new FakeMember("dup"),
            new FakeMember("dup")
        };

        var result = TeamMemberResolver.Resolve(members, m => m.Key == "dup", logger, "team-7", "dup");

        Assert.Same(first, result);
        var warning = Assert.Single(logger.Warnings);
        Assert.Contains("3", warning);
    }

    [Fact]
    public void Resolve_NullLogger_DoesNotThrow_OnDuplicates()
    {
        var first = new FakeMember("dup");
        var members = new[] { first, new FakeMember("dup") };

        var result = TeamMemberResolver.Resolve(members, m => m.Key == "dup", logger: null, "team-1", "dup");

        Assert.Same(first, result);
    }

    private sealed class FakeMember : ITeamMember
    {
        public FakeMember(string key) { Key = key; }
        public string Key { get; }
        public string Name => null;
        public Invitation Invitation => null;
        public DateTime? LastSeen => null;
        public MembershipState? State => null;
        public AccessLevel AccessLevel => AccessLevel.User;
        public string[] TenantRoles => null;
        public string[] ScopeOverrides => null;
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Warnings { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
