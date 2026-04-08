namespace Tharga.Team.Service.Audit;

public enum AuditEventType
{
    ServiceCall,
    AuthSuccess,
    AuthFailure,
    ScopeDenial,
    AccessLevelDenial,
    DataChange,
    RateLimit
}
