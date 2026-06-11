using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests;

public class AuditRetentionTests
{
    [Theory]
    [InlineData(null)]   // unset → keep forever
    [InlineData(0)]      // was the instant-expiry footgun → now keep forever
    [InlineData(-5)]     // negative → keep forever
    public void GetExpireAfter_DisabledRetention_ReturnsNull(int? days)
    {
        Assert.Null(AuditRetention.GetExpireAfter(days));
    }

    [Fact]
    public void GetExpireAfter_AboveMax_ReturnsNull_NoOverflow()
    {
        // int.MaxValue used to overflow TimeSpan.FromDays; now treated as keep-forever.
        Assert.Null(AuditRetention.GetExpireAfter(int.MaxValue));
        Assert.Null(AuditRetention.GetExpireAfter(AuditRetention.MaxRetentionDays + 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    [InlineData(365)]
    public void GetExpireAfter_PositiveRetention_ReturnsMatchingTimeSpan(int days)
    {
        Assert.Equal(TimeSpan.FromDays(days), AuditRetention.GetExpireAfter(days));
    }

    [Fact]
    public void GetExpireAfter_AtMax_ReturnsTimeSpan()
    {
        Assert.Equal(TimeSpan.FromDays(AuditRetention.MaxRetentionDays),
            AuditRetention.GetExpireAfter(AuditRetention.MaxRetentionDays));
    }

    [Fact]
    public void AuditOptions_DefaultRetention_Is90()
    {
        Assert.Equal(90, new AuditOptions().RetentionDays);
    }
}
