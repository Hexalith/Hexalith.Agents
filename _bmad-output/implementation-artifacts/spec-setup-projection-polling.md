---
title: 'Poll Agent Setup Projection After Writes'
type: 'feature'
created: '2026-09-08'
status: 'done'
baseline_revision: 'dc75b145391a4fee4f2c53e5be8e4b98ced29627'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md'
warnings: []
deferred:
  - summary: >-
      Expected configuration versions cannot correlate lifecycle writes or distinguish a specific accepted configuration write.
    evidence: |-
      This limitation predates the polling bundle: activation and disable do not increment ConfigurationVersion, while configuration writes expose only an expected version rather than the accepted command identity. A same-version lifecycle read can therefore confirm the pre-write lifecycle, and a concurrent configuration write can satisfy current+1. Correcting either case requires changing the API/projection correlation contract that this bundle explicitly leaves untouched.
    location: >-
      src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:310
    severity: medium
  - summary: >-
      The intent may require retaining AuthoritativePending alongside every typed terminal read failure rather than rendering the existing fail-closed terminal surface.
    evidence: |-
      The phrase "preserving the truthful pending state" admits a strong reading, but the existing single-result UI contract replaces setup content with denied, missing, malformed, or unavailable surfaces. A product decision about whether prior pending truth must remain visible beside those terminal outcomes would settle the ambiguity.
    location: >-
      src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:375
    severity: "medium (unverified)"
---

<intent-contract>

## Intent

**Problem:** After an accepted Agent setup write, `AgentConfiguration` performs one expected-version read. If that read reports `AuthoritativePending`, the page remains pending until a manual reload even when the projection catches up moments later.

**Approach:** Keep re-reading the same expected configuration version on a short, `TimeProvider`-driven interval until the authoritative gateway reports `ProjectionConfirmed`, a five-second timeout expires, component disposal cancels the operation, or a terminal read outcome occurs. Preserve the last pending truth on timeout/cancellation and never manufacture confirmation from write acceptance or elapsed time.

## Boundaries & Constraints

**Always:** Poll only after a submitted write whose immediate expected-version read succeeds with `AuthoritativePending`; use the exact expected version already computed for the write; bound the loop to five seconds with 250 ms intervals; propagate a component-lifetime cancellation token through writes, reads, delays, and awaited gateway tasks; clear the submitted notice only on returned `ProjectionConfirmed`; keep typed non-success read outcomes fail-closed.

**Never:** Do not edit the deferred-work ledger, change API/client/server projection contracts, infer lifecycle callability, optimistically update setup state, expose sensitive configuration, add new markup/localization, use wall-clock sleeps in tests, or create a reusable polling framework.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Delayed catch-up | Accepted write; immediate expected-version read is pending; later read is confirmed | Re-read the same expected version and render confirmed projected truth; remove Submitted notice | No error expected |
| Polling exhaustion | Every read remains pending through five seconds | Stop polling; retain `AuthoritativePending`, stale freshness, and Submitted notice | Do not convert timeout to success or unavailable |
| Navigation/disposal | Component is disposed while polling | Cancel delay/read promptly and issue no later reads or renders | Swallow only cancellation caused by component lifetime |
| Terminal read outcome | A retry returns unavailable, denied, missing, malformed, or throws | Stop retrying and preserve typed fail-closed behavior; a thrown retry retains the last pending setup and reports write unavailable | Never expose exception text |
| Immediate confirmation | First expected-version read is confirmed | Render confirmation and do not schedule retries | No error expected |

</intent-contract>

## Code Map

- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:279-335` -- owns accepted-write expected-version calculation, the current one-shot read, rendered truth assignment, and Submitted-notice clearing; add the bounded lifetime-cancelled loop here.
- `src/Hexalith.Agents.Server/Projections/AgentSetupViewFactory.cs:44-54` -- read-only authority mapping expected-version lag to `AuthoritativePending` and catch-up to `ProjectionConfirmed`; UI must consume, not duplicate, this decision.
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs:41-58` -- read-only gateway already forwards expected version and structured failures; no contract change is needed.
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs:24-43` -- register `TimeProvider.System` as a non-overriding UI default so production composition satisfies the component dependency.
- `test/Hexalith.Agents.UI.Tests/AgentsTestContext.cs:33-95,134` -- shared clock registration; use the package-provided fake time provider for deterministic timer advancement.
- `test/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs:198-350` -- existing bUnit truth-flow and one-shot catch-up coverage; extend sequential responses and replace the indefinitely pending assumption.
- `test/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs:22-109` -- prove default clock composition remains complete and an existing clock registration wins.
- `test/Hexalith.Agents.UI.Tests/AccessibilityTests.cs:35-55` -- keep shell skip-link assertions aligned with bUnit 2.9 URI normalization while retaining the same fragment targets.
- `test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj` -- add the centrally versioned `Microsoft.Extensions.TimeProvider.Testing` test dependency.
- `Directory.Packages.props:11-24` -- align the root Fluent UI and bUnit/xUnit pins with the imported FrontComposer stack so the required bUnit matrix can compile and run.
- `eng/verify-story-5.2.ps1:50-73` -- existing read-only verification lane already selects `AgentConfiguration` and UI composition suites.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor` -- inject `TimeProvider`, own/dispose a lifetime cancellation source, pass cancellation into all setup I/O, and implement immediate-read-then-bounded-poll semantics without replacing pending truth on timeout.
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` -- add a `TryAddSingleton` default for `TimeProvider.System` while preserving host/test overrides.
- `test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj` and `AgentsTestContext.cs` -- install and register `FakeTimeProvider` so polling tests advance virtual time deterministically.
- `Directory.Packages.props` -- align Fluent UI and bUnit/xUnit with the imported FrontComposer stack so the focused and full UI test lanes compile and run.
- `test/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs` -- add bUnit cases for delayed confirmation, exhaustion, disposal cancellation, terminal failure, and immediate-confirmation no-retry behavior; assert identical expected-version reads and outermost rendered truth.
- `test/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs` -- cover default and pre-registered `TimeProvider` resolution.
- `test/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` -- update the two shell skip-link href assertions for bUnit 2.9 normalization without weakening their target checks.

**Acceptance Criteria:**
- Given a submitted configuration-changing write and a lagging setup projection, when fake time advances and the projection catches up within five seconds, then the configuration page re-reads only the same next configuration version, renders `ProjectionConfirmed`, and removes the Submitted notice.
- Given a submitted write whose setup projection remains lagging, when the five-second polling budget expires, then no further reads occur and the page still renders `AuthoritativePending`, stale freshness, and the Submitted notice.
- Given active polling, when the configuration component is disposed, then the in-flight cancellation token is cancelled and advancing time produces no additional gateway read or UI mutation.
- Given an immediate confirmation or terminal read failure, when post-write refresh runs, then it performs no unnecessary retry and presents only the authoritative confirmed state or existing typed fail-closed outcome.

## Spec Change Log

## Review Triage Log

### 2026-09-08 — Review pass
- verdicts: 25 findings — high 0, medium 19, low 3, false 2, maybe-false 1
- findings:
  - `[medium]` `[patch]` The xUnit 4 alignment made established `dotnet test --filter` project commands unusable under .NET 10 — kept the compatible xUnit 3 runner and explicitly pinned its exact `xunit.v3.assert` and `xunit.v3.extensibility.core` transitive packages, restoring the focused and full project commands.
  - `[medium]` `[defer]` Same-version activation and disable reads can clear Submitted before the lifecycle event projects — the baseline already used configuration version for lifecycle confirmation, and fixing it requires a lifecycle/correlation contract outside this bundle; recorded in `deferred`.
  - `[medium]` `[defer]` A concurrent configuration write can satisfy current+1 before the accepted write projects — the baseline expected-version mechanism cannot identify which command produced a version, and the intent forbids changing that contract; recorded with the related correlation limitation in `deferred`.
  - `[medium]` `[patch]` The first expected-version read was outside the five-second budget and could hang the refresh — moved timeout creation ahead of that read and use the linked token for the complete refresh.
  - `[medium]` `[patch]` Submitted and AuthoritativePending transitions could remain invisible while asynchronous polling continued — added explicit renders after acceptance and after the first pending result, with focused intermediate-state coverage.
  - `[medium]` `[patch]` Polling retries could overwrite administrator edits — authoritative drafts now apply on the immediate refresh only, and a retry-preservation test exercises edits made while polling.
  - `[medium]` `[patch]` Sensitive instructions remained in the component and DOM until polling completed and were skipped on disposal — clear the draft immediately after capturing the command, with an in-flight non-cooperative-read test.
  - `[low]` `[patch]` Gateway completion racing disposal could assign disposed component state — recheck the active token after awaited write/read completion and before each assignment.
  - `[medium]` `[patch]` Disposal coverage exercised only timer cancellation rather than outstanding gateway I/O — added a cancellation-ignoring incomplete read and verified prompt submission completion with no later read or markup mutation.
  - `[medium]` `[patch]` Terminal retry coverage omitted denied, missing, and malformed outcomes — expanded it to all typed fail-closed results and asserted polling stops.
  - `[medium]` `[patch]` The timeout did not cover a non-completing immediate expected-version read — the linked timeout now encloses that read, and the in-flight sensitive-data test pins its 4,999 ms/5,000 ms behavior.
  - `[low]` `[patch]` A read racing disposal could mutate component fields — `LoadAsync` now throws on the linked cancellation token before assigning `_result` or drafts.
  - `[medium]` `[patch]` An unrelated gateway `OperationCanceledException` escaped instead of rendering a typed failure — non-lifetime cancellation is now mapped through the existing unavailable write outcome and covered directly.
  - `[medium]` `[patch]` Retry reads reapplied authoritative drafts and could erase live edits — retries now pass `applyAuthoritativeDrafts: false`, with display-name and description assertions after catch-up.
  - `[medium]` `[patch]` The sensitive draft was not cleared on every submit path — it is now cleared before awaiting any write outcome, including disposal.
  - `[medium]` `[patch]` The xUnit 4 test application lacked repository runner compatibility — retained the repository's working VSTest-compatible xUnit 3 lane with aligned exact transitive pins instead of changing the repository-wide runner.
  - `[medium]` `[patch]` Tests did not prove polling remains active immediately before five seconds and stops at the boundary — exhaustion now asserts activity at 4,999 ms and completion at 5,000 ms.
  - `[medium]` `[patch]` Tests did not dispose during non-cooperative gateway I/O — added an incomplete expected-version task that ignores cancellation and asserted the wrapper completes without mutation.
  - `[medium]` `[patch]` The test-stack upgrade broke existing focused/full project commands — corrected the dependency pins and verified both commands execute tests successfully.
  - `[medium]` `[patch]` Submitted instructions remained rendered for the five-second polling window — the draft now leaves the render tree before the write await, proven while the first catch-up read remains incomplete.
  - `[false]` `[reject]` Polling should live in a reusable gateway/client/server surface — the verbatim intent explicitly names `AgentConfiguration`, so the page-local loop and bUnit surface are aligned.
  - `[low]` `[patch]` Exhaustion and cancellation tests used synthetic same-version lifecycle polling — changed the polling scenarios to version-bumping response-mode writes so their expected-version lag is realizable.
  - `[maybe-false]` `[defer]` Typed terminal failures may need to retain the last AuthoritativePending view — existing fail-closed UI behavior supports replacement, but only a product decision can settle the stronger reading; recorded as unverified in `deferred`.
  - `[medium]` `[patch]` Cancellation coverage lacked an actually blocked read — the new non-cooperative read test exercises lifetime cancellation through `WaitAsync`; direct component disposal is the bUnit equivalent of navigation teardown for this component.
  - `[false]` `[reject]` Supporting dependency and accessibility assertion changes diverged from the feature intent — they preserve the same runtime UI and fragment targets and were required to compile and execute the requested bUnit evidence against the imported FrontComposer stack.

## Design Notes

Use `CancellationTokenSource(TimeSpan, TimeProvider)` linked with the component-lifetime token, `Task.Delay(TimeSpan, TimeProvider, CancellationToken)`, and `Task.WaitAsync(CancellationToken)` so both delay and a non-cooperative gateway task are bounded by virtual time. Handle timeout inside the polling helper so the last successful pending result remains rendered; component disposal cancellation exits quietly. Other exceptions continue through the existing unavailable-write path without raw error disclosure.

## Verification

**Commands:**
- `dotnet build test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -m:1` -- expected: UI and tests compile warning-free.
- `dotnet test test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug --no-build --filter FullyQualifiedName~AgentConfiguration -m:1` -- expected: all Agent configuration bUnit tests pass, including virtual-time polling cases.
- `dotnet test test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug --no-build -m:1` -- expected: the full owning UI test project passes.

## Auto Run Result

Implemented a page-local, five-second post-write catch-up loop that reuses the exact expected configuration version, exposes Submitted and AuthoritativePending transitions, stops on confirmation or terminal outcomes, and remains cancellable through non-cooperative gateway tasks. Timeout and lifetime cancellation preserve the last successfully rendered truth rather than manufacturing confirmation.

Files changed:

- `Directory.Packages.props` — aligned Fluent UI/bUnit with the imported FrontComposer stack while retaining xUnit 3 and pinning its exact transitive packages for the established VSTest-compatible commands.
- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor` — added the bounded polling loop, lifetime cancellation, explicit transition renders, fail-closed cancellation handling, safe draft behavior, and immediate sensitive-draft clearing.
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` — registered `TimeProvider.System` as a non-overriding production default.
- `test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj` — added the centrally versioned fake-time dependency.
- `test/Hexalith.Agents.UI.Tests/AgentsTestContext.cs` — exposed and registered a per-test `FakeTimeProvider`.
- `test/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs` — covered immediate/delayed confirmation, the exact timeout boundary, disposal during delay and non-cooperative I/O, typed/throwing terminal outcomes, intermediate truth, draft preservation, and sensitive-data removal.
- `test/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs` — verified default and host-supplied `TimeProvider` composition.
- `test/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` — retained the same skip-link fragment checks under bUnit 2.9 URI normalization.
- `_bmad-output/implementation-artifacts/spec-setup-projection-polling.md` — recorded the frozen intent, implementation map, review triage, verification, and residual risks.

Review findings breakdown:

- Applied 11 grouped patches (high 0, medium 9, low 2): restored test-runner compatibility; bounded the initial read; rendered intermediate truth; preserved live drafts; removed submitted instructions immediately; guarded disposal races; mapped unrelated cancellation fail-closed; pinned the timeout boundary; exercised non-cooperative I/O; completed terminal-outcome coverage; and made polling tests use realizable version-bumping writes.
- Deferred two items in this spec only: the pre-existing expected-version correlation limitation for lifecycle/concurrent writes (medium), and the unverified stronger reading that typed terminal failures must retain the prior pending view (medium if true). The deferred-work ledger was not edited.
- Rejected the broader gateway/client/server placement finding because the verbatim intent explicitly assigns behavior to `AgentConfiguration`.
- Rejected the dependency/accessibility scope finding because those changes preserve runtime behavior and were required to compile and execute the requested bUnit evidence against the imported FrontComposer stack.

Follow-up review recommendation: `true`. This first pass patched nine medium-severity grouped entries; a follow-up should specifically re-check cancellation/render ordering around the newly bounded non-cooperative initial read and its exact timeout edge.

Verification performed:

- `dotnet build test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -m:1` — passed with 0 warnings and 0 errors.
- `dotnet test test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug --no-build --filter FullyQualifiedName~AgentConfiguration -m:1` — passed 33/33.
- `dotnet test test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug --no-build -m:1` — passed 1,011/1,011.
- `git diff --check` — passed.

Residual risks are limited to the two deferred contract/intent questions recorded in frontmatter. No API, client, server projection, markup, localization, or deferred-work ledger contract was changed.
