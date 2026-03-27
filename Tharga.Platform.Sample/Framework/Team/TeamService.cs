using Tharga.MongoDB;
using Tharga.Team;
using Tharga.Team.MongoDB;

namespace Tharga.Platform.Sample.Framework.Team;

public class TeamService : TeamServiceRepositoryBase<TeamEntity, TeamMember>
{
    public TeamService(IUserService userService, ITeamRepository<TeamEntity, TeamMember> teamRepository, IMongoDbServiceFactory mongoDbServiceFactory)
        : base(userService, teamRepository, mongoDbServiceFactory)
    {
    }

    protected override Task<TeamEntity> CreateTeam(string teamKey, string name, IUser user, string displayName)
    {
        return Task.FromResult(new TeamEntity
        {
            Key = teamKey,
            Name = name,
            Members =
            [
                new TeamMember
                {
                    Key = user.Key,
                    Name = displayName,
                    AccessLevel = AccessLevel.Owner,
                    State = MembershipState.Member
                }
            ]
        });
    }

    protected override Task<TeamMember> CreateTeamMember(InviteUserModel model)
    {
        return Task.FromResult(new TeamMember
        {
            Key = null,
            Name = model.Name,
            Invitation = new Invitation
            {
                EMail = model.Email,
                InviteKey = Guid.NewGuid().ToString(),
                InviteTime = DateTime.UtcNow
            },
            State = MembershipState.Invited,
            AccessLevel = model.AccessLevel
        });
    }
}
