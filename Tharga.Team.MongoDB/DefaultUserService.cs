using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Tharga.Toolkit;

namespace Tharga.Team.MongoDB;

/// <summary>
/// The standard user service, storing <see cref="DefaultUserEntity"/> users. Register it and write no
/// user storage code at all.
/// </summary>
/// <remarks>
/// <b>Everything stays overridable.</b> Derive from this to change how a user is built from their claims,
/// or how their key is generated. Use <see cref="UserServiceRepositoryBase{TUserEntity}"/> directly when
/// the user entity needs properties of your own.
/// </remarks>
public class DefaultUserService : UserServiceRepositoryBase<DefaultUserEntity>
{
    public DefaultUserService(
        AuthenticationStateProvider authenticationStateProvider,
        IUserRepository<DefaultUserEntity> userRepository,
        ILogger<UserServiceBase> logger = null,
        IIconStore iconStore = null)
        : base(authenticationStateProvider, userRepository, logger, iconStore)
    {
    }

    /// <summary>
    /// Builds the stored user the first time somebody signs in.
    /// </summary>
    /// <remarks>
    /// <c>EMail</c> falls back to <c>"unknown"</c> rather than throwing: the property is required, and an
    /// identity provider that returns no email address should not stop somebody signing in over a field
    /// that is only ever displayed.
    /// </remarks>
    protected override Task<DefaultUserEntity> CreateUserEntityAsync(ClaimsPrincipal claimsPrincipal, string identity)
    {
        return Task.FromResult(new DefaultUserEntity
        {
            Key = GenerateUserKey(),
            Identity = identity,
            EMail = claimsPrincipal.GetEmail() ?? "unknown",
            Name = claimsPrincipal.GetDisplayName(),
            DirectoryId = claimsPrincipal.GetDirectoryId()
        });
    }

    /// <summary>
    /// The key a new user is stored under. Override to use a format of your own.
    /// </summary>
    /// <remarks>
    /// <b>Matches how team keys are generated</b>, which <c>TeamServiceBase</c> has always owned. Until
    /// this existed the toolkit generated team keys itself but left user keys to the host, so every host
    /// invented its own format for one half of the same model. Virtual because the format is a legitimate
    /// host choice, unlike the rest of building the entity.
    /// <para>
    /// Not uniqueness-checked against the store, unlike the team key: a user is looked up by
    /// <c>Identity</c>, and the key is an opaque handle rather than something anyone types.
    /// </para>
    /// </remarks>
    protected virtual string GenerateUserKey() => StringExtension.UpperCaseAlphaNumericCharacters.Random();
}
