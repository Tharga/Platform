# Tharga.Team.Entra

Microsoft Entra ID user-directory provider for Tharga Team. Implements `IUserDirectoryService` over
Microsoft Graph so the platform can:

- **Verify** that a local user still exists (and is enabled) in Entra — resolved by the stored directory
  object id, falling back to email/UPN lookup (which also relinks the user).
- **List directory-only users** — users that exist in Entra but not in the platform.
- **Delete** a user from Entra (opt-in, from the user-delete flow). Graph performs a soft delete: the
  user is restorable by an administrator for 30 days, but is removed org-wide immediately.

## Registration

```csharp
builder.Services.AddThargaEntraUserDirectory(builder.Configuration);
```

Configuration is read from the `AzureAd` section (`TenantId`, `ClientId`, `ClientSecret`) — the same
section the Tharga platform sign-in already uses. Override or supply values in code:

```csharp
builder.Services.AddThargaEntraUserDirectory(builder.Configuration, o =>
{
    o.ClientSecret = builder.Configuration["Entra:ClientSecret"];
    // or plug any Azure.Core TokenCredential (e.g. managed identity):
    // o.Credential = new ManagedIdentityCredential();
});
```

## Incomplete configuration hides the directory

Without complete credentials — no `TenantId`, `ClientId` or `ClientSecret`, and no explicit
`Credential` — the directory reports `IsConfigured == false`, and every directory feature (verify
actions, the Directory column, the directory-only tab, the delete-from-directory opt-in) stays hidden
**exactly as if nothing were registered**. Offering a Verify button that throws on click is worse than
not offering it.

Calling the service directly still throws `InvalidOperationException` naming the three settings, so a
host bypassing the UI gets a diagnosis rather than a silent failure.

> **Azure AD B2C has no `TenantId` key.** The tenant is embedded in `Authority`, so binding the
> `AzureAd` section leaves `TenantId` null and the directory unusable. Set it explicitly in the
> `configure` callback.

## Entra app-registration permissions

Grant the app registration **application** (app-only) Graph permissions, with admin consent:

| Feature | Permission |
|---|---|
| Verify users, list directory-only users | `User.Read.All` |
| Delete users from Entra | `User.ReadWrite.All` |

Deleting also requires the app's service principal to hold a directory role allowed to delete users
(e.g. *User Administrator*) when the target is an administrator-role holder.
