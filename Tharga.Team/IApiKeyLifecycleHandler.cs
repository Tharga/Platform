namespace Tharga.Team;

/// <summary>
/// Opt-in hook that receives an API key's private token at the moment it exists — on create and on
/// recycle/regenerate — plus a tokenless signal on delete. Register one or more implementations with
/// <c>AddThargaApiKeyLifecycleHandler&lt;T&gt;()</c>; they are invoked after the corresponding
/// operation succeeds.
/// <para>
/// The token is handed only to in-process code the host explicitly registered. If the handler throws,
/// the originating operation (create/recycle/delete) throws too — capture failures are not swallowed.
/// </para>
/// </summary>
public interface IApiKeyLifecycleHandler
{
    /// <summary>
    /// Invoked after an API key is created, recycled, or deleted. See <see cref="ApiKeyLifecycleContext"/>
    /// for what is available per <see cref="ApiKeyLifecycleReason"/>.
    /// </summary>
    Task OnApiKeyLifecycleAsync(ApiKeyLifecycleContext context);
}
