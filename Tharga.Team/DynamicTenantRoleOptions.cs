namespace Tharga.Team;

/// <summary>
/// Options for runtime-defined (dynamic) tenant roles, configured via
/// <see cref="TenantRoleServiceCollectionExtensions.AddThargaDynamicTenantRoles(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{DynamicTenantRoleOptions})"/>.
/// </summary>
public sealed class DynamicTenantRoleOptions
{
    /// <summary>
    /// The scope required to create / edit / delete a team's custom roles — enforced by the service-layer
    /// authorization decorator and the <c>TenantRoleManager</c> component. Defaults to
    /// <see cref="TeamScopes.Manage"/> (<c>team:manage</c>); set to a narrower scope (e.g. <c>access:manage</c>)
    /// to move custom-role administration onto a dedicated admin surface without also granting the broad
    /// <c>team:manage</c>.
    /// </summary>
    public string ManageScope { get; set; } = TeamScopes.Manage;
}
