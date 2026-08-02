using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Team.Service.Audit;
using Tharga.Team.Support.Notifications;
using Tharga.Team.Support.Slack;

namespace Tharga.Team.Support;

/// <summary>
/// Registration for the support module.
/// </summary>
public static class SupportRegistration
{
    /// <summary>
    /// Registers Slack notifications: audited events are matched against
    /// <see cref="NotificationOptions.Routes"/> and posted to the channel the matching route names.
    /// </summary>
    /// <remarks>
    /// Opt-in twice over. Nothing references this package, so no consumer acquires it by installing what
    /// they already had; and once registered it still posts nothing until a Slack bot token and a channel
    /// are configured. Call it after <c>AddThargaAuditLogging</c> — the sink joins the audit fan-out, so
    /// without audit logging there is nothing to notify about.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Slack connection settings and the routing table. Leaving the routes alone keeps the built-ins,
    /// which need only <see cref="NotificationOptions.DefaultChannel"/> to start working.
    /// </param>
    public static IServiceCollection AddThargaSupport(this IServiceCollection services, Action<SupportOptions> configure = null)
    {
        var options = new SupportOptions();
        configure?.Invoke(options);

        // Projected onto the two options types the components consume, so each depends on its own
        // section rather than on the whole module — and so a later section cannot widen what Slack sees.
        services.Configure<SlackOptions>(o =>
        {
            o.BotToken = options.Slack.BotToken;
            o.ApiBaseAddress = options.Slack.ApiBaseAddress;
            o.Timeout = options.Slack.Timeout;
        });
        services.Configure<NotificationOptions>(o =>
        {
            o.DefaultChannel = options.Notifications.DefaultChannel;
            o.Routes = options.Notifications.Routes;
        });

        services.AddHttpClient(SlackClient.HttpClientName);
        services.TryAddSingleton<ISlackClient, SlackClient>();

        // Singleton because CompositeAuditLogger is one, and a scoped sink captured by a singleton is
        // the captive dependency that has already taken this repo's sample down once.
        services.TryAddSingleton<NotificationRouter>();
        services.TryAddSingleton<SlackNotificationSink>();

        // Two registrations of the one instance: the audit fan-out finds it as a logger, the host starts
        // its background pump. Resolving the concrete type in both keeps them the same object — a second
        // instance would queue entries nothing drains.
        services.AddSingleton<IAuditLogger>(sp => sp.GetRequiredService<SlackNotificationSink>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<SlackNotificationSink>());

        return services;
    }
}
