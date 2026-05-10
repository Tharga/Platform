using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

public abstract class TeamServiceRepositoryBase<TTeamEntity, TMember> : TeamServiceBase
    where TTeamEntity : TeamEntityBase<TMember>
    where TMember : TeamMemberBase
{
    private readonly ITeamRepository<TTeamEntity, TMember> _teamRepository;
    private readonly IMongoDbServiceFactory _mongoDbServiceFactory;

    protected TeamServiceRepositoryBase(IUserService userService, ITeamRepository<TTeamEntity, TMember> teamRepository, IMongoDbServiceFactory mongoDbServiceFactory)
        : base(userService)
    {
        _teamRepository = teamRepository;
        _mongoDbServiceFactory = mongoDbServiceFactory;
    }

    protected abstract Task<TTeamEntity> CreateTeam(string teamKey, string name, IUser user, string displayName);
    protected abstract Task<TMember> CreateTeamMember(InviteUserModel model);

    protected override async Task<ITeam> GetTeamAsync(string teamKey)
    {
        return await _teamRepository.GetAsync(teamKey);
    }

    protected override async Task<ITeam> CreateTeamAsync(string teamKey, string name, IUser user, string displayName)
    {
        var team = await CreateTeam(teamKey, name, user, displayName);

        await _teamRepository.AddAsync(team);

        return team;
    }

    protected override Task SetTeamNameAsync(string teamKey, string name)
    {
        return _teamRepository.RenameAsync(teamKey, name);
    }

    protected override async Task DeleteTeamAsync(string teamKey)
    {
        var databaseContext = new DatabaseContext { DatabasePart = teamKey };
        var service = _mongoDbServiceFactory.GetMongoDbService(() => databaseContext);
        var databaseName = service.GetDatabaseName();
        service.DropDatabase(databaseName);

        await _teamRepository.DeleteAsync(teamKey);
    }

    protected override async Task AddTeamMemberAsync(string teamKey, InviteUserModel model)
    {
        var memberModel = await CreateTeamMember(model);

        // Auto-generate Member.Key if not set by the consumer (typical for invited members
        // that don't yet correspond to a User document)
        if (string.IsNullOrEmpty(memberModel.Key))
        {
            memberModel = memberModel with { Key = Guid.NewGuid().ToString() };
        }

        // Auto-generate Invitation if not set by the consumer
        if (memberModel.Invitation == null && !string.IsNullOrEmpty(model.Email))
        {
            memberModel = memberModel with
            {
                Invitation = new Invitation
                {
                    EMail = model.Email,
                    InviteKey = Guid.NewGuid().ToString(),
                    InviteTime = DateTime.UtcNow
                }
            };
        }

        // Default state to Invited if not set
        if (memberModel.State == null)
        {
            memberModel = memberModel with { State = MembershipState.Invited };
        }

        await _teamRepository.AddMemberAsync(teamKey, memberModel);
    }

    protected override Task RemoveTeamMemberAsync(string teamKey, string userKey)
    {
        return _teamRepository.RemoveMemberAsync(teamKey, userKey);
    }

    protected override Task<ITeam> SetTeamMemberInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept)
    {
        return _teamRepository.SetInvitationResponseAsync(teamKey, userKey, inviteKey, accept);
    }

    protected override async Task<string> GetInvitedMemberNameAsync(string teamKey, string inviteKey)
    {
        var team = await _teamRepository.GetAsync(teamKey);
        var member = team?.Members.FirstOrDefault(x => x.Invitation != null && x.Invitation.InviteKey == inviteKey);
        return member?.Name;
    }

    protected override Task SetTeamMemberLastSeenAsync(string teamKey, string userKey)
    {
        return _teamRepository.SetLastSeenAsync(teamKey, userKey, DateTime.UtcNow);
    }

    protected override async Task<ITeamMember> GetTeamMembersAsync(string teamKey, string userKey)
    {
        var team = await _teamRepository.GetTeamsByUserAsync(userKey).FirstOrDefaultAsync(x => x.Key == teamKey);
        return team?.Members.FirstOrDefault(x => x.Key == userKey);
    }

    protected override Task SetTeamMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        return _teamRepository.SetMemberRoleAsync(teamKey, userKey, accessLevel);
    }

    protected override Task SetTeamMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles)
    {
        return _teamRepository.SetMemberTenantRolesAsync(teamKey, userKey, tenantRoles);
    }

    protected override Task SetTeamMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides)
    {
        return _teamRepository.SetMemberScopeOverridesAsync(teamKey, userKey, scopeOverrides);
    }

    protected override Task SetTeamMemberNameAsync(string teamKey, string userKey, string name)
    {
        return _teamRepository.SetMemberNameAsync(teamKey, userKey, name);
    }

    protected override IAsyncEnumerable<ITeam> GetTeamsAsync(IUser user)
    {
        return _teamRepository.GetTeamsByUserAsync(user.Key);
    }

    protected override Task SetTeamConsentInternalAsync(string teamKey, string[] consentedRoles)
    {
        return _teamRepository.SetConsentAsync(teamKey, consentedRoles);
    }

    protected override IAsyncEnumerable<ITeam> GetConsentedTeamsInternalAsync(string[] userRoles)
    {
        return _teamRepository.GetTeamsByConsentAsync(userRoles);
    }
}