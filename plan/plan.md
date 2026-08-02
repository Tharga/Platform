# Plan: Complete the team-service registration

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Only `SixLabors.ImageSharp`
      3.1.12 → 4.0.0, held for the paid-licence reason. Everything else current.

## 2. Version

- [x] **`MAJOR_MINOR` stays `3.10`** — releases as `3.10.2`.

---

## 3. Infer the member type

- [ ] `TeamMemberTypeResolver` (or equivalent): walk the registered team service's base chain for a
      generic base with a type argument assignable to `ITeamMember`.
- [ ] Use it in `RegisterTeamService<TServiceBase, TUserService>()` — the two-argument overload — so it
      does what the three-argument one does whenever the type is discoverable.
- [ ] **An explicit `TMember` always wins.** The three-argument overload records a decision; inference is
      a fallback for when no decision was expressed, and must never override one.
- [ ] Tests: `TeamServiceRepositoryBase<TEntity, TMember>` resolves; a direct `TeamServiceBase` subclass
      does not; a deep chain still resolves.

## 4. `TryAdd` for every facet

- [ ] Switch the five `services.AddScoped(typeof(IFacet), …)` calls to `TryAdd` semantics.
- [ ] Test: a host that registered its own `ITeamOversightService` before calling still resolves its own.

**Registering facets a host never uses costs nothing** — they are scoped and simply never resolved.

## 5. Say so at startup when it is still incomplete

- [ ] `TeamServiceCompletenessCheck`, modelled on `UserServiceCompletenessCheck`: an `IHostedService`
      that resolves each facet and reports the ones missing, naming them.
- [ ] `ThrowOnIncompleteTeamService` on `ThargaBlazorOptions`, mirroring `ThrowOnIncompleteUserService`.
- [ ] **Logs by default, throws only on opt-in** — same trade the user-side check documents: turning a
      pre-existing gap into a boot failure after a routine upgrade is worse than making it unmissable.
- [ ] The message must name the interfaces *and* say how to fix it — pass `TMember` explicitly, or
      register the facet. A diagnostic that only states the problem sends the reader back to the source.

## 6. Tests

- [ ] **The assertion that would have caught this:** both overloads produce the same resolvable set.
- [ ] Each facet resolves to the *same instance* — they are facets of one object, and separate instances
      would be a subtler bug than none at all.
- [ ] `TryAdd` respects a host's own registration.
- [ ] The check names a missing facet, and is silent when nothing is missing.
- [ ] Mutation-check each guard by removing it.

## 7. Documentation

- [ ] `README.md` and `docs/` — that the two-argument overload now infers, and what to do when it cannot.
- [ ] **A release-note line naming the interfaces**, which the request asks for as the fallback and which
      3.10.0 never had.
- [ ] Separate `docs:` commit before close-out.

## 8. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings (baseline 8).
- [ ] **`MAJOR_MINOR` stays `3.10`.**
- [ ] Mark the request **Done** in `$DOC_ROOT/Tharga/Requests.md`, and add the cross-project follow-up
      so PlutusWave can drop their five hand-wired lines.
- [ ] Archive to `$DOC_ROOT/.../done/complete-team-service-registration.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-02 (setup).** Branch cut off `master` after #183 merged. Package check unchanged.

**The survey changed what this feature is.** The request reads as "the toolkit does not register its new
interfaces". It does — all five, inside `if (o._memberType != null)`, which only the **three-argument**
`RegisterTeamService` sets. PlutusWave used the two-argument overload, so it registered none of them. The
defect is one overload silently doing much less than its sibling, with nothing to indicate which you got.

**And that is why the repo never caught it:** the sample uses the three-argument overload, so every facet
resolves here and the broken path is never exercised. Same shape as the two field bugs in the suspend
feature — the tests covered the path the repo takes, not the one a host takes.

**Next:** confirm the plan, then §3.
