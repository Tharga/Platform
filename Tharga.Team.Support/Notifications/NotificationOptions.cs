namespace Tharga.Team.Support.Notifications;

/// <summary>
/// Which events reach Slack, and where.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Channel used by any route that does not name one. Until this is set, the built-in routes send
    /// nothing.
    /// </summary>
    public string DefaultChannel { get; set; }

    /// <summary>
    /// The routing table. Replace it to take full control, or edit it to add and remove single events.
    /// </summary>
    /// <remarks>
    /// Starts as <see cref="DefaultRoutes"/> so a host that names a channel gets useful traffic without
    /// writing a table first. Clearing it turns notifications off without unregistering anything.
    /// </remarks>
    public IList<NotificationRoute> Routes { get; set; } = DefaultRoutes();

    /// <summary>
    /// The events worth telling someone about on a fresh install: a team appearing, someone joining or
    /// leaving it, and a user being deleted.
    /// </summary>
    /// <remarks>
    /// <b>The issue also named "user logs on" and "user created". Neither is in this list, because
    /// neither exists as an audited event today</b> — the toolkit audits API-key authentication
    /// (<c>auth:*</c>) but not an interactive logon, and users are created as a side effect of first
    /// sign-in rather than through an audited call. Routing them is a one-line addition here once those
    /// events are raised; a default naming an event nothing emits would look configured and do nothing.
    /// </remarks>
    public static IList<NotificationRoute> DefaultRoutes() =>
    [
        new() { Event = "team:create", Template = "New team *{team.name}* created by {actor}." },
        new() { Event = "team:invite", Template = "{actor} invited *{member.email}* to team {team}." },
        new() { Event = "team:remove-member", Template = "{actor} removed a member from team {team}." },
        new() { Event = "user:delete", Template = "{actor} deleted user *{user.key}*." }
    ];
}
