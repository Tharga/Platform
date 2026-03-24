using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;
using Tharga.Team.Service.Email;

namespace Tharga.Team.Blazor.Tests;

public class EmailRegistrationTests
{
    [Fact]
    public void AddThargaPlatform_WithEmailOptions_RegistersSmtpSender()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.Auth.ValidateConfiguration = false;
            o.Email = new EmailOptions { SmtpHost = "smtp.test.com", FromAddress = "test@test.com" };
        });

        var provider = builder.Services.BuildServiceProvider();
        var sender = provider.GetService<ITeamEmailSender>();

        Assert.NotNull(sender);
        Assert.IsType<SmtpTeamEmailSender>(sender);
    }

    [Fact]
    public void AddThargaPlatform_WithCustomEmailService_RegistersCustomSender()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.Auth.ValidateConfiguration = false;
            o.Email = new EmailOptions { SmtpHost = "smtp.test.com" };
            o.AddEmailService<FakeEmailSender>();
        });

        var provider = builder.Services.BuildServiceProvider();
        var sender = provider.GetService<ITeamEmailSender>();

        Assert.NotNull(sender);
        Assert.IsType<FakeEmailSender>(sender);
    }

    [Fact]
    public void AddThargaPlatform_WithoutEmail_DoesNotRegisterSender()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaPlatform(o =>
        {
            o.Auth.ValidateConfiguration = false;
        });

        var provider = builder.Services.BuildServiceProvider();
        var sender = provider.GetService<ITeamEmailSender>();

        Assert.Null(sender);
    }

    private class FakeEmailSender : ITeamEmailSender
    {
        public Task SendInviteAsync(string recipientEmail, string recipientName, string inviteLink, string teamName)
            => Task.CompletedTask;
    }
}
