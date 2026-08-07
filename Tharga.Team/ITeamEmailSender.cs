namespace Tharga.Team;

/// <summary>
/// Sends the invitation email. Implement it to use your own mail infrastructure (SendGrid, Azure, an existing
/// pipeline) instead of the built-in SMTP sender.
/// </summary>
/// <remarks>
/// <b>Invitations are the only mail the toolkit sends</b> — the single member below is the whole surface, and
/// no other feature is planned to grow one. Worth knowing before deciding between adapting an existing
/// pipeline and configuring SMTP: this is not a mail abstraction the toolkit will keep adding to.
/// <para>
/// When nothing is registered, the invite dialogs fall back to manual link copying rather than failing. That
/// is a supported configuration, not a degraded one — but it looks identical to having forgotten to configure
/// email, so a host seeing invitations go unsent should check whether a sender is registered at all.
/// </para>
/// </remarks>
public interface ITeamEmailSender
{
    Task SendInviteAsync(string recipientEmail, string recipientName, string inviteLink, string teamName);
}
