# Feature: ApiKey Copy Button Visibility

## Goal
Show the copy button for any unlocked key, not just when the key is currently revealed.

## Originating branch
develop

## Scope
- Change copy button visibility condition in ApiKeyView.razor

## Acceptance Criteria
- [ ] Copy button visible when key has an ApiKey value (unlocked), regardless of show/hide state
- [ ] Copy button hidden for locked keys (no ApiKey value)
- [ ] All tests pass

## Done Condition
All acceptance criteria met.
