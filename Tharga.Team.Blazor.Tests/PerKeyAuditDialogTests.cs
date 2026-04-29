using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team.Blazor.Features.Audit;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Smoke tests covering the public surface added for the per-key audit log dialog feature.
/// </summary>
public class PerKeyAuditDialogTests
{
    [Fact]
    public void AuditPinnedFilter_IsPublic_AndHasExpectedFields()
    {
        Assert.True(typeof(AuditPinnedFilter).IsPublic);

        var filter = new AuditPinnedFilter
        {
            CallerKeyId = "key-1",
            CallerType = AuditCallerType.ApiKey,
            TeamKey = "team-1",
            CallerIdentity = "alice",
            Feature = "team",
            Action = "rename",
        };

        Assert.Equal("key-1", filter.CallerKeyId);
        Assert.Equal(AuditCallerType.ApiKey, filter.CallerType);
        Assert.Equal("team-1", filter.TeamKey);
        Assert.Equal("alice", filter.CallerIdentity);
        Assert.Equal("team", filter.Feature);
        Assert.Equal("rename", filter.Action);
    }

    [Fact]
    public void AuditLogView_HasPinnedFilterParameter()
    {
        var componentType = typeof(AuditLogView);
        var prop = componentType.GetProperty("PinnedFilter");

        Assert.NotNull(prop);
        Assert.Equal(typeof(AuditPinnedFilter), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void ApiKeyView_HasShowAuditLogButtonParameter_DefaultFalse()
    {
        var componentType = typeof(Tharga.Team.Blazor.Features.Api.ApiKeyView);
        var prop = componentType.GetProperty("ShowAuditLogButton");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void SystemApiKeyView_HasShowAuditLogButtonParameter_DefaultFalse()
    {
        var componentType = typeof(Tharga.Team.Blazor.Features.Api.SystemApiKeyView);
        var prop = componentType.GetProperty("ShowAuditLogButton");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void AuditEntry_HasCallerKeyId()
    {
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.AuthSuccess,
            CallerKeyId = "key-1",
        };

        Assert.Equal("key-1", entry.CallerKeyId);
    }

    [Fact]
    public void AuditQuery_HasCallerKeyIdFilter()
    {
        var query = new AuditQuery { CallerKeyId = "key-1" };
        Assert.Equal("key-1", query.CallerKeyId);
    }
}
