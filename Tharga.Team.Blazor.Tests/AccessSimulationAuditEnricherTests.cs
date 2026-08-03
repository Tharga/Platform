using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Simulation;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// An action taken while simulating is still attributable to the real person.
/// </summary>
public class AccessSimulationAuditEnricherTests
{
    private static AccessSimulation Simulation() => new()
    {
        Kind = AccessSimulationKind.User,
        Label = "Bob",
        Scopes = ["orders:read"]
    };

    private static AccessSimulationAuditEnricher Enricher(string cookieValue)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "alice-subject"),
            new(ClaimTypes.Name, "Alice")
        };
        if (cookieValue != null) claims.Add(new Claim(AccessSimulationCookie.ClaimType, cookieValue));

        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(context);

        return new AccessSimulationAuditEnricher(accessor.Object);
    }

    private static AuditEntry Entry() => new()
    {
        Timestamp = DateTime.UtcNow,
        EventType = AuditEventType.ServiceCall,
        Feature = "team",
        Action = "invite",
        CallerIdentity = "Alice"
    };

    [Fact]
    public void AnEntryMadeWhileSimulating_RecordsWhatWasSimulated()
    {
        var metadata = new Dictionary<string, string>();

        Enricher(AccessSimulationCookie.Write(Simulation())).Enrich(Entry(), metadata);

        Assert.Equal("true", metadata[AccessSimulationMetadataKeys.Active]);
        Assert.Equal(nameof(AccessSimulationKind.User), metadata[AccessSimulationMetadataKeys.Kind]);
        Assert.Equal("Bob", metadata[AccessSimulationMetadataKeys.Target]);
    }

    /// <summary>
    /// The point of the whole design: simulation removes scopes and roles, never identity, so the entry
    /// still names the real caller. The enricher adds context and does not touch the actor.
    /// </summary>
    [Fact]
    public void TheActorRemainsTheRealCaller()
    {
        var entry = Entry();
        var metadata = new Dictionary<string, string>();

        Enricher(AccessSimulationCookie.Write(Simulation())).Enrich(entry, metadata);

        Assert.Equal("Alice", entry.CallerIdentity);
        Assert.DoesNotContain("Bob", metadata.Where(kv => kv.Key != AccessSimulationMetadataKeys.Target).Select(kv => kv.Value));
    }

    [Fact]
    public void WithNoSimulation_NothingIsAdded()
    {
        var metadata = new Dictionary<string, string>();

        Enricher(null).Enrich(Entry(), metadata);

        Assert.Empty(metadata);
    }

    /// <summary>A hand-edited cookie must not make the audit path throw — it records, it does not gate.</summary>
    [Fact]
    public void AMalformedSimulation_AddsNothingAndDoesNotThrow()
    {
        var metadata = new Dictionary<string, string>();

        Enricher("not-base64!!").Enrich(Entry(), metadata);

        Assert.Empty(metadata);
    }

    [Fact]
    public void WithNoHttpContext_NothingIsAdded()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns((HttpContext)null);

        var metadata = new Dictionary<string, string>();
        new AccessSimulationAuditEnricher(accessor.Object).Enrich(Entry(), metadata);

        Assert.Empty(metadata);
    }
}
