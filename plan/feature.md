# Feature: Persist every enum by name

## Goal

Bring this repo in line with the new shared instruction — *store enums by name, never by ordinal* — and
leave behind the tests that keep it that way.

## Why now

Found while investigating `default(AccessLevel) == Owner`. The backlog claimed that entry could not be
fixed without a migration because "the numeric values are persisted in every team document". That turned
out to be true for two of three sites and false for the third, because the three do not agree:

| Entity | Property | Stored as |
|---|---|---|
| `IconEntity` | `Kind` | String |
| `TeamMemberBase` | `State` | String |
| `TeamMemberBase` | `AccessLevel` | String |
| `AuditEntryEntity` | `EventType`, `CallerType`, `ScopeResult` | String |
| `TeamEntityBase` | `ConsentAccessLevel` | **Int32** |
| `ApiKeyEntity` | `AccessLevel` | **Int32** |

Six of eight already comply, so the two outliers read as oversights rather than decisions — neither
declares a representation, and the driver's default is `Int32`, which means *omitting the attribute is
choosing the ordinal* without appearing to choose anything.

This also unblocks the v4 work. A `None = 0` sentinel renumbers every member, which is safe for values
stored by name and silently re-grades values stored by number. Converting now is the prerequisite; leaving
it is what would make the v4 fix impossible to do safely.

## Scope

- `TeamEntityBase.ConsentAccessLevel` → `[BsonRepresentation(BsonType.String)]`
- `ApiKeyEntity.AccessLevel` → `[BsonRepresentation(BsonType.String)]`
- Tests pinning the representation of every persisted enum, so a dropped attribute fails loudly

## Not in scope

- **The `None` sentinel / `default(AccessLevel) == Owner`.** Breaking; deferred to v4 and recorded in the
  backlog. This feature is the prerequisite, not the fix.
- **A rewrite of existing documents.** Not needed — see below.

## No migration is needed

Verified by test rather than assumed: the driver's enum deserializer accepts `Int32`, `Int64` and `String`
on read regardless of the configured representation, which governs writing only. So existing numeric
documents keep reading correctly and convert to names the next time each is written.

The stragglers still matter, but later and for a different reason: a leftover number is correct only while
the ordinals hold still. Before v4 renumbers anything, either those documents have been rewritten or the
renumbering has to account for them.

## Risks checked

- **No query filters or sorts on either field** — grepped; the only write is
  `TeamRepository.Set(x => x.ConsentAccessLevel, …)`, which goes through the class-map serializer and will
  write a name. A filter comparing ordinals would have changed meaning; there is none.
- **Consumers reading the raw collection.** Anyone querying `ConsentAccessLevel: 3` externally would need
  to query `"Viewer"` instead. Nothing in this repo does, but it belongs in the release note.

## Acceptance criteria

- [ ] Both properties declare `[BsonRepresentation(BsonType.String)]`
- [ ] A test asserts the stored BSON type of every persisted enum in the repo
- [ ] A test proves documents already written as numbers still read correctly
- [ ] Full test suite passes; the sample compiles
- [ ] Backlog updated so the v4 sentinel entry records that its prerequisite is done

## Done condition

Every persisted enum in this repo is written by name, and a test fails if that stops being true.

## Version

No `MAJOR_MINOR` bump. The public API is unchanged and reads tolerate both representations, so no consumer
of the package has to act. The one caveat — external tooling querying the raw collection numerically —
goes in the release note rather than the version.
