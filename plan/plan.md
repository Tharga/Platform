# Plan: `GetSelectedTeamAsync` stops recursing (#195)

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-07** across the whole solution. Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid Six Labors build-time licence. Everything else current, so there is
      nothing to apply and nothing to bundle into this PR.

## 2. Version

- [x] **Patch.** No API is removed or changed; the new interface member carries a default implementation, so
      it is additive even for an implementer. CI computes the patch from the latest tag — nothing to edit.

---

## 3. The fix — one notification decision, taken outside the lock

- [x] `ResolveAsync` does the resolution under the semaphore and returns `(ITeam Team, bool Notify)`.
      `GetSelectedTeamAsync` raises the event after the lock is released.
- [x] `HasChanged` compares key **and** name. Name matters: `NeedsResolution`'s third clause exists to
      replace a renamed team, and suppressing that event would leave subscribers rendering the old name.
- [x] A refresh still notifies nobody — the page is being replaced, so there is no subscriber left to tell.
- [x] `AssignTeamAsync` folded into `ResolveAsync`; it had one call site and its two paths belong to the
      resolution, not to a shared helper.
- [x] `NeedsResolution` extracted from the three-clause condition at line 63. Same rule, readable.
- [x] `SetTeamCookieAsync` extracted — the refresh path hardcoded `'selected_team_id'` while
      `SetSelectedTeamAsync` used `Constants.SelectedTeamKeyCookie` for the same cookie.
- [x] `_semaphore.WaitAsync()` moved out of the `try`. It was inside, so a throw from the wait would have
      released a semaphore that was never acquired.

## 4. The cheap read

- [x] `bool TryGetSelectedTeam(out ITeam team)` on `ITeamStateService`, with a **default implementation
      returning false**. That is what keeps the addition non-breaking, and it is honest rather than a
      placeholder: `false` means "nothing known cheaply, ask the resolve", which is exactly true of an
      implementation that keeps no cache.
- [x] The real implementation reads the field with no lock — a reference read, and taking the semaphore
      would make the accessor neither cheap nor synchronous.

## 5. Documentation

- [x] XML on `GetSelectedTeamAsync`: it resolves rather than reads; it may hit local storage, create the
      first team, force a refresh and raise the event; do not call it from a handler.
- [x] XML on `SelectedTeamChangedEvent`: raised only on a real change, and the args carry the team.
- [x] XML on `SelectedTeamChangedEventArgs` — it had none, and it is the answer to the whole issue.
- [x] `Tharga.Team.Blazor/README.md` + `docs/articles/implementation-guide.md`, as a separate `docs:` commit.
      **Neither surface mentioned `ITeamStateService` at all**, so this is a new section rather than an edit —
      a public service consumers inject, whose misuse caused the outage, with no documentation on either
      surface. New "Reacting to the selected team" section in the guide (the correct handler pattern, and a
      cost column per member) plus a Components bullet in the README.

## 6. The sample stops teaching the bug

- [x] `NavMenu.razor` and `AccessPage.razor` take the team from `e.SelectedTeam` instead of discarding the
      args and re-resolving. **This is the durable half:** nothing in the toolkit demonstrated the correct
      pattern, which is how the obvious-but-wrong reading survived three releases. `AccessPage.LoadAsync`
      now takes the team as a parameter, so the resolve happens once, in `OnInitializedAsync`.

## 7. Tests

**9 tests in `SelectedTeamNotificationTests`. Whole suite green at 1741, warnings unchanged at 11.**

- [x] **The regression, at the shape it actually took**: an authenticated user with no team, a handler that
      calls the getter, asserting nothing is raised and nothing re-enters. Driven through
      `TeamStateService` itself — a test of the comparison alone would have passed on the broken code,
      because the defect was the *call* being unconditional, not the comparison being wrong.
- [x] The recursion test caps re-entry at 20, so on the broken behaviour it **fails with a count instead of
      taking the test host down with a stack overflow**. Confirmed: mutation 1 below failed in 7 ms.
- [x] Self-check that the no-team case really does re-resolve on every call, so a zero raise count means the
      raise was suppressed rather than the resolution skipped.
- [x] Event raised on null → team; on a rename; **not** on a second identical resolution.
- [x] The raise happens with the lock free — a handler re-enters and completes within the raise.
- [x] `TryGetSelectedTeam`: nothing before resolution, the team after, and no interop or event either way.
- [x] The default interface implementation reports nothing known.
- [x] **Mutation-checked, 3/3 caught, each by exactly the test written for it:**
      | Mutation | Caught by |
      |---|---|
      | raise unconditionally (`notify \|\| true`) | the recursion test **and** the no-op test |
      | compare key only, drop the name | `RenamingTheSelectedTeam_Raises` |
      | raise inside the lock | `TheEventIsRaisedWithTheResolutionLockReleased` — and it took 187 ms against 2 ms, which is the queueing the issue describes |

## 8. Close-out (only when the user confirms)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, warnings unchanged.
- [ ] Reply on #195 with what changed and what it means for their 21 components.
- [ ] `git rm -r plan`, final `fix:` commit, push, PR.

---

## Open

- **The no-team case still re-resolves on every call.** `NeedsResolution` is true whenever
  `_selectedTeam == null`, so a user with no team pays a local-storage round trip per call — now asserted by
  a test rather than left implicit. Harmless once the recursion is gone (one trip per legitimate reload
  instead of thousands), and caching "resolved to nothing" would need care to avoid missing a team created in
  another tab. Left alone deliberately; `TryGetSelectedTeam` gives the hot paths a way round it.
- **A duplicate `@inject ITeamManagementService` sits at `TeamComponent.razor:9` and `:12`** on master.
  Pre-existing, harmless, and unrelated — noted so it is not lost, not fixed here.

## Last session

**2026-08-07 (setup).** Branch cut from `origin/master`. Mechanism confirmed by reading the code rather than
trusting the report — the issue's ask #5 turned out to be already satisfied, which moved it from an API
change to a documentation one. Scope agreed with the user: fix the loop, add the cheap read, document, skip
the rename.

**2026-08-07.** §3, §4, §6, §7 done, and the XML half of §5. The fix is two changes to one method: the raise
is conditional, and it happens after the lock. Both were needed — the condition stops the loop being
created, the placement stops the loop that does occur from queueing on a held lock.

**Found while building: the sample was an exact reproduction.** `NavMenu.razor:61` and `AccessPage.razor:125`
both discarded the event args and called the getter from the handler — the same two lines the reporter
audited 21 of their own components for. That is the strongest evidence the API led people here rather than
this being 20 careless call sites, and it is why §6 counts as part of the fix rather than tidying.

**Next:** §8 close-out, once the user confirms the reporter's reproduction is quiet. Everything else is done:
fix, cheap read, sample, tests, both doc surfaces. Not yet pushed — waiting on approval.

**What still needs a human.** The unit tests pin the mechanism and the mutation run shows they would catch its
return, but the reported failure was an *authenticated user with no team* on a real circuit. Confirming that
end to end needs an interactive sign-in, so it is the user's check. The reporter offered to test a
pre-release against their reproduction, which is the strongest verification available here.
