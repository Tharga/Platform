using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

public class RoleSelectionResolverTests
{
    [Fact]
    public void Split_Partitions_Selected_Into_Visible_And_Hidden()
    {
        var (visible, hidden) = RoleSelectionResolver.Split(
            visibleRoleNames: new[] { "Editor", "Viewer" },
            selected: new[] { "Editor", "CaseAdministrator" });

        Assert.Equal(new[] { "Editor" }, visible);
        Assert.Equal(new[] { "CaseAdministrator" }, hidden);
    }

    [Fact]
    public void Split_All_Visible_When_Nothing_Hidden()
    {
        var (visible, hidden) = RoleSelectionResolver.Split(
            visibleRoleNames: new[] { "Editor", "Viewer" },
            selected: new[] { "Editor", "Viewer" });

        Assert.Equal(new[] { "Editor", "Viewer" }, visible);
        Assert.Empty(hidden);
    }

    [Fact]
    public void Split_Handles_Null_Inputs()
    {
        var (visible, hidden) = RoleSelectionResolver.Split(null, null);

        Assert.Empty(visible);
        Assert.Empty(hidden);
    }

    [Fact]
    public void Merge_Preserves_Hidden_Assignments()
    {
        // Operator deselected everything visible; a hidden-but-assigned role must survive.
        var result = RoleSelectionResolver.Merge(
            visibleSelection: Array.Empty<string>(),
            hiddenSelected: new[] { "CaseAdministrator" });

        Assert.Equal(new[] { "CaseAdministrator" }, result);
    }

    [Fact]
    public void Merge_Combines_Visible_And_Hidden_Without_Duplicates()
    {
        var result = RoleSelectionResolver.Merge(
            visibleSelection: new[] { "Editor", "CaseAdministrator" },
            hiddenSelected: new[] { "CaseAdministrator" });

        Assert.Equal(new[] { "Editor", "CaseAdministrator" }, result);
    }

    [Fact]
    public void Split_Then_Merge_RoundTrips_When_Selection_Unchanged()
    {
        var visibleRoles = new[] { "Editor", "Viewer" };
        var assigned = new[] { "Editor", "CaseAdministrator" };

        var (visible, hidden) = RoleSelectionResolver.Split(visibleRoles, assigned);
        var result = RoleSelectionResolver.Merge(visible, hidden);

        Assert.Equal(new[] { "Editor", "CaseAdministrator" }, result);
    }
}
