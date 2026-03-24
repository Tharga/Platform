# Feature: optional-scope-registries

**Originating branch:** develop
**Date started:** 2026-03-23

## Goal

Make `IScopeRegistry` and `ITenantRoleRegistry` optional in Blazor components so that team management works without requiring scope/role registration.

## Scope

- `TeamComponent.razor` — resolve registries optionally; hide roles/scopes UI when not registered
- `ApiKeyView.razor` — resolve `IScopeRegistry` optionally; degrade scope info tooltip gracefully
- No changes to `TeamClaimsAuthenticationStateProvider` or `ApiKeyAuthenticationHandler` (already optional)

## Acceptance criteria

- [ ] `TeamComponent` renders without error when `AddThargaScopes()` and `AddThargaTenantRoles()` are not called
- [ ] `ApiKeyView` renders without error when `AddThargaScopes()` is not called
- [ ] Roles/scopes columns are hidden when registries are missing, regardless of `ShowMemberRoles`/`ShowScopeOverrides` options
- [ ] When registries ARE registered, existing behavior is preserved
- [ ] All tests pass

## Done condition

All acceptance criteria met, all tests pass, user confirms feature is complete.
