namespace Tharga.Team.Service.Audit;

/// <summary>What kind of actor an audit entry is attributed to.</summary>
public enum AuditCallerType
{
    /// <summary>A person, authenticated through the web sign-in flow.</summary>
    User,

    /// <summary>An API key.</summary>
    ApiKey,

    /// <summary>
    /// The application itself acting without a person behind it — a hosted service, a message handler, a
    /// scheduled job. Declared through an ambient audit scope; never inferred.
    /// </summary>
    System,

    /// <summary>
    /// No actor could be established: no authenticated principal and no ambient audit scope.
    /// </summary>
    /// <remarks>
    /// Deliberately its own value rather than defaulting to <see cref="User"/>. A background caller used
    /// to be recorded as a user with a null identity, which is a false attribution — and read back later,
    /// a false attribution is indistinguishable from a true one. An absent answer can at least be seen.
    /// </remarks>
    Unknown
}
