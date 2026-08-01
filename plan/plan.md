# Plan: Persist every enum by name

## Steps

- [x] **Update NuGet packages** — mandatory first step. `dotnet outdated` reports only
      `SixLabors.ImageSharp 3.1.12 -> 4.0.0`, which is **deliberately held**: 4.0+ requires a paid Six
      Labors build-time license. Not applied; nothing else is outdated.
- [x] **Audit every persisted enum in the repo.** Eight found across `IconEntity`, `TeamMemberBase`,
      `AuditEntryEntity`, `TeamEntityBase` and `ApiKeyEntity`. Six already store by name; two do not.
- [x] **Prove the current behaviour before changing it.** `AccessLevelDefaultTests` — 7 tests covering the
      `Owner` default, the missing-field case, the `State` contrast, and the stored representation of both
      a compliant and a non-compliant field.
- [x] **Establish whether a migration is needed.** It is not. A probe proved the driver reads `Int32` and
      `String` alike, so the attribute alone converts documents lazily on write. This replaced the planned
      custom serializer and one-time rewrite, and corrected the shared instruction, which had asserted the
      opposite.
- [x] **Check for query regressions.** No filter or sort touches either field; the only write path goes
      through the class-map serializer.
- [x] **Add `[BsonRepresentation(BsonType.String)]` to `TeamEntityBase.ConsentAccessLevel`.**
- [x] **Add `[BsonRepresentation(BsonType.String)]` to `ApiKeyEntity.AccessLevel`.**
- [x] **Flip `AConsentLevel_IsStoredByNumber` to assert the name** — now `AConsentLevel_IsStoredByName`,
      plus `AConsentLevelWrittenAsANumber_StillReads` proving the conversion strands nothing. The
      per-property `ApiKeyEntity` case is covered by the assembly sweep below rather than duplicated.
- [x] **Add a representation test covering all eight persisted enums.** `PersistedEnumRepresentationTests`
      in **both** test projects — each sweeps its own assembly, so neither needs a reference to the other.
      Each carries a second test asserting the sweep actually finds the properties it is meant to check, so
      a filter that quietly matches nothing cannot pass forever. Verified negatively: removing the
      attribute from `ApiKeyEntity` failed the test naming that exact property.
- [x] **Verify** — 1125 tests pass; sample compiles (`-t:Compile`, since the running app locks its `bin`
      and no test project builds it).
- [x] **Update the backlog** — the v4 sentinel entry now records that its storage prerequisite is done,
      and that the remaining constraint is timing (pre-conversion documents still hold numbers) rather than
      storage.
- [x] **Release note** — added to `Tharga.Team.MongoDB/README.md` as a "How enums are stored" section: the
      rule for new entities, that no migration is required, and the one consumer-visible caveat (external
      tooling querying these two fields numerically). Deliberately not tied to a version number — no bump,
      and the patch number is assigned by the release workflow.

## Notes

**The shared instruction was corrected mid-flight.** It originally said adding the representation attribute
alone was "worse than leaving it". A test disproved that: the driver tolerates both representations on
read. The rule now states the tested behaviour and keeps the real hazard, which is narrower — leftover
numeric values are correct only until someone renumbers the enum.

**Why this is the v4 prerequisite.** The `None = 0` sentinel shifts every ordinal. Values stored by name
are unaffected; values stored by number would silently come back meaning a different level. Doing this
first is what makes that change safe to make at all.

## Last session

Feature complete. Both fields converted, 1125 tests pass, sample compiles, backlog updated.

The change itself is two attributes; the durable part is the pair of assembly sweeps, which turn the new
shared instruction into something enforced rather than remembered.

Remaining: close-out (remove `plan/`) and PR.
