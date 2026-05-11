# Plan: MCP user-scope and team-scope resources

## Steps

- [x] **1. Add `GetMembersAsync` to `ITeamService` + default `TeamServiceBase` implementation**
  - Declare `IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey)` on `ITeamService`. Use `IAsyncEnumerable` (not `Task<IReadOnlyList>`) so the dispatcher can stream large member lists if needed; matches `GetTeamsAsync()` shape.
  - In `TeamServiceBase`, provide a non-abstract implementation:
    ```csharp
    public virtual async IAsyncEnumerable<ITeamMember> GetMembersAsync(string teamKey)
    {
        var team = await GetTeamAsync(teamKey); // protected non-generic
        if (team == null) yield break;
        var membersProperty = team.GetType().GetProperty("Members");
        if (membersProperty?.GetValue(team) is System.Collections.IEnumerable members)
        {
            foreach (var member in members.OfType<ITeamMember>())
                yield return member;
        }
    }
    ```
    Reflection is contained inside `TeamServiceBase`; callers see a typed `IAsyncEnumerable<ITeamMember>`.
  - The `TeamManagementService` decorator (`Tharga.Team/TeamManagementService.cs`) — verify whether it implements `ITeamService` directly. If yes, add a forwarding pass-through. (Probably no — it's a separate `ITeamManagementService` decorator.)
  - `TestTeamService` (in `Tharga.Team.Service.Tests`) and `StubTeamService` (in `Tharga.Team.Blazor.Tests`) inherit `TeamServiceBase` — they'll pick up the default implementation. Verify nothing breaks.

- [x] **2. `PlatformUserResourceProvider`**
  - New file `Tharga.Platform.Mcp/PlatformUserResourceProvider.cs`. Implements `IMcpResourceProvider`, `Scope = McpScope.User`.
  - Constructor injects `IUserService` and `ITeamService`. Plus `IHttpContextAccessor` for the current `ClaimsPrincipal` (the `IMcpContext` has UserId but `IUserService.GetCurrentUserAsync` wants a `ClaimsPrincipal` — pass through HttpContext).
  - `ListResourcesAsync` — if `context?.UserId == null` return empty array; else return single descriptor for `platform://me`.
  - `ReadResourceAsync("platform://me")`:
    - Get current user via `_userService.GetCurrentUserAsync()` (HttpContext-derived).
    - Get memberships via `await foreach var t in _teamService.GetTeamsAsync()`. For each, project `{ teamKey: t.Key, teamName: t.Name }`. Access level comes from the member row matching the current user — use the new `GetMembersAsync(t.Key)` and find the entry by user.Key.
    - Serialize with the same `_jsonOptions` pattern as `PlatformSystemResourceProvider`.
  - URI constant: `public const string MeUri = "platform://me";`

- [x] **3. `PlatformTeamResourceProvider`**
  - New file `Tharga.Platform.Mcp/PlatformTeamResourceProvider.cs`. Implements `IMcpResourceProvider`, `Scope = McpScope.Team`.
  - Constructor injects `ITeamService` and `IApiKeyAdministrationService` (the latter optional — only enables the apikeys resource when registered).
  - URI constants:
    ```csharp
    public const string TeamUri = "platform://team";
    public const string MembersUri = "platform://team/members";
    public const string ApiKeysUri = "platform://team/apikeys";
    ```
  - `ListResourcesAsync` — if `context?.TeamId` (the `TeamKey` claim) is null/empty, return empty array. Otherwise return descriptors for `TeamUri`, `MembersUri`, and (if `_apiKeyAdministrationService != null`) `ApiKeysUri`.
  - `ReadResourceAsync`:
    - Require `context?.TeamId` non-null; throw `UnauthorizedAccessException("No team selected.")` if missing.
    - `TeamUri` → fetch non-generic `ITeam` via `_teamService.GetTeamsAsync().FirstOrDefaultAsync(t => t.Key == context.TeamId)` (avoids needing a new non-generic `GetTeamAsync` on `ITeamService` — uses existing `GetTeamsAsync()` filtered by Key). Serialize `{ key, name, icon, consentedRoles }`.
    - `MembersUri` → `await foreach var m in _teamService.GetMembersAsync(context.TeamId)`. Project `{ key, name, accessLevel, state, tenantRoles, scopeOverrides, invited: m.Invitation != null }`.
    - `ApiKeysUri` → `await foreach var k in _apiKeyAdministrationService.GetKeysAsync(context.TeamId)`. Project `{ key, name, accessLevel, expiryDate, createdAt, createdBy }` — deliberately omit raw `ApiKey` value (redaction pattern).

- [x] **4. Register both providers in `McpPlatformBuilderExtensions.AddPlatform`**
  - Always register `PlatformUserResourceProvider` (always available; `IUserService` and `ITeamService` are core Platform services).
  - Always register `PlatformTeamResourceProvider`. The provider self-gates on TeamKey claim presence; `IApiKeyAdministrationService` is optional via the existing nullable-constructor pattern.
  - Existing `ExposeSystemResources` opt-in for `PlatformSystemResourceProvider` is unchanged.

- [x] **5. Tests**
  - Add tests to existing `Tharga.Platform.Mcp.Tests` project.
  - `PlatformUserResourceProviderTests`: empty list when context.UserId is null; happy-path list returns one descriptor; happy-path read returns user + memberships JSON; missing user (race / race between auth and read) throws or returns empty.
  - `PlatformTeamResourceProviderTests`: empty list when context.TeamId is null; list returns 2 descriptors when ApiKey service absent, 3 when present; read throws `UnauthorizedAccessException` when TeamId null; happy-path read for each of the three URIs returns expected JSON shape with redacted apikey values.
  - `TeamServiceBaseGetMembersAsyncTests`: in `Tharga.Team.Service.Tests`. Build a `TestTeamService` with a team containing members; assert `GetMembersAsync(teamKey)` yields them in order.
  - Verify the existing `PlatformSystemResourceProviderTests` still pass.

- [x] **6. Update `Tharga.Platform.Mcp/README.md`**
  - Add a new "User and team resources" section before "System-scope diagnostic resources". Document the three new URIs + payload shapes. Move the existing system-scope section's deferred-work note to reflect that user+team are now shipped; cross-tenant is the only remaining piece.

- [x] **7. Update `Tharga.Team/README.md`**
  - Mention the new `GetMembersAsync` on `ITeamService` in the "Service interfaces" bullet (one-line addition).

- [x] **8. Build + full test suite**
  - `dotnet build c:/dev/tharga/Toolkit/Platform/Tharga.Platform.sln -c Release` clean.
  - `dotnet test c:/dev/tharga/Toolkit/Platform/Tharga.Platform.sln -c Release` green.

- [x] **9. Commit + push the feature branch**
  - Conventional prefix: `feat:` — this adds new MCP surface (not just a fix).
  - Suggested message: `feat: MCP user-scope and team-scope resource providers`.

- [x] **10. Pause for user verification.** Plan/ stays on the feature branch; deleted in the close-out commit before the PR opens (per the principle in shared-instructions).

## Verification approach

- After step 1, run `Tharga.Team.Service.Tests` to confirm the new `GetMembersAsync` default works with existing TestTeamService.
- After step 4, run `Tharga.Platform.Mcp.Tests` to confirm the existing system-slice tests still pass with the new providers registered.
- Build between every step that adds a new file.

## Open questions

(none — three design choices were locked via `AskUserQuestion` during planning: scope = user+team, member API = new ITeamService method, URI shape = `platform://team*`)

## Last session
2026-05-11 — All implementation steps complete. 17 new tests (5 user-provider, 10 team-provider, 2 GetMembersAsync); 313 total green. READMEs updated. Ready for commit + push + user verification.
