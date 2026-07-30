namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Whether a user store persists the external directory link at all, and how to describe its absence.
/// </summary>
/// <remarks>
/// <see cref="IUser.DirectoryId"/> is a default interface member returning null, so a null value alone
/// cannot distinguish "this host never stores a directory id" from "this user has not been resolved
/// against the directory yet". Those read very differently to an operator, and rendering an empty cell
/// implies a third thing again — that the user has no directory account. The store's shape answers the
/// first question: the same opt-in test <c>UserRepository</c> applies before writing.
/// </remarks>
public static class DirectoryLink
{
    /// <summary>
    /// Whether the concrete user entity declares <see cref="IUser.DirectoryId"/> and therefore persists
    /// it. Default interface members are not part of an implementing class's reflected surface, so a
    /// host that has not opted in reports false.
    /// </summary>
    public static bool IsStored(Type userType)
        => userType?.GetProperty(nameof(IUser.DirectoryId)) != null;

    /// <summary>Why a directory id is missing, given whether the store persists one at all.</summary>
    public static string AbsenceText(bool isStored)
        => isStored ? "Not resolved yet" : "Not stored";

    /// <summary>The longer explanation, for a tooltip on the absence text.</summary>
    public static string AbsenceHint(bool isStored)
        => isStored
            ? "This user has not been resolved against the directory yet. It is captured on their next resolve."
            : "This application's user entity does not declare DirectoryId, so no directory id is ever stored.";
}
