namespace Tharga.Team;

/// <summary>
/// Whether the icon feature can actually do what a surface is about to promise.
/// </summary>
/// <remarks>
/// Both prerequisites here are opt-ins that fail silently when unmet: a user entity that does not declare
/// <see cref="IUser.Icon"/> discards the reference write and reports success, and the default
/// <see cref="NoOpIconProcessor"/> returns the image untouched. Neither is wrong as a default — consumers
/// own their entity types, and image processing is a separate package — but "unmet prerequisite" and
/// "worked" must not look the same to the caller. Ask here before offering or attempting the operation.
/// </remarks>
public static class IconCapability
{
    /// <summary>
    /// Whether <paramref name="userEntityType"/> can persist an icon reference — it declares
    /// <see cref="IUser.Icon"/>. The interface member is a default returning null, so an entity that
    /// never opted in still compiles and still reads as null; only the declared property makes the write
    /// stick.
    /// </summary>
    public static bool CanPersistUserIcon(Type userEntityType)
        => userEntityType?.GetProperty(nameof(IUser.Icon)) != null;

    /// <summary>
    /// Whether <paramref name="processor"/> actually transforms an image. The default
    /// <see cref="NoOpIconProcessor"/> does not, so a surface promising automatic downscaling with only
    /// that registered is telling the user something false — oversized uploads are rejected instead.
    /// </summary>
    public static bool CanProcessImages(IIconProcessor processor)
        => processor != null && processor is not NoOpIconProcessor;
}
