# Plan: team-events-and-leave

## Steps

### Part A: Complete event firing
1. [x] Write tests verifying each of the 6 methods fires `TeamsListChangedEvent` — 9 tests (6 new + 3 existing)
2. [x] Add missing event invocations to `TeamServiceBase` — all 6 methods now fire events
3. [x] Run tests, verify — 156 tests pass

### Part B: Leave team with validation
4. [x] Add `TransferOwnershipAsync` method to `ITeamService` / `TeamServiceBase` / `ITeamManagementService`
5. [x] Add leave validation logic to `RemoveMemberAsync` (check admin count, block owner)
6. [x] Write tests for leave validation + transfer ownership — 8 tests in LeaveTeamTests + 1 event test
7. [x] Add "Transfer Ownership" dialog to TeamComponent — button + member picker dialog
8. [x] Leave button stays hidden for owner (transfer first, then leave as admin) — owner sees "Transfer Ownership" button instead
9. [x] Run full test suite — 166 tests pass

### Finalize
11. [ ] Update backlog (remove completed items from Platform.md)
12. [ ] Commit
