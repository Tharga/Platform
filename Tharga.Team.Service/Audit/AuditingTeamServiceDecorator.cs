using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Tharga.Team.Service.Audit;

/// <summary>
/// Decorator that wraps <see cref="ITeamService"/> and logs audit entries
/// for all mutation operations via <see cref="CompositeAuditLogger"/>.
/// Read operations are passed through without logging.
/// </summary>
public class AuditingTeamServiceDecorator : ITeamService
{
    private readonly ITeamService _inner;
    private readonly CompositeAuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string Feature = "team";

    public AuditingTeamServiceDecorator(ITeamService inner, CompositeAuditLogger auditLogger, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
    }

    public event EventHandler<TeamsListChangedEventArgs> TeamsListChangedEvent
    {
        add => _inner.TeamsListChangedEvent += value;
        remove => _inner.TeamsListChangedEvent -= value;
    }

    public event EventHandler<SelectTeamEventArgs> SelectTeamEvent
    {
        add => _inner.SelectTeamEvent += value;
        remove => _inner.SelectTeamEvent -= value;
    }

    // Read operations — pass through

    public IAsyncEnumerable<ITeam> GetTeamsAsync() => _inner.GetTeamsAsync();
    public IAsyncEnumerable<ITeam<TMember>> GetTeamsAsync<TMember>() where TMember : ITeamMember => _inner.GetTeamsAsync<TMember>();
    public Task<ITeam<TMember>> GetTeamAsync<TMember>(string teamKey) where TMember : ITeamMember => _inner.GetTeamAsync<TMember>(teamKey);
    public Task<ITeamMember> GetTeamMemberAsync(string teamKey, string userKey) => _inner.GetTeamMemberAsync(teamKey, userKey);
    public IAsyncEnumerable<ITeam> GetConsentedTeamsAsync(string[] userRoles) => _inner.GetConsentedTeamsAsync(userRoles);
    public Task SetMemberLastSeenAsync(string teamKey) => _inner.SetMemberLastSeenAsync(teamKey);

    // Mutation operations — log audit entries

    public async Task<ITeam> CreateTeamAsync(string name)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.CreateTeamAsync(name);
            sw.Stop();
            Log("create", nameof(CreateTeamAsync), sw.ElapsedMilliseconds, true, teamKey: result?.Key);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("create", nameof(CreateTeamAsync), sw.ElapsedMilliseconds, false, ex.Message);
            throw;
        }
    }

    public async Task RenameTeamAsync<TMember>(string teamKey, string name) where TMember : ITeamMember
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.RenameTeamAsync<TMember>(teamKey, name);
            sw.Stop();
            Log("rename", nameof(RenameTeamAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("rename", nameof(RenameTeamAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task DeleteTeamAsync<TMember>(string teamKey) where TMember : ITeamMember
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.DeleteTeamAsync<TMember>(teamKey);
            sw.Stop();
            Log("delete", nameof(DeleteTeamAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("delete", nameof(DeleteTeamAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task AddMemberAsync(string teamKey, InviteUserModel model)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.AddMemberAsync(teamKey, model);
            sw.Stop();
            Log("invite", nameof(AddMemberAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("invite", nameof(AddMemberAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task RemoveMemberAsync(string teamKey, string userKey)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.RemoveMemberAsync(teamKey, userKey);
            sw.Stop();
            Log("remove-member", nameof(RemoveMemberAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("remove-member", nameof(RemoveMemberAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SetMemberRoleAsync(teamKey, userKey, accessLevel);
            sw.Stop();
            Log("set-role", nameof(SetMemberRoleAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("set-role", nameof(SetMemberRoleAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SetMemberTenantRolesAsync(teamKey, userKey, tenantRoles);
            sw.Stop();
            Log("set-tenant-roles", nameof(SetMemberTenantRolesAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("set-tenant-roles", nameof(SetMemberTenantRolesAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SetMemberScopeOverridesAsync(teamKey, userKey, scopeOverrides);
            sw.Stop();
            Log("set-scope-overrides", nameof(SetMemberScopeOverridesAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("set-scope-overrides", nameof(SetMemberScopeOverridesAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task SetInvitationResponseAsync(string teamKey, string userKey, string inviteCode, bool accept)
    {
        var action = accept ? "accept-invite" : "reject-invite";
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SetInvitationResponseAsync(teamKey, userKey, inviteCode, accept);
            sw.Stop();
            Log(action, nameof(SetInvitationResponseAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log(action, nameof(SetInvitationResponseAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task SetTeamConsentAsync(string teamKey, string[] consentedRoles)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SetTeamConsentAsync(teamKey, consentedRoles);
            sw.Stop();
            Log("set-consent", nameof(SetTeamConsentAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("set-consent", nameof(SetTeamConsentAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    public async Task TransferOwnershipAsync<TMember>(string teamKey, string newOwnerUserKey) where TMember : ITeamMember
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.TransferOwnershipAsync<TMember>(teamKey, newOwnerUserKey);
            sw.Stop();
            Log("transfer-ownership", nameof(TransferOwnershipAsync), sw.ElapsedMilliseconds, true, teamKey: teamKey);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log("transfer-ownership", nameof(TransferOwnershipAsync), sw.ElapsedMilliseconds, false, ex.Message, teamKey);
            throw;
        }
    }

    private void Log(string action, string methodName, long durationMs, bool success, string errorMessage = null, string teamKey = null)
    {
        var entry = AuditHelper.BuildEntry(_httpContextAccessor, Feature, action, methodName, durationMs, success, errorMessage, teamKey);
        _auditLogger.Log(entry);
    }
}
