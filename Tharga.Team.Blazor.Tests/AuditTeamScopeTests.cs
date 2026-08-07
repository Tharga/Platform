using Tharga.Team.Blazor.Features.Audit;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Which team an audit read is scoped to (Tharga/Team#175). The defect was that
/// <c>AuditLogView.TeamKey</c> scoped the query but not the access decision, so a host passing it alone was
/// refused even holding that team's <c>audit:read</c>.
/// </summary>
public class AuditTeamScopeTests
{
    /// <summary>
    /// The reported case. The access probe passes no team of its own, so with no pin the parameter is the only
    /// thing that can name one — and it has to, or the read is routed to the system-scope service and refused.
    /// </summary>
    [Fact]
    public void TheParameterAlone_NamesTheTeam()
    {
        Assert.Equal("ABC123", AuditTeamScope.Resolve(queryTeamKey: null, pinnedTeamKey: null, parameterTeamKey: "ABC123"));
    }

    /// <summary>A pin outranks the parameter: it is a statement about this dialog, not a default.</summary>
    [Fact]
    public void APin_OutranksTheParameter()
    {
        Assert.Equal("PINNED", AuditTeamScope.Resolve(null, "PINNED", "PARAM"));
    }

    /// <summary>
    /// The query outranks both. <c>ApplyPinnedFilter</c> has already forced a pinned team onto it, so the two
    /// cannot disagree for a grid query; this keeps the resolution honest for reads that come from elsewhere.
    /// </summary>
    [Fact]
    public void TheQuery_OutranksBoth()
    {
        Assert.Equal("QUERY", AuditTeamScope.Resolve("QUERY", "PINNED", "PARAM"));
    }

    /// <summary>
    /// Naming no team must stay system-wide. This is the branch a system API key and a cross-team reader use,
    /// and widening the fix must not have narrowed it.
    /// </summary>
    [Fact]
    public void NoTeamAnywhere_StaysSystemWide()
    {
        Assert.Null(AuditTeamScope.Resolve(null, null, null));
    }

    /// <summary>
    /// Empty is not a team. A blank parameter is what a host renders before its own state resolves, and
    /// treating it as a team would route the read to the team-bound service with nothing to match.
    /// </summary>
    [Theory]
    [InlineData("", "", "")]
    [InlineData("", "", null)]
    [InlineData(null, "", "")]
    public void EmptyIsNotATeam(string query, string pinned, string parameter)
    {
        Assert.Null(AuditTeamScope.Resolve(query, pinned, parameter));
    }

    /// <summary>An empty stronger source falls through to the weaker one rather than blanking the scope.</summary>
    [Fact]
    public void AnEmptyStrongerSource_FallsThrough()
    {
        Assert.Equal("PARAM", AuditTeamScope.Resolve("", "", "PARAM"));
        Assert.Equal("PINNED", AuditTeamScope.Resolve("", "PINNED", "PARAM"));
    }
}
