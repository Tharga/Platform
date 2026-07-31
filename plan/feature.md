# Feature: Audit actor provenance

Closes GitHub [#163](https://github.com/Tharga/Team/issues/163), which Eplicta is waiting on for its
FortDocs worker (EP-4723 / EP-4724), and folds in the two audit items already on the backlog that touch
the same record and the same method.

## The problem, in one line

`AuditHelper.BuildEntry` sources the actor entirely from `IHttpContextAccessor.HttpContext.User`, so a
caller with no `HttpContext` is recorded as **`AuditCallerType.User` with a null `CallerIdentity`** — not
merely incomplete, but **wrong**: the audit log claims a user did something a background worker did.

## Scope

### 1. An actor that works without HTTP (#163)

- **`AuditCallerType.System`** and **`AuditCallerSource.Background`** — the vocabulary for "not a person".
- **`IAuditContextAccessor`**, `AsyncLocal`-backed, that `BuildEntry` falls back to when there is no
  `HttpContext`. A worker opens a scope around each unit of work and its entries carry a service identity,
  the team, and a correlation id.
- **Default to `Unknown`, never `User`.** With neither an HTTP principal nor an ambient scope, the honest
  answer is that we do not know. Recording `User` is the actual defect — a wrong attribution is worse than
  an absent one, because it is indistinguishable from a real one when read back.

`TeamKey` already works on this path (it is passed explicitly), so only the actor is missing.

### 2. A stable caller key (backlog → Audit item 1)

`CallerIdentity` resolves `ClaimTypes.Name` → `preferred_username` → `NameIdentifier` → `name`, so its
content depends on which claims the identity provider emits — and the per-user audit dialogs match it as a
**substring**. When the name claim holds a display name and the grid pins an email, the dialog is empty
though entries exist.

Add **`CallerUserKey`**, populated from the resolved user, and pin on that. This is exactly the fix
`CallerKeyId` already applies for API keys, for the reason that entry records: the name was
"human-friendly but not unique".

### 3. A caller filter in `AuditLogView` (backlog → Audit item 2)

Caller is a grid column but not a filter, so outside a pinned dialog there is no way to ask "what has this
actor done". Small, and it is what makes items 1 and 2 usable.

## Why these three together

They are one story — *the audit log records who acted, reliably, and you can find it again* — and they
touch the same three places (`AuditEntry`, `AuditHelper.BuildEntry`, the Mongo mapping). Shipping them
separately means three passes over the same record and three releases for consumers to absorb.

## Out of scope

- **A target/subject field** ("what was done *to* a user") — backlog Audit item 4, genuinely unrecorded,
  needs its own schema decision, and is Nice rather than Important. Do it after this.
- **Reprocessing existing entries.** Rows written before this have no `CallerUserKey` and keep whatever
  `CallerType` they were given; nothing backfills.

## Acceptance criteria

- [ ] A call with no `HttpContext` and no ambient scope records `Unknown`, never `User`.
- [ ] A call inside an audit scope records the supplied actor, `AuditCallerType.System` and
      `AuditCallerSource.Background`.
- [ ] The scope is `AsyncLocal`, so it survives `await` and nested calls, and does not leak across
      concurrent work.
- [ ] An HTTP caller is unaffected — same `CallerType`, `CallerSource` and `CallerIdentity` as today.
- [ ] `AuditEntry.CallerUserKey` is populated for a resolved user caller, round-trips through Mongo, and
      is filterable via `AuditQuery`.
- [ ] The per-user audit dialogs pin the stable key rather than the display string.
- [ ] `AuditLogView` offers a caller filter.
- [ ] Full test suite passes.

## Version

**Additive but minor.** New public enum members, a new interface and registration, and a new field on
`AuditEntry`. `MAJOR_MINOR` moves **3.8 → 3.9**.

Two behaviour changes worth a release note:

- **A caller that previously recorded as `User` with a null identity now records as `Unknown`.** That is
  the fix, but a consumer filtering or reporting on `CallerType == User` will see those rows move.
- **New enum members can break an exhaustive `switch`** in consumer code that covers every existing case
  without a default arm.
