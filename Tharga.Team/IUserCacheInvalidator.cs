namespace Tharga.Team;

/// <summary>
/// Implemented by a user store that caches resolved users, so something outside it can drop a stale
/// entry. <see cref="UserServiceBase"/> implements it; a store written from scratch need not.
/// </summary>
/// <remarks>
/// This exists because the cache belongs to the toolkit and the writes do not. <c>UserServiceBase</c>
/// invalidates in the paths it owns, and a host overriding one of those to supply persistence
/// <i>replaces</i> the path that invalidates — so the write commits and every later read is served
/// stale. The host has no reason to know the cache exists, and nothing told it.
/// <para>
/// The symptom is worth recognising: a change appears not to take, <b>survives every page reload, and
/// corrects only on process restart</b>. Nothing else looks like that. A write that never landed looks
/// identical on screen and has the opposite fix, which is what made it cost two diagnoses.
/// </para>
/// <para>
/// Keyed by <see cref="IUser.Key"/> rather than identity because that is what the mutating members take.
/// The cache is small — one entry per signed-in user per process — so the scan is cheaper than
/// maintaining a second index.
/// </para>
/// </remarks>
public interface IUserCacheInvalidator
{
    /// <summary>
    /// Drops any cached copy of the user with this key. A no-op when nothing is cached for it, so it is
    /// always safe to call and safe to call twice.
    /// </summary>
    void InvalidateUserByKey(string userKey);
}
