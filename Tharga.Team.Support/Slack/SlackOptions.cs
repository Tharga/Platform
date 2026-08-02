namespace Tharga.Team.Support.Slack;

/// <summary>
/// Connection settings for the Slack transport.
/// </summary>
public class SlackOptions
{
    /// <summary>
    /// Bot user OAuth token, <c>xoxb-…</c>. The app needs the <c>chat:write</c> scope and must be a
    /// member of every channel it posts to.
    /// </summary>
    /// <remarks>
    /// Until this is set the transport posts nothing. That is the intended state for a host that
    /// installed the package but has not configured Slack — see <see cref="SlackClient"/>.
    /// </remarks>
    public string BotToken { get; set; }

    /// <summary>
    /// Slack API base address. Overridable so a test or a proxy can point elsewhere; there is no reason
    /// to change it in production.
    /// </summary>
    public string ApiBaseAddress { get; set; } = "https://slack.com/api/";

    /// <summary>How long a single post may take before it is abandoned. Default 10 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
