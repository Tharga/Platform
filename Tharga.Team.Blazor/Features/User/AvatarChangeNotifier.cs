namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// Circuit-scoped signal raised when the current user changes their own avatar, so components elsewhere
/// on the page that show it (e.g. the top-right profile menu) can refresh without a full page reload.
/// </summary>
public sealed class AvatarChangeNotifier
{
    public event Action Changed;

    public void Notify() => Changed?.Invoke();
}
