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

## Entra app-registration permissions

Grant the app registration **application** (app-only) Graph permissions, with admin consent:

| Feature | Permission |
|---|---|
| Verify users, list directory-only users | `User.Read.All` |
| Delete users from Entra | `User.ReadWrite.All` |

Deleting also requires the app's service principal to hold a directory role allowed to delete users
(e.g. *User Administrator*) when the target is an administrator-role holder.
