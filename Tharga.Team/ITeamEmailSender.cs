namespace Tharga.Team;

/// <summary>
/// Abstraction for sending team-related emails. Consumers can implement this
/// to use their own email infrastructure (SendGrid, Azure, etc.).
/// When not registered, invite dialogs fall back to manual link copying.
/// </summary>
public interface ITeamEmailSender
{
    Task SendInviteAsync(string recipientEmail, string recipientName, string inviteLink, string teamName);
}
