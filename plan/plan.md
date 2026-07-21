# Plan: Audit metadata (#129)

Branch `feature/audit-metadata` off `master` (at `32be738`). See `feature.md` for goal, background and
acceptance criteria.

## Steps

- [x] **1. NuGet updates (up front, whole solution)** — no-op. `dotnet outdated` reports "No outdated
  dependencies were detected" (checked 2026-07-20, after 3.2.0). Re-check at close-out per the workflow.

- [x] **2. Thread metadata through the entry builder** — done. Suite **632 green** (+5).
  `AuditHelper.BuildEntry` takes an optional `IReadOnlyDictionary<string, string> metadata`;
  `AuditingTeamServiceDecorator.Log` passes it through. Two normalizations chosen while writing:
  an **empty** dictionary stores as `null` (so renderers and exporters handle one "nothing here"
  representation, not two), and the entry **copies** the dictionary rather than aliasing it (a decorator
  reusing its builder after logging would otherwise retroactively rewrite a recorded entry). Both are
  asserted. Added `AuditMetadataKeys` — the key vocabulary as constants, since these become part of the
  audit record's public shape.

  Original step text:
  `AuditHelper.BuildEntry` gains an optional `IReadOnlyDictionary<string, string> metadata = null`;
  `AuditingTeamServiceDecorator.Log` and the API-key decorator's equivalent pass it through. No
  behaviour change yet — every existing call site keeps working, metadata stays null.
  Tests: an entry built without metadata is unchanged; with metadata, it round-trips.

- [x] **3. Populate metadata on built-in operations** — done. Suite **644 green** (+12).
  Three best-effort read helpers (`TryGetTeamAsync<TMember>`, `TryFindTeamAsync`, `TryGetMemberAsync`)
  that swallow failures — audit detail must never fail the operation it describes. A failed read omits
  its key rather than recording a misleading null; asserted by a test.
  **Judgement call to confirm:** `delete` also reads the team name first, which wasn't in the agreed
  four. The name is unrecoverable once the team is gone, so it seemed the clearest case of a read
  earning its cost — easy to drop if you disagree.
  **Limitation:** `SetTeamConsentAsync` isn't generic, so there's no exact team read available; it scans
  the caller's own teams. A non-member changing consent through consent-granted access finds nothing and
  the `.old` key is omitted. Exact reads used everywhere `TMember` is in scope.
  Sentinels: consent cleared records `"none"`, a cleared display-name override records `""` — both stay
  distinguishable from a failed read (key absent).
  **Snag:** NSubstitute can't proxy `ITeam<TestMember>` while `TestMember` is internal — added
  `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />` to the test project.

  Original step text:
  Depends on the answer to open question 2. Baseline (old value cheap or already in hand):
  - `CreateTeamAsync` — team name (key is already on the entry).
  - `RenameTeamAsync` — old + new name. **Requires a read before the mutation.**
  - `SetTeamConsentAsync` — old + new access level and roles. Requires a read.
  - `SetMemberRoleAsync` / `SetMemberTenantRolesAsync` / `SetMemberScopeOverridesAsync` /
    `SetMemberNameAsync` — member key + before/after. Requires a read.
  - `AddMemberAsync` / `RemoveMemberAsync` — member identity; no before/after needed.
  - `SetTeamCustomRolesAsync` — role names added/removed.
  Use a shared constant set for key names (`team.name`, `team.name.old`, …) rather than string
  literals scattered across call sites.
  Tests: one per operation asserting the expected keys land on the entry.

- [x] **4. `IAuditEnricher` hook** — done. Suite **652 green** (+8).
  `void Enrich(AuditEntry entry, IDictionary<string,string> metadata)` — synchronous because
  `CompositeAuditLogger.Log` is, singleton-resolved because that logger is a singleton (read request
  state via `IHttpContextAccessor`, as the rest of the pipeline does). Applied **after** `ShouldLog`, so
  filtered entries are never enriched (tested). **Add-only, toolkit wins, first enricher wins** on a key
  conflict — each enricher gets a fresh bag, merged into a copy that starts from the entry's own
  metadata. A throwing enricher is caught, logged via an optional `ILogger<CompositeAuditLogger>`, and
  skipped; other enrichers still apply and the entry is still logged (tested — this is the core safety
  rule). Registration: `services.AddThargaAuditEnricher<T>()`, works for Blazor and pure-service
  consumers alike. Constructor params are optional so the existing direct-construction tests are
  unaffected.

  Original step text:
  - `IAuditEnricher` with `void Enrich(AuditEntry entry)` (or a mutable metadata bag — decide when
    writing; `AuditEntry` is an immutable record, so the enricher likely returns additions rather than
    mutating).
  - Invoked in `CompositeAuditLogger.Log`, before `ShouldLog` dispatch — the single funnel all entries
    pass through.
  - **Must not be able to break the audited operation.** Wrap each enricher call; log and continue on
    failure. An audit sink taking down a team rename would be a worse bug than the missing metadata.
  - Decide: may an enricher overwrite toolkit-set keys, or only add? Default to add-only, last-writer
    loses, and document it.
  - Registration mirrors `AddClaimsEnricher<T>()`.
  - Tests: enricher values reach the entry; a throwing enricher doesn't propagate; toolkit keys survive.

- [ ] **5. `LoggerAuditLogger` — stop discarding metadata**
  Include it in the structured output. Note this is the **default** `StorageMode`, so this is where most
  consumers would otherwise silently lose everything this feature adds.

- [ ] **6. `AuditLogView` — render metadata**
  Depends on the answer to open question 1. Whichever shape, entries with `Metadata == null` or empty
  must render exactly as today.

- [ ] **7. CSV export**
  Add a metadata column to the hard-coded header/row builder (`AuditLogView.razor.cs:379-380`).
  Representation decision: a single JSON-encoded column is the obvious option; must survive the existing
  `Escape` treatment. Verify JSON export is unaffected.

- [ ] **8. Revive `ConsentAuditWiringTests`**
  Take `730f188` from `origin/investigate/consent-audit`, add the missing
  `SetTeamCustomRolesInternalAsync` override to `FakeTeamService`, confirm it passes. Extend it to
  assert the new consent metadata while it's in hand.

- [ ] **9. Build + full test suite** — `dotnet build -c Release`, `dotnet test -c Release`.

- [ ] **10. Docs** — `implementation-guide.md` (audit section: what metadata the toolkit records, the
  enricher hook, the Logger-mode caveat) and `Tharga.Team.Blazor/README.md`. Separate `docs:` commit.

- [ ] **11. Push branch, hand to user for testing** — do **not** open the PR yet.

- [ ] **12. Close-out** — only after the user confirms. Re-run `dotnet outdated`, archive `feature.md`
  to the Plan directory `done/`, `git rm -r plan`, final commit `feat: audit-metadata complete`, push,
  open PR.

## Decisions taken

- **Enricher over method parameters.** Metadata arguments on `ITeamService` would pollute the domain
  contract for every caller, including those with auditing off.
- **Hook in `CompositeAuditLogger`**, not in each decorator — one funnel, so enrichers apply uniformly
  to team, API-key and any future audited surface.
- **Keep enumeration unaudited** — decided during the 3.2.0 work, unchanged here.

## Decisions confirmed with the user (2026-07-20)

1. **Grid: expandable detail row.** Click a row to expand in place and show key/value pairs as a small
   table. Native to Radzen's `DataGrid`, keeps the grid compact, scales to any number of keys, and needs
   no dialog to dismiss. Rows with no metadata must expand to nothing (or not offer expansion) rather
   than showing an empty panel.
2. **Before/after where it earns the read.** Capture old + new for **rename**, **consent level**,
   **member access level** and **member display name** — the changes where the old value is needed to
   interpret the entry. **Skip** the extra read for invite/remove, where the member identity is the
   whole story. Custom-role CRUD records the role names and scopes it was given, no read.

## Last session

**2026-07-20.** Branch created off `master` (`32be738`); `dotnet outdated` clean so step 1 was a no-op.
Plan written, awaiting confirmation and answers to the two open questions. No code written yet.
