using Tharga.Team;
using Tharga.Team.Blazor.Features.Roles;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Editing working-set behind the <c>TenantRoleManager</c> component (Tharga/Platform#117):
/// load/add/remove/validate/project custom roles.
/// </summary>
public class TenantRoleManagerModelTests
{
    [Fact]
    public void Load_then_ToDefinitions_round_trips_roles()
    {
        var model = new TenantRoleManagerModel();
        model.Load([new TenantRoleDefinition("Registrar", ["case:read", "case:write"], "Registers cases")]);

        var definitions = model.ToDefinitions();

        var role = Assert.Single(definitions);
        Assert.Equal("Registrar", role.Name);
        Assert.Equal(["case:read", "case:write"], role.Scopes);
        Assert.Equal("Registers cases", role.Description);
    }

    [Fact]
    public void AddNew_and_Remove_mutate_the_working_set()
    {
        var model = new TenantRoleManagerModel();
        var added = model.AddNew();
        added.Name = "Reader";
        added.Scopes = ["case:read"];

        Assert.Single(model.Roles);

        model.Remove(added);
        Assert.Empty(model.Roles);
    }

    [Fact]
    public void ToDefinition_trims_name_and_nulls_blank_description()
    {
        var model = new TenantRoleManagerModel();
        var role = model.AddNew();
        role.Name = "  Reader  ";
        role.Description = "   ";
        role.Scopes = ["case:read"];

        var definition = Assert.Single(model.ToDefinitions());
        Assert.Equal("Reader", definition.Name);
        Assert.Null(definition.Description);
    }

    [Fact]
    public void Validate_flags_empty_names()
    {
        var model = new TenantRoleManagerModel();
        model.AddNew().Name = "   ";

        var errors = model.Validate([]);

        Assert.Contains(errors, e => e.Contains("empty"));
    }

    [Fact]
    public void Validate_flags_duplicate_names()
    {
        var model = new TenantRoleManagerModel();
        model.AddNew().Name = "Reader";
        model.AddNew().Name = "Reader";

        var errors = model.Validate([]);

        Assert.Contains(errors, e => e.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_flags_collision_with_code_role()
    {
        var model = new TenantRoleManagerModel();
        model.AddNew().Name = "Developer";

        var errors = model.Validate(["Developer"]);

        Assert.Contains(errors, e => e.Contains("built-in"));
    }

    [Fact]
    public void Validate_passes_for_a_clean_set()
    {
        var model = new TenantRoleManagerModel();
        var role = model.AddNew();
        role.Name = "Registrar";
        role.Scopes = ["case:read"];

        Assert.Empty(model.Validate(["Developer"]));
    }
}
