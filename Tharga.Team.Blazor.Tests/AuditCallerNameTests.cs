using Tharga.Team.Blazor.Features.Audit;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Tests;

public class AuditCallerNameTests
{
    [Fact]
    public void GetCallerDisplayName_Returns_Resolved_Name_When_Cached()
    {
        var view = new AuditLogView();
        view._callerNameCache["user-id-123"] = "alice@example.com";

        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.ServiceCall,
            CallerIdentity = "user-id-123"
        };

        var result = view.GetCallerDisplayName(entry);

        Assert.Equal("alice@example.com", result);
    }

    [Fact]
    public void GetCallerDisplayName_Falls_Back_To_Raw_Identity_When_Not_Cached()
    {
        var view = new AuditLogView();

        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.ServiceCall,
            CallerIdentity = "unknown-id-456"
        };

        var result = view.GetCallerDisplayName(entry);

        Assert.Equal("unknown-id-456", result);
    }

    [Fact]
    public void GetCallerDisplayName_Returns_Empty_When_CallerIdentity_Is_Null()
    {
        var view = new AuditLogView();

        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.ServiceCall,
            CallerIdentity = null
        };

        var result = view.GetCallerDisplayName(entry);

        Assert.Equal("", result);
    }

    [Fact]
    public void GetCallerDisplayName_Is_Case_Insensitive()
    {
        var view = new AuditLogView();
        view._callerNameCache["USER@EXAMPLE.COM"] = "User Name";

        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.ServiceCall,
            CallerIdentity = "user@example.com"
        };

        var result = view.GetCallerDisplayName(entry);

        Assert.Equal("User Name", result);
    }
}
