using Microsoft.Extensions.Options;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Tests for the <see cref="IAuditEnricher"/> hook (#129): consumer metadata reaches the entry, an
/// enricher can never break logging, and the merge is add-only with the toolkit winning.
/// </summary>
public class AuditEnricherTests
{
    private sealed class RecordingAuditLogger : IAuditLogger
    {
        public readonly List<AuditEntry> Entries = [];
        public void Log(AuditEntry entry) => Entries.Add(entry);
        public Task<AuditQueryResult> QueryAsync(AuditQuery query) => Task.FromResult(new AuditQueryResult());
    }

    private sealed class DelegateEnricher(Action<AuditEntry, IDictionary<string, string>> enrich) : IAuditEnricher
    {
        public void Enrich(AuditEntry entry, IDictionary<string, string> metadata) => enrich(entry, metadata);
    }

    private static AuditEntry Entry(Dictionary<string, string> metadata = null) => new()
    {
        Timestamp = DateTime.UtcNow,
        EventType = AuditEventType.ServiceCall,
        Action = "rename",
        Metadata = metadata
    };

    private static (CompositeAuditLogger sut, RecordingAuditLogger recorder) Build(params IAuditEnricher[] enrichers)
    {
        var recorder = new RecordingAuditLogger();
        var sut = new CompositeAuditLogger([recorder], Options.Create(new AuditOptions()), enrichers);
        return (sut, recorder);
    }

    [Fact]
    public void Enricher_AddsMetadataToTheEntry()
    {
        var (sut, recorder) = Build(new DelegateEnricher((_, m) => m["request.id"] = "abc-123"));

        sut.Log(Entry());

        Assert.Equal("abc-123", Assert.Single(recorder.Entries).Metadata["request.id"]);
    }

    [Fact]
    public void Enricher_SeesToolkitMetadataOnTheEntry()
    {
        string observed = null;
        var (sut, _) = Build(new DelegateEnricher((e, _) => observed = e.Metadata?[AuditMetadataKeys.TeamNameNew]));

        sut.Log(Entry(new Dictionary<string, string> { [AuditMetadataKeys.TeamNameNew] = "Acme" }));

        Assert.Equal("Acme", observed);
    }

    /// <summary>Add-only: an enricher cannot overwrite a key the toolkit already recorded.</summary>
    [Fact]
    public void Enricher_CannotOverwriteAToolkitKey()
    {
        var (sut, recorder) = Build(new DelegateEnricher((_, m) => m[AuditMetadataKeys.TeamNameNew] = "Forged"));

        sut.Log(Entry(new Dictionary<string, string> { [AuditMetadataKeys.TeamNameNew] = "Acme" }));

        Assert.Equal("Acme", Assert.Single(recorder.Entries).Metadata[AuditMetadataKeys.TeamNameNew]);
    }

    /// <summary>Registration order decides a key conflict between two enrichers: first writer wins.</summary>
    [Fact]
    public void Enrichers_FirstWriterOfAKeyWins()
    {
        var (sut, recorder) = Build(
            new DelegateEnricher((_, m) => m["source"] = "first"),
            new DelegateEnricher((_, m) => m["source"] = "second"));

        sut.Log(Entry());

        Assert.Equal("first", Assert.Single(recorder.Entries).Metadata["source"]);
    }

    /// <summary>
    /// The whole point of the safety rule: a throwing enricher is skipped, the entry is still logged,
    /// and other enrichers' contributions survive.
    /// </summary>
    [Fact]
    public void ThrowingEnricher_IsSkipped_EntryStillLogged_OthersStillApply()
    {
        var (sut, recorder) = Build(
            new DelegateEnricher((_, _) => throw new InvalidOperationException("boom")),
            new DelegateEnricher((_, m) => m["survived"] = "yes"));

        sut.Log(Entry());

        var entry = Assert.Single(recorder.Entries);
        Assert.Equal("yes", entry.Metadata["survived"]);
    }

    [Fact]
    public void NoEnrichers_LeavesTheEntryUntouched()
    {
        var (sut, recorder) = Build();
        var original = Entry(new Dictionary<string, string> { ["k"] = "v" });

        sut.Log(original);

        Assert.Same(original, Assert.Single(recorder.Entries));
    }

    [Fact]
    public void Enricher_ThatAddsNothing_LeavesTheEntryUntouched()
    {
        var (sut, recorder) = Build(new DelegateEnricher((_, _) => { }));
        var original = Entry(new Dictionary<string, string> { ["k"] = "v" });

        sut.Log(original);

        Assert.Same(original, Assert.Single(recorder.Entries));
    }

    /// <summary>Filtered-out entries are never enriched — no wasted work, and enrichers can't resurrect them.</summary>
    [Fact]
    public void FilteredEntry_IsNotEnrichedOrLogged()
    {
        var recorder = new RecordingAuditLogger();
        var options = Options.Create(new AuditOptions { ExcludedActions = ["rename"] });
        var ran = false;
        var sut = new CompositeAuditLogger([recorder], options, [new DelegateEnricher((_, _) => ran = true)]);

        sut.Log(Entry());

        Assert.Empty(recorder.Entries);
        Assert.False(ran);
    }
}
