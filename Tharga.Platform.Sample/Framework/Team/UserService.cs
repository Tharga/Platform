using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tharga.Team.MongoDB;
using Tharga.Toolkit;

namespace Tharga.Platform.Sample.Framework.Team;

public class UserService : UserServiceRepositoryBase<UserEntity>
{
    public UserService(AuthenticationStateProvider authenticationStateProvider, IUserRepository<UserEntity> userRepository)
        : base(authenticationStateProvider, userRepository)
    {
    }

    protected override Task<UserEntity> CreateUserEntityAsync(ClaimsPrincipal principal, string identity)
    {
        var email = principal.GetEmail() ?? "unknown";
        var name = principal.GetDisplayName();
        return Task.FromResult(new UserEntity
        {
            Key = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            Identity = identity,
            EMail = email,
            Name = name
        });
    }
}
