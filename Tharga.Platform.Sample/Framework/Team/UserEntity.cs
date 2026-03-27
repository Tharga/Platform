using Tharga.MongoDB;
using Tharga.Team;

namespace Tharga.Platform.Sample.Framework.Team;

public record UserEntity : EntityBase, IUser
{
    public required string Key { get; init; }
    public required string Identity { get; init; }
    public required string EMail { get; init; }
    public string? Name { get; init; }
}
