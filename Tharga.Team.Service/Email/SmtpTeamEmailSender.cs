using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Tharga.Team;

namespace Tharga.Team.Service.Email;

public class SmtpTeamEmailSender : ITeamEmailSender
{
    private readonly EmailOptions _options;

    public SmtpTeamEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendInviteAsync(string recipientEmail, string recipientName, string inviteLink, string teamName)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            throw new InvalidOperationException("SMTP host is not configured.");

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = !string.IsNullOrEmpty(_options.Username)
                ? new NetworkCredential(_options.Username, _options.Password)
                : null
        };

        var from = new MailAddress(_options.FromAddress, _options.FromName);
        var to = new MailAddress(recipientEmail, recipientName);

        using var message = new MailMessage(from, to)
        {
            Subject = $"You've been invited to join {teamName}",
            Body = $"""
                Hi {recipientName},

                You have been invited to join the team "{teamName}".

                Click the link below to accept the invitation:
                {inviteLink}

                If you did not expect this invitation, you can safely ignore this email.
                """,
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
    }
}
