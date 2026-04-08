using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

public static class ThargaTeamRegistration
{
    public static void AddThargaTeamRepository(this IServiceCollection services, Action<ThargaTeamOptions> options = default)
    {
        var o = new ThargaTeamOptions();
        options?.Invoke(o);

        services.AddSingleton(Options.Create(o));

        if (o._userEntity != null)
        {
            var userEntityType = o._userEntity;

            var userRepositoryInterfaceType = typeof(IUserRepository<>).MakeGenericType(userEntityType);
            var userRepositoryImplementationType = typeof(UserRepository<>).MakeGenericType(userEntityType);

            var userRepositoryCollectionInterfaceType = typeof(IUserRepositoryCollection<>).MakeGenericType(userEntityType);
            var userRepositoryCollectionImplementationType = typeof(UserRepositoryCollection<>).MakeGenericType(userEntityType);

            services.AddTransient(userRepositoryInterfaceType, userRepositoryImplementationType);
            services.AddTransient(userRepositoryCollectionInterfaceType, userRepositoryCollectionImplementationType);
            services.TrackMongoCollection(userRepositoryCollectionInterfaceType, userRepositoryCollectionImplementationType);
        }

        if (o._teamEntity != null && o._teamMemberModel != null)
        {
            var teamEntityType = o._teamEntity;
            var teamMemberModelType = o._teamMemberModel;

            var teamRepositoryInterfaceType = typeof(ITeamRepository<,>).MakeGenericType(teamEntityType, teamMemberModelType);
            var teamRepositoryImplementationType = typeof(TeamRepository<,>).MakeGenericType(teamEntityType, teamMemberModelType);

            var teamRepositoryCollectionInterfaceType = typeof(ITeamRepositoryCollection<,>).MakeGenericType(teamEntityType, teamMemberModelType);
            var teamRepositoryCollectionImplementationType = typeof(TeamRepositoryCollection<,>).MakeGenericType(teamEntityType, teamMemberModelType);

            services.AddTransient(teamRepositoryInterfaceType, teamRepositoryImplementationType);
            services.AddTransient(teamRepositoryCollectionInterfaceType, teamRepositoryCollectionImplementationType);
            services.TrackMongoCollection(teamRepositoryCollectionInterfaceType, teamRepositoryCollectionImplementationType);
        }
    }
}