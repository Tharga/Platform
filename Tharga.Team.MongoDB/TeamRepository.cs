using MongoDB.Driver;

namespace Tharga.Team.MongoDB;

internal class TeamRepository<TTeamEntity, TMember> : ITeamRepository<TTeamEntity, TMember>
    where TTeamEntity : TeamEntityBase<TMember>
    where TMember : TeamMemberBase
{
    private readonly ITeamRepositoryCollection<TTeamEntity, TMember> _collection;

    public TeamRepository(ITeamRepositoryCollection<TTeamEntity, TMember> collection)
    {
        _collection = collection;
    }

    public IAsyncEnumerable<TTeamEntity> GetTeamsByUserAsync(string userKey)
    {
        return _collection.GetAsync(x => x.Members.Any(y => y.Key == userKey && y.State == MembershipState.Member));
    }

    public Task<TTeamEntity> GetAsync(string teamKey)
    {
        return _collection.GetOneAsync(x => x.Key == teamKey);
    }

    public Task AddAsync(TTeamEntity teamEntity)
    {
        return _collection.AddAsync(teamEntity);
    }

    public async Task SetLastSeenAsync(string teamKey, string userKey, DateTime utcNow)
    {
        var filter = new FilterDefinitionBuilder<TTeamEntity>().Eq(x => x.Key, teamKey);
        var team = await _collection.GetOneAsync(filter);
        team = team with
        {
            Members = team.Members.Where(x => x.Key != userKey)
                .Union([team.Members.Single(x => x.Key == userKey) with { LastSeen = utcNow }])
                .ToArray()
        };
        await _collection.ReplaceOneAsync(team);
    }

    public Task AddMemberAsync(string teamKey, TMember member)
    {
        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .AddToSet(x => x.Members, member);
        return _collection.UpdateOneAsync(filter, update);
    }

    public async Task RemoveMemberAsync(string teamKey, string userKey)
    {
        var team = await _collection.GetOneAsync(x => x.Key == teamKey);
        var members = team.Members.Where(x => x.Key != userKey).ToArray();

        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .Set(x => x.Members, members);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task SetMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel)
    {
        var team = await _collection.GetOneAsync(x => x.Key == teamKey);
        var member = team.Members.Single(x => x.Key == userKey);
        member = member with { AccessLevel = accessLevel };
        var members = team.Members.Where(x => x.Key != userKey).Union([member]);

        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .Set(x => x.Members, members);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task SetMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles)
    {
        var team = await _collection.GetOneAsync(x => x.Key == teamKey);
        var member = team.Members.Single(x => x.Key == userKey);
        member = member with { TenantRoles = tenantRoles };
        var members = team.Members.Where(x => x.Key != userKey).Union([member]);

        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .Set(x => x.Members, members);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task SetMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides)
    {
        var team = await _collection.GetOneAsync(x => x.Key == teamKey);
        var member = team.Members.Single(x => x.Key == userKey);
        member = member with { ScopeOverrides = scopeOverrides };
        var members = team.Members.Where(x => x.Key != userKey).Union([member]);

        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .Set(x => x.Members, members);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task SetMemberNameAsync(string teamKey, string userKey, string name)
    {
        var team = await _collection.GetOneAsync(x => x.Key == teamKey);
        var member = team.Members.Single(x => x.Key == userKey);
        var trimmed = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        member = member with { Name = trimmed };
        var members = team.Members.Where(x => x.Key != userKey).Union([member]);

        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .Set(x => x.Members, members);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task<ITeam> SetInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept)
    {
        var team = await _collection.GetOneAsync(x => x.Key == teamKey);
        var member = team.Members.Single(x => x.Invitation != null && x.Invitation.InviteKey == inviteKey);
        if (accept)
        {
            member = member with
            {
                Key = userKey,
                Name = null,
                Invitation = null,
                LastSeen = DateTime.UtcNow,
                State = MembershipState.Member
            };
        }
        else
        {
            member = member with
            {
                Key = userKey,
                LastSeen = DateTime.UtcNow,
                State = MembershipState.Rejected
            };
        }

        var members = team.Members
            .Where(x => x.Invitation == null || x.Invitation.InviteKey != inviteKey)
            .Union([member]);

        var filter = new FilterDefinitionBuilder<TTeamEntity>()
            .Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>()
            .Set(x => x.Members, members);

        var response = await _collection.UpdateOneAsync(filter, update);
        return await response.GetAfterAsync();
    }

    public Task DeleteAsync(string teamKey)
    {
        return _collection.DeleteOneAsync(x => x.Key == teamKey);
    }

    public Task RenameAsync(string teamKey, string name)
    {
        var filter = new FilterDefinitionBuilder<TTeamEntity>().Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>().Set(x => x.Name, name);
        return _collection.UpdateOneAsync(filter, update);
    }

    public Task SetConsentAsync(string teamKey, string[] consentedRoles)
    {
        var filter = new FilterDefinitionBuilder<TTeamEntity>().Eq(x => x.Key, teamKey);
        var update = new UpdateDefinitionBuilder<TTeamEntity>().Set(x => x.ConsentedRoles, consentedRoles);
        return _collection.UpdateOneAsync(filter, update);
    }

    public IAsyncEnumerable<TTeamEntity> GetTeamsByConsentAsync(string[] roles)
    {
        return _collection.GetAsync(x => x.ConsentedRoles != null && x.ConsentedRoles.Any(r => roles.Contains(r)));
    }
}