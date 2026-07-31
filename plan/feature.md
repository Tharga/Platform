# Feature: Resolving the caller outside an HTTP request and outside a circuit

Phase 1 of `planned/06-audit-access-verification.md`, pulled out because it is a live defect rather than
verification work: **every MCP resource read fails today**, so `Tharga.Team.Mcp` is unusable for reads.

## The defect

`BlazorTeamPrincipalAccessor` assumes there are exactly two worlds:

```csharp
var httpContext = httpContextAccessor.HttpContext;
if (httpContext != null) return httpContext.User;
return (await authenticationStateProvider.GetAuthenticationStateAsync()).User;   // Razor-only
```

When neither holds — an MCP request handler, a hosted service, a message handler — the fallback calls an
API that only works inside a Razor component's DI scope, and throws:

> `System.InvalidOperationException: Do not call GetAuthenticationStateAsync outside of the DI scope for a
> Razor component. Typically, this means you can call it only within a Razor component or inside another
> DI service that is resolved for a Razor component.`

Observed on the sample with a valid team API key: `initialize` and `resources/list` succeed;
`resources/read` returns JSON-RPC `-32603 "An error occurred."`, and with a debugger attached stops on the
exception above. `resources/list` survives because it self-gates on the captured `IMcpContext`;
`resources/read` goes through `ITeamService` → the authorization decorators → this accessor.

**This is the third instance of one mistake:** *no `HttpContext`, therefore it must be X*. X was "a user"
in the audit entry builder (#163), "a Razor circuit" here. The shape to fix is the assumption, not just
this call site.

## Scope

1. **Reproduce first**, as a failing test. The exception is definitive about the mechanism, but not about
   which call site produced it — confirm before changing behaviour.
2. **Teach the accessor the third case.** With no `HttpContext` and no circuit, the honest answer is *no
   principal*, not an exception. Callers already handle a null/anonymous principal: the authorization
   decorators refuse, and the read paths yield nothing.
3. **Do not silently swallow.** A caller that genuinely is in a circuit and fails must still fail loudly —
   the fix must distinguish "not in a circuit" from "in a circuit and broken".

## Explicitly not in scope

- **Making MCP reads return data.** This makes them stop throwing. Whether a team API key can then read
  `team://team` depends on the authorization rules, which is exactly what 06 phase 3 verifies. Fixing the
  crash and asserting the access matrix are different jobs, and conflating them would let a permissive
  result look like success.
- **The audit `CallerFilter` background flag**, `Tharga/Mcp#18`, and the REST endpoints. All tracked
  elsewhere.

## Acceptance criteria

- [ ] Resolving the principal with no `HttpContext` and no circuit does not throw.
- [ ] It yields no principal, so authorization refuses rather than accidentally permitting.
- [ ] An HTTP caller is unchanged — same principal, same claims.
- [ ] A genuine in-circuit resolution is unchanged.
- [ ] A failure *inside* a circuit still surfaces, rather than being swallowed as "no principal".
- [ ] An MCP `resources/read` with a team API key no longer returns `-32603`.
- [ ] Full test suite passes.

## Version

Bug fix, no API change. **No `MAJOR_MINOR` bump** — nothing requires a consumer to act.

If it turns out a caller was being *permitted* somewhere by the exception path failing open, that would be
a behaviour change and would need saying explicitly. Not expected: the exception propagates, so today it
fails closed by crashing.
