namespace Tharga.Team.Support.Slack;

/// <summary>
/// Posts messages to Slack.
/// </summary>
/// <remarks>
/// Deliberately knows nothing about teams, users or audit entries. Everything in this namespace is
/// about Slack and only Slack, so it can be lifted into a standalone package as a move rather than a
/// rewrite. <c>SlackNamespaceIsolationTests</c> enforces that.
/// </remarks>
public interface ISlackClient
{
    /// <summary>
    /// Posts <paramref name="text"/> to <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Channel name (<c>#alerts</c>) or channel id (<c>C0123456789</c>).</param>
    /// <param name="text">Message body. Slack <c>mrkdwn</c> is supported.</param>
    /// <param name="cancellationToken">Abandons the post; the caller is told it did not happen.</param>
    /// <returns>The outcome. Implementations report failure rather than throwing.</returns>
    Task<SlackPostResult> PostAsync(string channel, string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// What happened to a post.
/// </summary>
/// <param name="Success">True when Slack accepted the message.</param>
/// <param name="Error">Why it did not, or null on success.</param>
public readonly record struct SlackPostResult(bool Success, string Error)
{
    /// <summary>Slack accepted the message.</summary>
    public static SlackPostResult Ok() => new(true, null);

    /// <summary>Slack did not accept it, for the stated reason.</summary>
    public static SlackPostResult Failed(string error) => new(false, error);
}
