# User management & directory verification

The platform stores a user record for everyone who signs in. This article covers the administration
features built on top of that store: per-user activity tracking (last seen), verifying users against an
external directory (Microsoft Entra ID), listing directory-only users, and deleting users — from the
application and, on explicit opt-in, from the directory.

## Last seen

The user service stamps `LastSeen` on the user record whenever the user makes an authenticated request,
throttled to at most one write per interval (default **15 minutes**, per process). The stamp happens in
the user-resolve path, so it works whether or not a team is selected.

Tracking is **opt-in by entity shape**: declare the property on your user entity and the toolkit starts
writing it — leave it off and nothing is written.

```csharp
public record UserEntity : EntityBase, IUser
{
    public required string Key { get; init; }
    public required string Identity { get; init; }
    public required string EMail { get; init; }
    public string Name { get; init; }

    public DateTime? LastSeen { get; init; }     // opt-in: last authenticated activity
    public string DirectoryId { get; init; }     // opt-in: Entra object id (oid)
}
```

To change the stamp interval, override the virtual on your user service:

```csharp
public class UserService : UserServiceRepositoryBase<UserEntity>
{
    protected override TimeSpan? LastSeenStampInterval => TimeSpan.FromMinutes(5);
    // TimeSpan.Zero stamps every resolve; null disables stamping entirely.
}
```

The users admin list (`<UsersView />` → Users tab) shows a **Last seen** column. This is distinct from
the per-team-member `LastSeen`, which tracks when a member last selected that team.

## Directory linking (`DirectoryId`)

Directory operations resolve the user by the Entra object id (`oid`). The toolkit captures it two ways:

- **New users** — populate it in `CreateUserEntityAsync` from the sign-in claims:

  ```csharp
  protected override Task<UserEntity> CreateUserEntityAsync(ClaimsPrincipal principal, string identity)
      => Task.FromResult(new UserEntity
      {
          Key = ...,
          Identity = identity,
          EMail = principal.GetEmail(),
          Name = principal.GetDisplayName(),
          DirectoryId = principal.GetDirectoryId()   // the oid claim, either raw or .NET-mapped
      });
  ```

- **Existing users** — backfilled automatically from the `oid` claim on their next authenticated visit,
  and by directory verification: when a user without a stored id is matched by email, the found object
  id is persisted (relink).

## Registering Microsoft Entra ID as the directory

Install **Tharga.Team.Entra** and register it; configuration is read from the same `AzureAd` section
the platform sign-in uses:

```csharp
builder.Services.AddThargaEntraUserDirectory(builder.Configuration);
```

App-only Graph authentication needs a client secret (or any `Azure.Core.TokenCredential` — certificate,
managed identity):

```csharp
// dotnet user-secrets set "AzureAd:ClientSecret" "<secret>"
builder.Services.AddThargaEntraUserDirectory(builder.Configuration, o =>
{
    // o.Credential = new ManagedIdentityCredential();   // instead of a secret
});
```

Grant the app registration **application** permissions in Entra, with admin consent:

| Feature | Graph permission |
|---|---|
| Verify users, list directory-only users | `User.Read.All` |
| Delete users from Entra | `User.ReadWrite.All` |

When no directory service is registered, all directory features (verify actions, the Directory column,
the directory-only tab, the delete-from-directory opt-in) are hidden — the rest of user administration
still works.

## The `users:manage` scope

All user administration operations require the **`users:manage` system scope** (registered
automatically, like `teams:delete`). Grant it by mapping an app role:

```csharp
o.ConfigureSystemRoles = roles =>
{
    roles.Map("Developer", SystemUserScopes.Manage);
};
```

Authorization is enforced in the service layer (an authorization decorator over
`IUserManagementService`), so the same rule protects the Blazor UI and any consumer REST endpoint.

## Verifying users

- **Per user** — the Verify action on a row checks the directory and shows a badge:
  **Found** (exists, enabled), **Disabled** (exists, account disabled), **Not found** (the stored
  directory id no longer exists — the user was deleted in Entra), **Not linked** (no directory id and
  no email match).
- **Verify all** — sweeps every user, updating badges as results stream in.

Verification by a stored directory id deliberately does **not** fall back to email: a broken link is a
finding, not a lookup miss.

## Directory-only users

The **Directory only** tab on `<UsersView />` lists users that exist in the directory but have no local
user record — matched by directory id with an email fallback, so pre-existing local users without a
stored `oid` are not falsely reported. Nothing is fetched until you press **Load** (a tenant's
directory can be large); results stream in page by page.

## Deleting users

The Delete action (or `IUserManagementService.DeleteUserAsync`) always performs the **local** delete:

1. Removes the user from **every** team (any membership state).
2. Deletes the user record.
3. Writes audit entries.

Deleting from the directory is an **explicit opt-in** — a checkbox in the confirm dialog, off by
default — because it removes the account **organization-wide**, not just from your application. Entra
performs a soft delete: an administrator can restore the user for 30 days.

A directory failure never rolls back the local delete; it is reported on the result:

```csharp
var result = await userManagementService.DeleteUserAsync(userKey, deleteFromDirectory: true);
// result.RemovedTeamCount, result.DirectoryDeleted, result.DirectoryError
```

## Audit

User administration is audited under feature `user`: `verify` (with the outcome), `verify-all` (one
summary entry with the processed count), and `delete` (team count, whether the directory user was
deleted, any directory error). The all-team removal inside a delete is additionally audited under
feature `team` as `remove-member-all`. The directory-only listing is a read and is not audited.

## Custom directory providers

Entra is an implementation of the `IUserDirectoryService` abstraction. To back verification against a
different directory, implement the interface (verify a user, delete by directory id, enumerate users)
and register it:

```csharp
builder.AddThargaPlatform(o =>
{
    o.AddUserDirectoryService<MyLdapDirectoryService>();
});
```
