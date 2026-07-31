namespace Tharga.Team.Service.Audit;

/// <summary>How the call reached the application.</summary>
public enum AuditCallerSource
{
    /// <summary>An HTTP request authenticated with an API key.</summary>
    Api,

    /// <summary>An HTTP request from the web UI (cookie or federated sign-in).</summary>
    Web,

    /// <summary>No HTTP request — a hosted service, message handler or scheduled job.</summary>
    Background,

    /// <summary>Not determinable.</summary>
    Unknown
}
