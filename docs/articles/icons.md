# Team & user icons

The platform gives teams and users real icons/avatars beyond email-based Gravatar. It's built on two
pluggable seams — **storage** (where bytes live) and **sourcing** (where a displayed image comes from) —
each with a working built-in default, so it works out of the box and can be swapped per site.

## The two seams

### `IIconStore` — storage
Where icon bytes are saved: `SaveAsync(kind, ownerKey, bytes, contentType) → reference`, `LoadAsync`,
`DeleteAsync`. The reference (an id/URL) is what's persisted on the team/user record.

- **Built-in default: `MongoIconStore`** (in `Tharga.Team.MongoDB`) — registered automatically by
  `AddThargaTeamRepository`, no extra package. Bytes live in their own `Icon` collection (keyed by
  reference), never inlined into the hot `Team`/`User` documents.
- **Custom:** `o.AddIconStore<T>()` for Azure Blob, S3, an existing DMS, etc. A custom store wins over the
  built-in one.

### `IIconSource` — sourcing
Where a displayed image comes from. An `IIconResolver` runs the registered sources in order and returns
the first non-null image; when none match, the avatar renders **initials**. Built-in sources, in order:

1. **`StoredIconSource`** — an explicitly-set (uploaded) icon. Registered first, so a stored icon takes
   precedence.
2. **consumer sources** — `o.AddIconSource<T>()`, so a host can supply images from its own system.
3. **`GravatarIconSource`** — for users with an email (when enabled).
4. **`DefaultIconSource`** — a configured generic default image, if any.

Resolution: **stored icon → custom sources → Gravatar → default image → initials.**

## Options

### `IconOptions` (upload limits — startup)
| Property | Default | Meaning |
|---|---|---|
| `MaxBytes` | 256 KB | Max **stored** size, validated after processing. |
| `MaxUploadBytes` | 10 MB | Max **original upload** accepted for reading, before downscaling. |
| `MaxDimension` | 256 | Max width/height (px) an image processor downscales to. 0 disables. |
| `AllowedContentTypes` | png, jpeg, gif, webp, svg | Accepted image types. |

Configure via `o.Icon` on `AddThargaTeam`.

### `IconSettings` (display/behavior — runtime-adjustable)
Registered as a singleton, so a host can change these at runtime (the sample has a page that does):

| Property | Default | Meaning |
|---|---|---|
| `GravatarEnabled` | true | Use Gravatar as a fallback for users without an uploaded icon. |
| `GravatarStyle` | `identicon` | Gravatar default-image style (`identicon`, `monsterid`, `retro`, `robohash`, `mp`, …). |
| `DefaultUserIconUrl` | null | A generic default image for users (after/instead of Gravatar). |
| `AllowUserUpload` | true | Whether users can upload their own icon. |
| `AllowAdminUpload` | true | Whether admins (`users:manage`) can upload an icon for a user. |

Configure initial values via `o.IconSettings`.

## Team icons

`ITeam.Icon` already exists on `TeamEntityBase`, so team icons need no entity change. A `team:manage`
holder sets a team icon from the team-management component (`TeamComponent` → the **Actions** button):
upload a file or point at an image URL (downloaded server-side). The operations —
`ITeamService.SetTeamIconAsync` / `ClearTeamIconAsync` — are gated by `team:manage` and audited
(`icon-set` / `icon-clear`); replacing or clearing deletes the previous blob. Rendered by `<TeamAvatar>`
(teams list, card title, team selector) with an initials fallback.

## User icons

Opt in by declaring `Icon` on your user entity (the same shape-based opt-in as `DirectoryId`/`LastSeen`):

```csharp
public record UserEntity : EntityBase, IUser
{
    public required string Key { get; init; }
    public required string Identity { get; init; }
    public required string EMail { get; init; }
    public string Name { get; init; }
    public string Icon { get; init; }   // opt in to user icons
}
```

- **Self-service:** the profile page's **Change picture** action (`IUserService.SetOwnIconAsync` /
  `ClearOwnIconAsync`) lets a user upload their own icon as an alternative to Gravatar (gated by
  `IconSettings.AllowUserUpload`). The top-right profile avatar refreshes live.
- **Administrative:** on the users admin list, **Set icon** (`IUserService.SetUserIconAsync`, gated by
  `users:manage` + `IconSettings.AllowAdminUpload`) lets an admin set a user's icon.

Rendered by `<UserAvatar>` everywhere a user is shown (top-right menu, users list, member grids), which
resolves stored → Gravatar → initials.

## Serving endpoint

Stored icons are served at `GET /_tharga/icon/{reference}` (mapped by `UseThargaTeam`) to
authenticated callers, with an immutable cache header (the reference changes when the icon changes).

## Automatic downscaling — `Tharga.Team.Images`

Uploads larger than `IconOptions.MaxBytes` would be rejected. Add the optional **`Tharga.Team.Images`**
package to instead **downscale** oversized images to fit `MaxDimension` (256 px), aspect preserved,
re-encoded as PNG:

```csharp
builder.Services.AddThargaImageProcessing();
```

It registers an `IIconProcessor` (ImageSharp) that the built-in store runs before validating/storing.
Images already within bounds and formats it can't decode (e.g. SVG) pass through unchanged. Bring your
own by registering a custom `IIconProcessor`.

## Quick start

```csharp
// Built-in Mongo store + Gravatar are automatic. Add downscaling and map admin/user upload to a role:
builder.Services.AddThargaImageProcessing();                 // optional: auto-downscale

builder.AddThargaTeam(o =>
{
    o.Icon.MaxDimension = 256;                               // resize target
    o.IconSettings.GravatarStyle = "identicon";              // or disable: o.IconSettings.GravatarEnabled = false
    o.ConfigureSystemRoles = roles => roles.Map("Developer", SystemUserScopes.Manage); // admin upload
});
```

Team icons work with no entity change; add `Icon` to your user entity for user icons.
