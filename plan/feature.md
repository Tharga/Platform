# Feature: Per-team tenant-role visibility hook (GitHub #113)

## Goal
Let a consumer hide specific tenant roles from `TeamComponent`'s per-member role editor on a
per-team basis, without pruning existing assignments. Driven by Eplicta/FortDocs feature-gated
roles (e.g. `CaseAdministrator`, `ArchiveViewer`) tied to per-team feature toggles.

## Scope
- New injectable `ITenantRoleVisibilityProvider` in `Tharga.Team`:
  ```csharp
  Task<bool> IsRoleVisibleAsync(string teamKey, string roleName, CancellationToken ct = default);
  ```
- Default identity implementation (all roles visible), registered via `TryAdd` so the change is
  non-breaking and consumers override by registering their own.
- `TeamComponent` computes a per-team visible-role list (filtering `ITenantRoleRegistry.All`
  through the provider during team load) and passes it to each row's `RoleEditor`.
- `RoleEditor` preserves assigned-but-hidden roles on change (union hidden-selected back into the
  emitted set) — hidden ≠ removed.
- Tests + docs.

## Out of scope
- Pruning/removing hidden role assignments (explicitly must be preserved).
- Any change to scope resolution (`GetScopesForRoles`) — hidden is a UI-editor concern only; a
  hidden-but-assigned role still grants its scopes at runtime.
- Filtering roles anywhere other than the `TeamComponent` role editor.

## Acceptance criteria
- [ ] `ITenantRoleVisibilityProvider` defined in `Tharga.Team` with the issue's signature.
- [ ] Default implementation returns `true` for all roles; registered via `TryAdd` (overridable).
- [ ] With no custom provider registered, `TeamComponent` behaves exactly as before (all roles shown).
- [ ] With a custom provider, roles it hides do not appear in the role editor for that team, but
      do appear for teams where it allows them.
- [ ] A role already assigned to a member but hidden for that team stays assigned after editing
      other roles (never pruned).
- [ ] Tests cover: default visibility, per-team filtering, hidden-assigned preservation,
      overridable registration.
- [ ] README/docs updated.

## Done condition
All acceptance criteria met, full test suite green, docs updated, PR opened develop → master.
