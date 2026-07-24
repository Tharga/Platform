namespace Tharga.Team;

/// <summary>
/// Pluggable storage for icon bytes. The built-in default (<c>MongoIconStore</c> in
/// <c>Tharga.Team.MongoDB</c>) stores icons in the database; a consuming system can replace it via
/// <c>o.AddIconStore&lt;T&gt;()</c> to store elsewhere (Azure Blob, S3, an existing DMS, …). This is the
/// <b>storage</b> seam — where bytes live; where a displayed image is <i>sourced from</i> is the separate
/// <see cref="IIconSource"/> seam.
/// </summary>
public interface IIconStore
{
    /// <summary>
    /// Persist an icon and return an opaque <c>reference</c> to it (an id or URL) for later load/delete
    /// and for storing on the owning team/user record.
    /// </summary>
    Task<string> SaveAsync(IconKind kind, string ownerKey, byte[] data, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Load an icon by the reference returned from <see cref="SaveAsync"/>; null when it does not exist.</summary>
    Task<IconContent> LoadAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Delete an icon by reference. A missing reference is a no-op.</summary>
    Task DeleteAsync(string reference, CancellationToken cancellationToken = default);
}
