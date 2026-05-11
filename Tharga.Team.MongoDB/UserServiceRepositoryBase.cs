using Microsoft.AspNetCore.Components.Authorization;
using MongoDB.Driver;
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
        if (user != null) return user;

        var candidate = await CreateUserEntityAsync(claimsPrincipal, identity);
        try
        {
            await _userRepository.AddAsync(candidate);
            return candidate;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Lost the race against a concurrent first-time login for the same identity.
            // The unique Identity index on UserRepositoryCollection guarantees only one wins;
            // re-read and return the winner. Issue Tharga/Platform#65.
            return await _userRepository.GetAsync(identity);
        }
    }

    protected override IAsyncEnumerable<IUser> GetAllAsync()
    {
        return _userRepository.GetAsync();
    }

    public override async Task SeedUserNameAsync(string userKey, string name)
    {
        if (string.IsNullOrEmpty(userKey)) return;

        var user = await _userRepository.GetByKeyAsync(userKey);
        if (user == null) return;
        if (!string.IsNullOrEmpty(user.Name)) return;

        await _userRepository.SetNameAsync(userKey, name);
        InvalidateUserCache(user.Identity);
    }

    public override async Task SetUserNameAsync(string userKey, string name)
    {
        if (string.IsNullOrEmpty(userKey)) return;

        var user = await _userRepository.GetByKeyAsync(userKey);
        if (user == null) return;

        await _userRepository.SetNameAsync(userKey, name);
        InvalidateUserCache(user.Identity);
    }
}