# Access simulation — see the app as a less privileged user

Let a team administrator temporarily drop some of their own access, look at the application as a member
with less would see it, and click once to come back.

It exists to make **setting a user's access correct** easy — to answer *"if I give this person this role,
what will they actually get?"* without keeping a throwaway account per role combination, or editing your
own roles and remembering to put them back.

**De-escalation only.** The effective set is always a subset of what the caller genuinely holds.

## Setup

```csharp
builder.AddThargaTeam(o =>
{
    o.Blazor.Simulation.Enabled = true;
});
```

Then place the control wherever it belongs in your layout — typically the header, beside the team
selector:

```razor
<AccessSimulationBar />
```

That one component is both halves: the **"View as…"** entry point when nothing is simulated, and the
**banner with the way out** when something is. Placing it means you cannot wire up the way in without the
way out.

Off by default. A host that does not enable it is unaffected — the cookie is never read, the filter is
never reached, and the audit enricher is not even registered.

## Who can use it

Anyone holding the **`simulation:use`** scope, registered at `AccessLevel.Administrator`. Since
Owner and Administrator are granted every registered scope, that means **team owners and administrators**
by default — without the toolkit hard-coding either. Widen it by granting the scope to a tenant role, or
withhold it by re-registering it at a level nobody has.

**Ending a simulation is never gated.** A simulation can remove `simulation:use` itself, so requiring it
to stop would let someone strand themselves.

## What you can simulate

| Target | What it means |
|--------|---------------|
| **A member** | The access that person actually holds in this team |
| **A role** | Exactly what that tenant role grants |
| **An access level** | Exactly what that level grants |
| **Scopes** | A set you tick by hand, from the scopes you hold |

All four work the same way: each names a **target scope set**, and the simulation keeps what the target
has *and you also have*, removing everything else.

**Applying a role replaces, it does not add.** Simulating the `Support` role leaves you with `Support`'s
scopes and nothing more — not your own plus its.

## What it cannot show, and why you are told

Before you apply a simulation, the picker tells you what it will **not** be able to reproduce.

This matters more than it sounds. If the target holds a scope you do not, the simulation shows the
intersection — **less than they really see**. Without a warning you would conclude *"they cannot reach
the billing page"* about something they can, and grant them more access than they need. That is the exact
outcome the feature exists to prevent, so the gap is stated rather than left silent.

Two things can be missing:

- **Scopes you do not hold yourself.** Rare for an administrator, who holds every *registered* scope —
  but a member's `ScopeOverrides` are not validated against the registry, so they can carry a scope no
  access level grants.
- **System-wide access.** Always, when simulating a person. System scopes come from application roles
  issued by your identity provider, which the toolkit does not store — so another user's system access is
  *unknown*, not empty.

**A simulation therefore shows access within the selected team, never someone's system-wide reach.** Your
own system scopes and application roles are dropped for every kind of simulation, so you will lose
cross-team visibility — including the wider team list — until you return.

## What it cannot reach

**Simulation filters claims.** A component that queries the store directly sees your real record however
thoroughly your claims were narrowed — a member record still says `Owner`, because nothing about your
stored access changes.

Everything in the toolkit routes authorization through claims, so this is invisible in normal use. If you
write a component that defaults UI state from a stored record, ask:

```csharp
if (AccessSimulationCookie.IsActive(principal)) { /* prefer the claim, not the record */ }
```

## Auditing

Anything done while simulating is **still recorded as you**. Simulation removes scopes and roles and
never touches identity claims, so the actor is the real person by construction.

Entries gain three metadata keys, so an otherwise puzzling record is legible — why an administrator's
action was refused, or performed at a level below the one they hold:

| Key | Value |
|-----|-------|
| `simulation.active` | `true` |
| `simulation.kind` | `User` · `Role` · `Scopes` · `AccessLevel` |
| `simulation.target` | The member, role or level being simulated |

## How it works

The active simulation rides in a session cookie, read once per request and carried on the principal
thereafter — the same pattern the selected team uses, and necessary because a live Blazor circuit has no
`HttpContext`.

**The cookie is not signed, and does not need to be.** The filter can only ever *remove* claims, so
editing the cookie to name scopes you do not hold achieves nothing. That is why the guarantee is a
property of the mechanism rather than of a calculation being correct.

Starting or stopping writes the cookie and reloads the page, which re-issues claims through the ordinary
request path. The filter is applied on both claim-issuance paths — the HTTP one and the periodic
in-circuit revalidation — so a simulation does not quietly expire at the next revalidation interval.

**Access level is the one thing replaced rather than removed**, because `[RequireAccessLevel]` reads a
single value and `AuthorizeView Roles="Team…"` reads the matching role. Both move together, clamped so
the simulated level is never more privileged than the real one.

## Simulation does not end at the browser

The reduced claims are your claims. If your host authenticates its API with the cookie scheme, **your own
REST calls are de-escalated too** while a simulation is active. That is deliberate — the alternative is a
claim set that differs by surface, which is exactly the confusion the toolkit's one-enforcement-point rule
exists to avoid.

It does not apply to API keys. A key's scopes are directly editable, so simulating one would earn nothing.
