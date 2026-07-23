namespace Tharga.Team;

/// <summary>
/// A user as known by the external directory (e.g. Microsoft Entra ID).
/// </summary>
/// <param name="DirectoryId">The directory's id for the user (for Entra: the Graph object id).</param>
/// <param name="Name">The directory display name.</param>
/// <param name="EMail">The directory mail address, falling back to the user principal name when no mail is set.</param>
/// <param name="Enabled">Whether the directory account is enabled; null when the directory did not report it.</param>
public sealed record DirectoryUser(string DirectoryId, string Name, string EMail, bool? Enabled);
