using System.Reflection;

namespace Tharga.Team;

/// <summary>
/// Works out which <see cref="ITeamMember"/> type a team service is built around, when the host did not
/// say so explicitly.
/// </summary>
/// <remarks>
/// The two-argument <c>RegisterTeamService&lt;TServiceBase, TUserService&gt;()</c> overload takes no
/// member type, and everything that a component injects — <c>ITeamManagementService</c> and its four
/// sibling facets — is built from <c>TeamManagementService&lt;TMember&gt;</c> and therefore needs one.
/// Without it those facets were simply not registered, and the failure surfaced when a page was rendered
/// rather than when the application started.
/// <para>
/// <b>Inference outranks the default.</b> A host that declared its own member type and used the
/// two-argument overload must get <i>their</i> type, not the toolkit's — handing them
/// <c>TeamMember</c> when their entity holds something else is worse than registering nothing, because
/// it would fail on the data rather than at the seam.
/// </para>
/// </remarks>
public static class TeamMemberTypeResolver
{
    /// <summary>
    /// Returns the member type <paramref name="teamServiceType"/> is built around, or null when it cannot
    /// be determined — a service deriving straight from <c>TeamServiceBase</c>, which is generic in
    /// nothing and so carries no member type to find.
    /// </summary>
    /// <remarks>
    /// Walks the base chain rather than the interfaces: the member type is a type argument of the storage
    /// base (<c>TeamServiceRepositoryBase&lt;TEntity, TMember&gt;</c>), which is what every host deriving
    /// from a storage package has. The first argument assignable to <see cref="ITeamMember"/> wins.
    /// </remarks>
    public static Type Resolve(Type teamServiceType)
    {
        for (var type = teamServiceType; type != null && type != typeof(object); type = type.BaseType)
        {
            if (!type.IsGenericType) continue;

            foreach (var argument in type.GetGenericArguments())
            {
                if (typeof(ITeamMember).IsAssignableFrom(argument) && argument is { IsAbstract: false, IsInterface: false })
                    return argument;
            }
        }

        return null;
    }

    /// <summary>
    /// The member type to register for <paramref name="teamServiceType"/>: the inferred one, else
    /// <paramref name="fallback"/>.
    /// </summary>
    public static Type ResolveOrDefault(Type teamServiceType, Type fallback)
        => Resolve(teamServiceType) ?? fallback;
}
