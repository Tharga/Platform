using Tharga.Team;
using Tharga.Team.Blazor.Features.Scopes;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for <see cref="ScopeReference.Build"/> — the dynamic projection of the configured scope model
/// (scope → description, granting access levels, granting tenant roles) that <c>ScopeView</c> renders.
/// </summary>
public class ScopeReferenceTests
{
    private static (ScopeRegistry scopes, TenantRoleRegistry roles) BuildRegistries()
    {
        var scopes = new ScopeRegistry();
        scopes.Register("orders:read", AccessLevel.Viewer, "View orders and details.");
        scopes.Register("orders:write", AccessLevel.User, "Create and edit orders.");
        scopes.Register("orders:refund", AccessLevel.Administrator, "Issue refunds.");

        var roles = new TenantRoleRegistry();
        roles.Register("Support", "orders:read");
        roles.Register("Editor", ["orders:write", "orders:refund"], "Content editors.");

        return (scopes, roles);
    }

    private static ScopeRow Row(IReadOnlyList<ScopeRow> rows, string name) => rows.Single(r => r.Name == name);

    [Fact]
    public void ViewerLevelScope_IsGrantedToAllLevels()
    {
        var (scopes, roles) = BuildRegistries();

        var row = Row(ScopeReference.Build(scopes, roles), "orders:read");

        Assert.Equal(
            new[] { AccessLevel.Owner, AccessLevel.Administrator, AccessLevel.User, AccessLevel.Viewer },
            row.AccessLevels);
    }

    [Fact]
    public void UserLevelScope_IsGrantedToOwnerAdminUser_NotViewer()
    {
        var (scopes, roles) = BuildRegistries();

        var row = Row(ScopeReference.Build(scopes, roles), "orders:write");

        Assert.Equal(
            new[] { AccessLevel.Owner, AccessLevel.Administrator, AccessLevel.User },
            row.AccessLevels);
    }

    [Fact]
    public void AdministratorLevelScope_IsGrantedToOwnerAndAdminOnly()
    {
        var (scopes, roles) = BuildRegistries();

        var row = Row(ScopeReference.Build(scopes, roles), "orders:refund");

        Assert.Equal(new[] { AccessLevel.Owner, AccessLevel.Administrator }, row.AccessLevels);
    }

    [Fact]
    public void Roles_AreListedOnlyForTheScopesTheyGrant()
    {
        var (scopes, roles) = BuildRegistries();

        var rows = ScopeReference.Build(scopes, roles);

        Assert.Equal(new[] { "Support" }, Row(rows, "orders:read").Roles);
        Assert.Equal(new[] { "Editor" }, Row(rows, "orders:write").Roles);
        Assert.Equal(new[] { "Editor" }, Row(rows, "orders:refund").Roles);
    }

    [Fact]
    public void Description_FlowsThrough()
    {
        var (scopes, roles) = BuildRegistries();

        Assert.Equal("View orders and details.", Row(ScopeReference.Build(scopes, roles), "orders:read").Description);
    }

    [Fact]
    public void Rows_AreOrdinalSortedByName()
    {
        var (scopes, roles) = BuildRegistries();

        var names = ScopeReference.Build(scopes, roles).Select(r => r.Name).ToArray();

        Assert.Equal(new[] { "orders:read", "orders:refund", "orders:write" }, names);
    }

    [Fact]
    public void NullScopeRegistry_ReturnsEmpty()
    {
        Assert.Empty(ScopeReference.Build(null, null));
    }

    [Fact]
    public void NullRoleRegistry_YieldsEmptyRoleLists_NoThrow()
    {
        var (scopes, _) = BuildRegistries();

        var rows = ScopeReference.Build(scopes, null);

        Assert.NotEmpty(rows);
        Assert.All(rows, r => Assert.Empty(r.Roles));
    }
}
