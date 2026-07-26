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

        // Properties are assigned after the using declaration rather than in an
        // object initializer, so the client is tracked for disposal from the
        // moment it is constructed.
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort);
        client.EnableSsl = _options.UseSsl;
        client.Credentials = !string.IsNullOrEmpty(_options.Username)
            ? new NetworkCredential(_options.Username, _options.Password)
            : null;

        var from = new MailAddress(_options.FromAddress, _options.FromName);
        var to = new MailAddress(recipientEmail, recipientName);

        using var message = new MailMessage(from, to);
        message.Subject = $"You've been invited to join {teamName}";
        message.Body = $"""
            Hi {recipientName},

            You have been invited to join the team "{teamName}".

            Click the link below to accept the invitation:
            {inviteLink}

            If you did not expect this invitation, you can safely ignore this email.
            """;
        message.IsBodyHtml = false;

        await client.SendMailAsync(message);
    }
}
