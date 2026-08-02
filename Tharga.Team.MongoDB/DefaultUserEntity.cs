using Tharga.MongoDB;

namespace Tharga.Team.MongoDB;

/// <summary>
/// The standard stored user. Declares every optional property, so directory linking, activity tracking
/// and icons all work without further wiring.
/// </summary>
/// <remarks>
/// <b>The optional properties are opt-in by shape</b> — the toolkit writes <see cref="LastSeen"/>,
/// <see cref="DirectoryId"/> and <see cref="Icon"/> only when the entity declares somewhere to put them.
/// A default has to choose for the host, and this one chooses all three (user, 2026-08-02).
/// <para>
/// The alternative was a smaller entity, but it fails badly and silently: Verify would find nothing,
/// avatars would never appear, and "last seen" would read <i>Never</i> forever — three documented
/// features quietly doing nothing, with no error to explain why. The cost of this choice is three
/// nullable fields and a <see cref="LastSeen"/> write on activity.
/// </para>
/// <para>
/// Declare your own record implementing <see cref="IUser"/> to persist fewer, or more. There is
/// deliberately no <c>UserEntityBase</c> to derive from — <see cref="IUser"/> is the contract, and the
/// optional properties are exactly the ones a host should be choosing between.
/// </para>
/// </remarks>
public record DefaultUserEntity : EntityBase, IUser
{
    public required string Key { get; init; }
    public required string Identity { get; init; }
    public required string EMail { get; init; }
    public string Name { get; init; }

    /// <summary>Enables linking to an external directory, and Verify.</summary>
    public string DirectoryId { get; init; }

    /// <summary>Enables activity tracking on the admin grids.</summary>
    public DateTime? LastSeen { get; init; }

    /// <summary>Enables stored user icons, as opposed to Gravatar or initials alone.</summary>
    public string Icon { get; init; }
}
