# Feature: `GetSelectedTeamAsync` stops recursing (issue #195)

**Type:** bug fix · **Target release:** patch on the current line · **Issue:** [#195](https://github.com/Tharga/Team/issues/195)

## Goal

`ITeamStateService.GetSelectedTeamAsync()` raises `SelectedTeamChangedEvent` unconditionally, so the
idiomatic Blazor pattern — subscribe to the change event, reload when it fires, and read the current team
to do the reload — recurses without bound. Stop that, and make the cheap read the handler actually wants
available.

## The mechanism, confirmed in code

`TeamStateService.cs:63` re-enters resolution whenever `_selectedTeam == null`, and `AssignTeamAsync`
(line 117) raises the event even when the resolution changed nothing:

```
authenticated user, no team
  -> _selectedTeam == null, so line 63 is true on every call
  -> TeamSelectionResolver.Resolve returns null (nothing to pick), AutoCreateFirstTeam off
  -> AssignTeamAsync(null, refresh: false) raises SelectedTeamChangedEvent with null
  -> handler reloads, calls GetSelectedTeamAsync, back to the top
```

Where the selection resolves to a value the loop terminates after a round or two, which is why this read
as "slightly slow" for three releases and only became fatal once self-service registration made
"authenticated with no team" a common state.

Two things the issue guessed at, now settled:

- **The event is raised while the semaphore is held** (line 117 sits inside the `try`), so the re-entrant
  calls queue on a lock the raiser still owns. That is why the circuit saturates rather than merely spinning.
- **`SelectedTeamChangedEventArgs` already carries the whole `ITeam`.** The issue's fifth ask ("consider
  putting the team key on the args") is already true, so a handler never needed the getter at all. That
  makes it a documentation answer, not an API change.

## Scope — agreed with the user 2026-08-07

In scope:

1. **Raise the event only on a real change** — key *or* name, since a rename leaves subscribers rendering a
   stale name. This alone terminates every loop, with no consumer changing a line.
2. **Raise it after releasing the semaphore**, so a handler calling back in is not queueing behind the
   raiser's own lock.
3. **`bool TryGetSelectedTeam(out ITeam team)`** — the side-effect-free read of the already-resolved
   selection, for the callers that just want the known value.
4. **Documentation** — the hazard on the getter, and that a handler should read `e.SelectedTeam`.
5. **The sample stops demonstrating the bug.** `NavMenu.razor:61` and `AccessPage.razor:125` are exact
   reproductions: both handlers discard the event args and call the getter to re-resolve.

Out of scope:

- **Renaming the getter** to `ResolveSelectedTeamAsync`. Considered and declined: an `[Obsolete]` forwarder
  would warn every consumer on upgrade for a hazard that, once the raise is conditional, no longer bites.
- **Moving the toolkit's own 13 call sites onto `TryGetSelectedTeam`.** The async getter *resolves* —
  auto-selection, first-team creation, the claims refresh. Swapping a resolving call for a cached read
  would break auto-selection. The new accessor is for callers that hold a selection already.

## Acceptance criteria

- [ ] A handler that calls `GetSelectedTeamAsync` from inside `SelectedTeamChangedEvent` terminates, for an
      authenticated user with **no team** — the fatal case.
- [ ] The event still fires when the selection genuinely changes, including on a rename.
- [ ] A second call that resolves to the same team raises nothing.
- [ ] The event is observed to be raised with the semaphore free.
- [ ] `TryGetSelectedTeam` performs no interop, raises no event and resolves nothing.
- [ ] Adding it breaks no existing implementer — it carries a default implementation reporting "nothing
      known", which is the safe answer because callers must treat `false` as "ask the resolve".
- [ ] Full suite green, warning count unchanged at 11.
- [ ] README and implementation guide updated; separate `docs:` commit.

## Done condition

The user confirms the reporter's reproduction no longer loops. Package consumers upgrade and need change
nothing.
