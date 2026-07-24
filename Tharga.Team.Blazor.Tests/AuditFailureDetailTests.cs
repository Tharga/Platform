using Tharga.Team.Blazor.Features.Audit;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the "OK" column failure helpers on <see cref="AuditLogView"/>. A failed audit entry surfaces a
/// short classification (the audit equivalent of a response code) plus a detailed reason; successful entries
/// surface neither.
/// </summary>
public class AuditFailureDetailTests
{
    private static AuditEntry Failure(
        AuditEventType type,
        string scope = null,
        AuditScopeResult scopeResult = AuditScopeResult.Denied,
        string error = null) =>
        new()
        {
            Timestamp = new DateTime(2026, 07, 24, 0, 0, 0, DateTimeKind.Utc),
            EventType = type,
            Success = false,
            ScopeChecked = scope,
            ScopeResult = scopeResult,
            ErrorMessage = error
        };

    [Fact]
    public void Success_HasNoCodeOrDetail()
    {
        var entry = new AuditEntry
        {
            Timestamp = new DateTime(2026, 07, 24, 0, 0, 0, DateTimeKind.Utc),
            EventType = AuditEventType.ServiceCall,
            Success = true
        };

        Assert.Null(AuditLogView.BuildFailureCode(entry));
        Assert.Null(AuditLogView.BuildFailureDetail(entry));
    }

    [Fact]
    public void Null_HasNoCodeOrDetail()
    {
        Assert.Null(AuditLogView.BuildFailureCode(null));
        Assert.Null(AuditLogView.BuildFailureDetail(null));
    }

    [Theory]
    [InlineData(AuditEventType.ScopeDenial, "ScopeDenial")]
    [InlineData(AuditEventType.AccessLevelDenial, "AccessLevelDenial")]
    [InlineData(AuditEventType.AuthFailure, "AuthFailure")]
    [InlineData(AuditEventType.RateLimit, "RateLimit")]
    public void ClassifiedFailure_UsesEventTypeAsCode(AuditEventType type, string expected)
    {
        Assert.Equal(expected, AuditLogView.BuildFailureCode(Failure(type)));
    }

    [Theory]
    [InlineData(AuditEventType.ServiceCall)]
    [InlineData(AuditEventType.DataChange)]
    public void ExceptionFailure_UsesErrorCode(AuditEventType type)
    {
        Assert.Equal("Error", AuditLogView.BuildFailureCode(Failure(type, error: "boom")));
    }

    [Fact]
    public void Detail_ScopeDenial_IncludesCodeScopeResultAndReason()
    {
        var detail = AuditLogView.BuildFailureDetail(Failure(
            AuditEventType.ScopeDenial,
            scope: "team:manage",
            scopeResult: AuditScopeResult.Denied,
            error: "Missing scope 'team:manage'."));

        Assert.Contains("ScopeDenial", detail);
        Assert.Contains("Scope: team:manage (Denied)", detail);
        Assert.Contains("Reason: Missing scope 'team:manage'.", detail);
    }

    [Fact]
    public void Detail_Exception_HasReasonButNoScopeLine()
    {
        var detail = AuditLogView.BuildFailureDetail(Failure(
            AuditEventType.ServiceCall,
            error: "Object reference not set."));

        Assert.Contains("Error", detail);
        Assert.Contains("Reason: Object reference not set.", detail);
        Assert.DoesNotContain("Scope:", detail);
    }
}
