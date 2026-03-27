namespace Tharga.Team;

public interface IUser
{
    public string Key { get; }
    public string Identity { get; }
    public string EMail { get; }

    /// <summary>
    /// Display name for the user. Defaults to null.
    /// Consumers should populate this from the 'name' claim in CreateUserEntityAsync.
    /// Used for default team names and member display names.
    /// </summary>
    public string Name => null;
}