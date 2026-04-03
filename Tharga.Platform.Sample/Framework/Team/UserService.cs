using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tharga.Team.MongoDB;

namespace Tharga.Platform.Sample.Framework.Team;

public class UserService : UserServiceRepositoryBase<UserEntity>
{
    public UserService(AuthenticationStateProvider authenticationStateProvider, IUserRepository<UserEntity> userRepository)
        : base(authenticationStateProvider, userRepository)
    {
    }

    protected override Task<UserEntity> CreateUserEntityAsync(ClaimsPrincipal principal, string identity)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? "unknown";
        var name = principal.FindFirst("name")?.Value;
        return Task.FromResult(new UserEntity
        {
            Key = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            Identity = identity,
            EMail = email,
            Name = name
        });
    }
}
