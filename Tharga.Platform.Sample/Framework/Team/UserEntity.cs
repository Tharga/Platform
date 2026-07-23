using Tharga.MongoDB;
using Tharga.Team;

namespace Tharga.Platform.Sample.Framework.Team;

public record UserEntity : EntityBase, IUser
{
    public required string Key { get; init; }
    public required string Identity { get; init; }
    public required string EMail { get; init; }
    public string Name { get; init; }

    /// <summary>
    /// Declaring these opts the store into directory linking and activity tracking — the toolkit only
    /// writes LastSeen / DirectoryId when the entity has the property to persist them.
    /// </summary>
    public string DirectoryId { get; init; }

    public DateTime? LastSeen { get; init; }
}
