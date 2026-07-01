namespace Tharga.Team;

/// <summary>
/// Default <see cref="ITenantRoleVisibilityProvider"/> that makes every tenant role visible for
/// every team. Registered unless a consumer supplies its own provider, keeping the visibility hook
/// non-breaking.
/// </summary>
public sealed class AllRolesVisibleTenantRoleVisibilityProvider : ITenantRoleVisibilityProvider
{
    public Task<bool> IsRoleVisibleAsync(string teamKey, string roleName, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
