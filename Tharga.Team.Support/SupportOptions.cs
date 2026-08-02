using Tharga.Team.Support.Notifications;
using Tharga.Team.Support.Slack;

namespace Tharga.Team.Support;

/// <summary>
/// Everything the support module is configured with, in sections.
/// </summary>
/// <remarks>
/// One options object rather than a lambda per section. Separate optional lambdas would have made
/// <c>AddThargaSupport(o =&gt; o.DefaultChannel = …)</c> silently bind to the wrong parameter, and the
/// module is going to grow sections — email, Jira, the AI bot — so the shape that survives adding one
/// is the one to start with.
/// </remarks>
public class SupportOptions
{
    /// <summary>How to reach Slack. At minimum, <see cref="SlackOptions.BotToken"/>.</summary>
    public SlackOptions Slack { get; } = new();

    /// <summary>Which events are notified, and where.</summary>
    public NotificationOptions Notifications { get; } = new();
}
