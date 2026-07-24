namespace Tharga.Team;

/// <summary>
/// What an icon belongs to. Lets a single <see cref="IIconStore"/> / <see cref="IIconSource"/> serve
/// both teams and users.
/// </summary>
public enum IconKind
{
    Team,
    User
}
