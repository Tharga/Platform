using System.Reflection;

namespace Tharga.Team;

/// <summary>
/// A persistence extension point on <see cref="UserServiceBase"/> whose default silently does nothing.
/// </summary>
/// <param name="Member">The member a host is expected to override.</param>
/// <param name="Consequence">What is silently lost when it is not overridden.</param>
public sealed record UserServiceGap(string Member, string Consequence)
{
    public override string ToString() => $"{Member} — {Consequence}";
}

/// <summary>
/// Finds persistence extension points a host has left un-overridden.
/// </summary>
/// <remarks>
/// Every member listed here is <c>virtual</c> with a do-nothing default, so forgetting one produces a
/// write that reports success and discards the data. Three such gaps were diagnosed separately in one
/// bug report before this existed; the consuming project asked for this guard over any individual fix.
/// <para>
/// <b>Reflection over the concrete type, not an interface map.</b>
/// <c>SetUserIconReferenceAsync</c> is <c>protected</c>, so it does not appear in an interface map at
/// all — and it is the one that cost the most to find. A guard that could not see it would miss the
/// worst case while appearing to cover the set.
/// </para>
/// <para>
/// Deriving from a storage base such as <c>UserServiceRepositoryBase</c> overrides all of these, so a
/// host on the built-in Mongo store reports nothing. The gaps only appear for a host extending
/// <see cref="UserServiceBase"/> directly.
/// </para>
/// </remarks>
public static class UserServiceCompleteness
{
    private const BindingFlags Declared =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// The gaps in <paramref name="userServiceType"/>, filtered to features the host can actually reach.
    /// </summary>
    /// <remarks>
    /// Reachability matters because an un-overridden member is only a defect if something can call it.
    /// A host with no icon store registered cannot reach the icon path, and reporting it would be noise
    /// of exactly the kind that trains people to ignore startup output.
    /// </remarks>
    public static IReadOnlyList<UserServiceGap> Find(Type userServiceType, bool iconStoreRegistered, bool directoryRegistered)
    {
        if (userServiceType == null) return [];

        var gaps = new List<UserServiceGap>();

        Check("SetUserNameAsync", "renaming a user reports success and changes nothing");
        Check("SeedUserNameAsync", "an invited user's name is discarded when they accept");

        if (iconStoreRegistered)
            Check("SetUserIconReferenceAsync", "an uploaded icon is stored, its reference discarded, and the blob orphaned");

        if (directoryRegistered)
            Check("SetUserDirectoryIdAsync", "the directory link is never persisted, so verification falls back to matching by email");

        return gaps;

        void Check(string member, string consequence)
        {
            if (!Overrides(userServiceType, member)) gaps.Add(new UserServiceGap(member, consequence));
        }
    }

    /// <summary>
    /// Whether any type between <paramref name="type"/> and <see cref="UserServiceBase"/> declares
    /// <paramref name="member"/>. Walking the chain matters: a host may extend an intermediate base of
    /// its own, and that base's override counts.
    /// </summary>
    public static bool Overrides(Type type, string member)
    {
        for (var current = type; current != null && current != typeof(UserServiceBase); current = current.BaseType)
        {
            if (current.GetMethod(member, Declared) != null) return true;
        }

        return false;
    }
}
