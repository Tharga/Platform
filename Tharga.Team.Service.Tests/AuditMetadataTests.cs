using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Tests for operation metadata on audit entries (#129) — the plumbing through
/// <see cref="AuditHelper.BuildEntry"/> that lets a decorator record what actually changed.
/// </summary>
public class AuditMetadataTests
{
    private static AuditEntry Build(IReadOnlyDictionary<string, string> metadata)
    {
        return AuditHelper.BuildEntry(
            new HttpContextAccessor(),
            feature: "team",
            action: "rename",
            methodName: "RenameTeamAsync",
            durationMs: 1,
            success: true,
            metadata: metadata);
    }

    [Fact]
    public void BuildEntry_WithoutMetadata_LeavesItNull()
    {
        Assert.Null(Build(null).Metadata);
    }

    /// <summary>
    /// An empty dictionary is normalized to null rather than stored, so downstream renderers and
    /// exporters only have to handle one "nothing here" representation.
    /// </summary>
    [Fact]
    public void BuildEntry_WithEmptyMetadata_LeavesItNull()
    {
        Assert.Null(Build(new Dictionary<string, string>()).Metadata);
    }

    [Fact]
    public void BuildEntry_WithMetadata_RoundTripsEveryPair()
    {
        var entry = Build(new Dictionary<string, string>
        {
            [AuditMetadataKeys.TeamNameOld] = "Acme Ltd",
            [AuditMetadataKeys.TeamNameNew] = "Acme Group"
        });

        Assert.Equal("Acme Ltd", entry.Metadata[AuditMetadataKeys.TeamNameOld]);
        Assert.Equal("Acme Group", entry.Metadata[AuditMetadataKeys.TeamNameNew]);
    }

    /// <summary>
    /// The entry must not alias the caller's dictionary — a decorator reusing or mutating its builder
    /// after logging would otherwise retroactively rewrite an already-recorded entry.
    /// </summary>
    [Fact]
    public void BuildEntry_CopiesMetadata_SoLaterMutationDoesNotAlterTheEntry()
    {
        var source = new Dictionary<string, string> { [AuditMetadataKeys.TeamName] = "Acme" };

        var entry = Build(source);
        source[AuditMetadataKeys.TeamName] = "Changed";
        source["added.after"] = "should not appear";

        Assert.Equal("Acme", entry.Metadata[AuditMetadataKeys.TeamName]);
        Assert.DoesNotContain("added.after", entry.Metadata.Keys);
    }

    [Fact]
    public void BuildEntry_PreservesNullAndEmptyValues()
    {
        var entry = Build(new Dictionary<string, string>
        {
            [AuditMetadataKeys.MemberNameOld] = "",
            [AuditMetadataKeys.MemberNameNew] = null
        });

        Assert.Equal("", entry.Metadata[AuditMetadataKeys.MemberNameOld]);
        Assert.Null(entry.Metadata[AuditMetadataKeys.MemberNameNew]);
    }
}
