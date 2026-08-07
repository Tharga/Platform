# Feature: register the email sender from the granular path (#176)

## Goal

Let a host on the documented granular path (`AddThargaAuth` + `AddThargaTeamBlazor`) register the invitation
email sender, instead of reproducing by hand what the `AddThargaTeam` facade does internally.

## Problem

`ITeamEmailSender` and `EmailOptions` were registered **only** inside the facade, in
`ThargaTeamRegistration`. A granular host had no supported hook, so it had to hand-copy the facade's
three-way choice against internal knowledge of what that method does.

**It failed more quietly than #157, its sibling.** `InviteUserDialog` and `TeamComponent` both resolve the
sender with `GetService<ITeamEmailSender>()` and degrade to manual link copying, so a granular host got no
error — invitations simply were never sent, and the fallback looked like intended behaviour.

Reported by Eplicta (Tharga/Team#176), non-blocking for them: registering `SmtpTeamEmailSender` by hand is a
workable interim.

## Scope

- `Email` (an `EmailOptions`) and `AddEmailService<T>()` on **`ThargaBlazorOptions`**, and the three-way
  registration moved into `AddThargaTeamBlazor`: custom sender > SMTP > nothing.
- The facade keeps `ThargaTeamOptions.Email` / `AddEmailService<T>()` working and **forwards** them down —
  exactly the shape icons took for #157, where the fix was folding into `AddThargaTeamBlazor` rather than
  adding a standalone `AddThargaTeamIcons`.
- Forward **only when set**, so a host that configured `o.Blazor.Email` directly is not overwritten with null
  by the options forwarder having already copied it.
- Documentation: a new "Sending the invitation email" section, since email had **no documentation at all** —
  which is itself part of what Eplicta reported.

**API shape decision.** A standalone `AddThargaTeamEmail(...)` was the alternative the issue offered. Rejected:
it leaves the facade and granular paths free to drift again, which is the defect being fixed. One
configuration surface, two entry points onto it.

**Also fixed in passing:** the facade's SMTP block copied `EmailOptions` property-by-property — the same
defect class as #177, on the same path. It now copies the whole instance through `OptionsForwarder`, with a
test driven from `EmailOptions`'s own shape.

## Acceptance criteria

- [x] A granular host can register a custom `ITeamEmailSender`, SMTP via `Email`, or neither.
- [x] A custom sender wins over SMTP options.
- [x] Neither configured registers **nothing**, so "no email" stays distinguishable from "wiring forgotten".
- [x] `FromName` still falls back to the application title — the one behaviour that changed layers.
- [x] The facade path keeps working unchanged, including email configured on its `Blazor` section.
- [x] Every `EmailOptions` property reaches the container, and a property added later cannot silently stop
      being forwarded.
- [x] Full solution builds with no new warnings; whole suite passes.

## Done condition

#176 closable, Eplicta can move their hand-rolled registration onto the supported hook, and the granular path
is no longer a step behind the facade for email.

## Notes

- `ThargaTeamOptions` has no `Title` of its own — the fallback always came from `o.Blazor.Title`, which the
  options forwarder copies into the Blazor layer's `Title`. So the resolved value is identical after the move,
  and there is now a test asserting it.
- Eplicta's documentation point is addressed on both surfaces: `ITeamEmailSender`'s XML docs and the new guide
  section both state that **invitations are the only mail the toolkit sends**, so a consumer can decide
  between adapting an existing pipeline and configuring SMTP.
