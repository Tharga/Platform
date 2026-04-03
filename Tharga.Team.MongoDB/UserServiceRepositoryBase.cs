using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Tharga.MongoDB;
using Tharga.Toolkit;

namespace Tharga.Team.MongoDB;

public abstract class UserServiceRepositoryBase<TUserEntity> : UserServiceBase
    where TUserEntity : EntityBase, IUser
{
    private readonly IUserRepository<TUserEntity> _userRepository;

    protected UserServiceRepositoryBase(AuthenticationStateProvider authenticationStateProvider, IUserRepository<TUserEntity> userRepository)
        : base(authenticationStateProvider)
    {
        _userRepository = userRepository;
    }

    protected abstract Task<TUserEntity> CreateUserEntityAsync(ClaimsPrincipal claimsPrincipal, string identity);

    protected override async Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal)
    {
        var identity = claimsPrincipal.GetIdentity().Identity;

        var user = await _userRepository.GetAsync(identity);
        if (user == null)
        {
            user = await CreateUserEntityAsync(claimsPrincipal, identity);
            await _userRepository.AddAsync(user);
        }

        return user;
    }

    protected override IAsyncEnumerable<IUser> GetAllAsync()
    {
        return _userRepository.GetAsync();
    }
}