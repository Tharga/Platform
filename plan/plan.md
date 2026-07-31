# Plan: Icon registration on the granular path, and signals for unmet prerequisites

Feature scope in `feature.md`. Tests run before each commit; `plan.md` is updated as each step lands.

Ordered so the Critical fix (#157) can ship even if the bUnit setup in step 2 proves expensive.

## Steps

- [x] **1. NuGet package check (feature-start requirement)**
      *Done — `dotnet outdated` across the whole solution reports only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, deliberately held (4.0+ needs a paid Six Labors build-time licence). Nothing to
      apply.*

- [x] **2. bUnit spike — timeboxed, and the decision point for this feature**
      Add `bunit` to `Tharga.Team.Blazor.Tests` and get **one** test rendering `LoginDisplay` against a
      service collection built by `AddThargaTeamBlazor` alone. It must fail with the reported
      `InvalidOperationException: Cannot provide a value for property 'AvatarChangeNotifier'` — reproducing
      #157 before fixing it.
      Expect to stub JSInterop and Radzen's dialog/notification services. **If that setup turns ugly,
      stop and say so**: steps 3–4 ship the fix without it and bUnit becomes its own feature. A Critical
      fix must not wait on test plumbing.

- [x] **3. Move the icon chain into `AddThargaTeamBlazor`** (#157, registration half)
      Move the block at `ThargaTeamRegistration.cs:170-191` into `ThargaBlazorRegistration`. Add
      `IconSettings` to `ThargaBlazorOptions` and have the facade forward `ThargaTeamOptions.IconSettings`
      through the `AddThargaTeamBlazor` call it already makes at line 67. Use `TryAdd*` where a
      hand-registered workaround would otherwise double up.
      Verify the facade path is unchanged — `AddThargaTeamTests` already asserts its registration set.

- [x] **4. Public `UseThargaTeamBlazor`** (#157, serving half)
      Maps `{IconRoute.Base}/{reference}`, mirroring the `AddThargaAuth` / `UseThargaAuth` pair.
      `UseThargaTeam` calls it rather than mapping the endpoint itself.

- [x] **5. Verify build + full suite, commit** — *Done: 1059 passed / 0 failed; build clean. #157 is
      closed and shippable on its own from here.*

### Notes from steps 2-5

- **The bUnit spike went the good way** — no JSInterop or Radzen stubbing needed; loose JSInterop mode
  covers Radzen's calls. Setup is three lines. The four API traps cost more than the wiring did:
  `RenderComponent<T>` → `Render<T>`; `TestContext` is ambiguous with xUnit v3's and obsolete anyway
  (`BunitContext`); and **`Services.AddAuthorization()` silently binds to ASP.NET's overload**, which
  registers `IAuthorizationService` but no `AuthenticationStateProvider` — the correct call is
  `this.AddAuthorization().SetNotAuthorized()`. bUnit's own exception text recommends the broken one.
- **The test found the second missing dependency before a consumer did.** With the icon chain fixed,
  rendering next failed on `IUserService` — which is correct, being host-supplied via
  `RegisterTeamService`. Stubbed in the test, with a comment drawing the line: stub what a host supplies,
  never what the library's own always-on components need, or the test hides the defect it guards.
- **`IconSettings` is forwarded by reference, not copied.** It is registered as a singleton and mutated at
  runtime, so identity is the contract; a copy would strand a host's later changes.
- **Warning check:** the build's CS1574/CS1587 in `TeamMenuText.cs:5` pre-date this branch (verified
  against master); the one warning this work introduced (obsolete `TestContext`) is fixed.

- [x] **6. Capability gating for user icons** (#160 items 1 and 3)
      *Done — new public `IconCapability` in `Tharga.Team`: `CanPersistUserIcon(Type)` (entity declares
      `Icon`) and `CanProcessImages(IIconProcessor)` (not the no-op). 7 tests.
      Both upload dialogs now inject `IIconProcessor` and say **"Images larger than N MB are rejected"**
      when nothing can downscale, instead of promising downscaling unconditionally.*
      Two decisions, both pure and unit-testable, both about not offering what cannot work:
      - Whether the upload UI is offered at all — needs the entity to declare `Icon` *and* a store.
      - Whether the dialog may claim automatic downscaling — needs a non-no-op `IIconProcessor`.
      Plus a single startup warning when uploads are enabled but the entity cannot persist a reference.

- [x] **7. Stop the orphan blob** (#160 item 2)
      *Done, and it closed #160 item 1 in the same move.* The guard is the **same check** for both
      defects, so it went in one place: `RequireIconPersistence(user)` throws `NotSupportedException`
      naming the entity type and the fix, **before** any bytes are written. Applied to both entry points
      — `SetOwnIconAsync` (self-upload) and `SetUserIconAsync` (admin upload); the second was easy to
      miss reading the issue, which only quotes the repository method.
      Preferred over compensating after the fact, per the plan: not writing beats deleting. It also
      matches `RequireIconStore`, which already names its own unmet prerequisite — the internal
      inconsistency the issue called its strongest argument.
      `UserServiceBase.SetUserIconAsync` stores bytes then writes the reference. Either check the
      capability before storing, or delete the blob when the reference write is skipped. Prefer the former
      — not writing is cheaper than compensating.

- [x] **8. Verify build + full suite, commit** — *Done: 1065 passed / 0 failed; build clean.*

- [x] **9. Documentation** (#160 item 4)
      *Done — `icons.md` gained a granular-path section (no extra registration needed; just
      `UseThargaTeamBlazor()` for the endpoint) with an upgrade note telling workaround-holders to delete
      theirs, the `IIconStore` forwarding trap with code showing accept-and-forward, a callout that
      uploads are now refused rather than discarded, and a note that the dialog wording varies by
      processor. **Also reworded both `RequireIconStore` messages** — they named only registration, which
      is misleading in the common case where the store IS registered and the subclass did not forward the
      constructor parameter. That was the issue's own complaint about the message.*
      `docs/articles/icons.md`: a granular-path section (registration + `UseThargaTeamBlazor`), and the
      `IIconStore` forwarding requirement for `TeamServiceRepositoryBase` / `UserServiceRepositoryBase`
      subclasses. Reword `RequireIconStore()`'s message to mention the constructor parameter, since it
      currently points away from the cause. Separate `docs:` commit.

- [x] **10. Version line 3.7 → 3.8** — *Done.*
      New public API (`UseThargaTeamBlazor`, `ThargaBlazorOptions.IconSettings`). Must land in this PR —
      the constant is hand-maintained and nothing in CI bumps it.

- [~] **11. Push and hand over for testing**
      Do **not** open the PR — the close-out commit must be last.

## Remaining (close-out, only on the user's confirmation)

Re-run `dotnet outdated`; close #157 and #160 with a summary; add the `## Follow-up` entry telling Eplicta
to delete their hand-rolled registration block and locally-mapped endpoint; archive `feature.md`;
`git rm -r plan`; commit `fix: icon-registration-and-signals complete`; push; open the PR.

## Notes

- **Branched off master after #165 merged**, so `AccessGuardState` and the audit fixes are in the base.
- **Eplicta has a workaround in production** — a hand-rolled registration block after
  `AddThargaTeamRepository` plus a locally-mapped `/_tharga/icon/{reference}`. The follow-up entry must
  tell them to delete both, or they will carry duplicate registrations.
- **Nothing has published since 3.7.0.** Two release runs were queued and unapproved at branch time, so
  the version this ships as depends on what is approved first.

## Last session

Branch created off master. Plan ordered so #157 ships independently of the bUnit spike.
