# Feature: Icon registration on the granular path, and signals for unmet prerequisites

Closes GitHub [#157](https://github.com/Tharga/Team/issues/157) (Critical) and
[#160](https://github.com/Tharga/Team/issues/160), both filed by Eplicta/FortDocs. Grouped because they
are the same subsystem, the same reporter, and #160's checklist is what a host hits *after* #157 lets them
get that far.

## The common thread

Every failure in both issues is **silence**. An app builds, boots clean, passes health checks and the full
test suite, and then does the wrong thing at render time or writes nothing to the database. As #160 puts
it: each is trivial once you know it; what made them expensive is that they fail as *nothing happening*.

That is also why this feature introduces bUnit — see below.

## 1. #157 — the icon chain is facade-only (Critical)

`LoginDisplay` sits in the layout and therefore renders on every page, and since 3.4 it hard-injects
`AvatarChangeNotifier`. `TeamAvatar` / `UserAvatar` inject `IIconResolver`; `UserProfileView` and
`UsersListView` inject `IconSettings`. The whole chain is registered **only** inside the facade
(`ThargaTeamRegistration.cs:170-191`). `AddThargaTeamBlazor` registers none of it.

So a host following the documented "Advanced: Step-by-step setup" gets
`InvalidOperationException: Cannot provide a value for property 'AvatarChangeNotifier'` on every render,
and the circuit dies with it — unusable, not degraded. `ValidateOnBuild` cannot catch it: Blazor resolves
`@inject` **properties at render time**, not through the constructor graph the validator walks.

**The serving half is split the same way** — the icon endpoint is mapped only by `UseThargaTeam`, so even
once the chain is registered, a stored icon could not be served back on the granular path.

**Fix — move the registration down, not sideways.** The facade already *calls* `AddThargaTeamBlazor`
(`ThargaTeamRegistration.cs:67`), so moving the chain into `AddThargaTeamBlazor` serves both paths with no
duplication:

- Icon chain registered unconditionally by `AddThargaTeamBlazor`. Unconditional rather than opt-in because
  `LoginDisplay` is always-on — an opt-in a host can forget reproduces the same crash.
- `IconSettings` added to `ThargaBlazorOptions`; the facade forwards its own `ThargaTeamOptions.IconSettings`
  through the call it already makes.
- A public `UseThargaTeamBlazor(app)` maps the icon endpoint, mirroring the existing
  `AddThargaAuth` / `UseThargaAuth` pair. `UseThargaTeam` calls it, so the facade is unchanged for its users.

## 2. #160 — unmet prerequisites report themselves

1. **`SetIconAsync` silently no-ops** when the user entity does not declare `Icon`. The dialog closes, no
   error, avatar unchanged, nothing logged. The opt-in itself is right; reporting success for a discarded
   write is not. **The strongest argument is internal inconsistency**: a missing `IIconStore` throws with a
   message naming the fix, while a missing entity property — same feature, same category of unmet
   prerequisite — does nothing. Gate the upload UI on capability so it is not offered when it cannot work,
   and log once at startup.
2. **A skipped reference write leaks the blob.** The bytes are stored *before* the reference write, so a
   skipped write orphans a row in the `Icon` collection.
3. **The upload dialog's "Large images are downscaled automatically" is unconditional** and false under the
   default `NoOpIconProcessor` — anything over `MaxBytes` is rejected instead. Condition the text on a real
   processor being registered.
4. **Docs**: the granular path is not covered in `icons.md` at all, and forwarding the optional
   `IIconStore` parameter in `TeamServiceRepositoryBase` / `UserServiceRepositoryBase` subclasses is
   undocumented — its error message currently points away from the cause.

## 3. bUnit, introduced here because this is what needs it

#157 is a render-time DI failure. Nothing in the current suite can catch that class of bug, and this
feature is the third instance in a week — the others being an unresolved Razor component tag that rendered
as an empty element, and two guard-order defects found by decompiling a shipped assembly.

**Scope deliberately narrow:** render smoke tests for the always-on components, under **both** registration
paths. A test that renders `LoginDisplay` after `AddThargaTeamBlazor` alone is exactly the test that would
have caught #157, and it is the acceptance criterion for the fix.

**Timeboxed.** Radzen needs JSInterop and dialog/notification stubbing, and that setup is the risk in this
feature. If it turns ugly, the #157 fix ships first on its own and the tests follow — a Critical fix must
not wait on test plumbing.

## Out of scope

- **Reprocessing already-stored icons.** Nothing here changes stored output.
- **The squaring request** (`Requests.md` → "Uploaded icons should be squared") — separate, has open design
  calls, and is Nice rather than Important.
- **A general bUnit suite.** Only the render smoke tests this feature's verification needs.

## Acceptance criteria

- [ ] A host using `AddThargaAuth` + `AddThargaTeamBlazor` + `AddThargaTeamRepository` renders
      `LoginDisplay` without throwing — asserted by a bUnit test, not by inspection.
- [ ] The icon endpoint is mappable on the granular path.
- [ ] The facade path is unchanged for its users.
- [ ] Setting a user icon on an entity without `Icon` either cannot be attempted from the UI, or fails
      loudly; it never reports success.
- [ ] No orphan blob is written when the reference write is skipped.
- [ ] The upload dialog does not promise downscaling that will not happen.
- [ ] `icons.md` covers the granular path and the `IIconStore` forwarding requirement.
- [ ] Full test suite passes.

## Version

**Additive** — new public `UseThargaTeamBlazor` and `ThargaBlazorOptions.IconSettings`, plus registrations
moved from one entry point to another that the first already calls. No consumer loses anything.
`MAJOR_MINOR` moves **3.7 → 3.8**, since this is public API growth and the version line is hand-maintained.

One behaviour change worth a release note: hosts on the granular path who hand-registered the icon chain as
a workaround (Eplicta does) will now have it registered twice. The registrations use `TryAdd` where
duplication would matter, but the workaround should be deleted on upgrade — call that out explicitly.
