# Brownfield Drift Review — ARCHITECTURE-SPINE.md (v8)

**Lens:** Does the spine ratify (rather than contradict) the actual current state of the codebase and its
tracking artifacts? Do tracking artifacts (sprint status, dependency/readiness registers) accurately reflect
real implementation state?

**Reviewer:** independent brownfield-drift pass, fresh verification (no findings trusted from prior rounds
without re-derivation from `git log`, source files, or running tests).

**Scope reviewed in full:** `ARCHITECTURE-SPINE.md` (798 lines), `sprint-status.yaml`,
`external-dependency-register.md`, `launch-readiness-register.md`, `src/` tree, `test/Hexalith.Agents.Server.Tests/
ForbiddenHostingOwnershipTests.cs` (executed), `deferred-work.md`, `epics.md` (Epic 5 sections and the
2026-09-09 "Required addition" reconciliation table), `AgentAggregate.cs`, `ProviderCatalogAggregate.cs`,
`AgentLaunchReadinessPolicy.cs`, the PRD's `validation-report-2026-09-09.md`, and `.memlog.md` (full round-3/
round-4 history) for context on what prior rounds already found/fixed.

## Freshness Check (git)

`git log -1 -- src/` → `a191d249` (2026-09-08 16:37), unchanged across every commit since, including the
spine's own last edits (`f691486`, `9aa784f`, `4ec626d`, `606712f`, `c4121b0`, all 2026-09-08/09). The one
commit that looked like a code change by its message — `606712f "Refactor code structure for improved
readability and maintainability"` — touches only six UX validation-report files under
`_bmad-output/planning-artifacts/ux-designs/...`, zero `src/`/`test/` files (`git diff a191d24 606712f --
src/ test/` is empty). **Verdict: `src/`/`test/` are byte-identical to the last brownfield-drift pass (v6);
this round's fresh verification is a re-derivation of the same code facts, not a re-review of new code.**
The spine's own text changed only in small, non-substantive ways since the last full pass (`architecture_
assumption_index_version: 2→3`, sources-list additions, and a few AD-13→AD-6 citation-number fixes) — no
new architectural claim was introduced that could newly contradict the code.

## Critical

None found this round.

## High (already-tracked implementation debt — confirmed unchanged, not new findings)

Per the task's own framing, these are confirmed present and unchanged, and are already routed to named
backlog stories by the spine, `epics.md`, and the dependency register. Listed for completeness of a fresh
check, not as new defects.

### H1 — AD-2 platform/tenant `ProviderCatalog` split still absent

`src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs:19` doc-comments itself as "the
tenant-scoped governed provider/model catalog" — no `system`-tenant platform catalog, no
`TenantProviderEnablement` aggregate exists anywhere under `src/` (`find … -iname "*TenantProviderEnablement*"`
returns nothing). This exactly matches `external-dependency-register.md`'s own
`NC-5.3-PLATFORM-CATALOG-SCOPE` non-conformance record ("shipped implementation uses tenant-scoped Provider
catalog streams... does not yet implement AD-2's platform `system` catalog plus tenant-scoped
`TenantProviderEnablement` split") and the Live-Seam Matrix row ("platform catalog migration and tenant
enablement are `Deferred`"). Sprint status correctly shows `5-3-govern-provider-models-and-pricing-through-
live-operations: backlog` (not done) for the reopened AC that would fix this. Consistent, no drift.

### H2 — AD-4 `ConfigurationVersion` still not bumped by lifecycle events

`AgentAggregate.cs:267` (`Handle(ActivateAgent...)`) emits `new AgentActivated(agentId)` and
`AgentAggregate.cs:298` (`Handle(DisableAgent...)`) emits `new AgentDisabled(agentId)` — neither event
carries or bumps `ConfigurationVersion`, confirmed by grepping every `ConfigurationVersion` reference in the
file (only `AgentPartyIdentityLinked`, `AgentResponseModeConfigured`, `SelectAgentProviderModel`, etc. bump
it). This is exactly `deferred-work.md` `DW-4` and exactly what `sprint-status.yaml:88` cites as the reason
`5-2-configure-hexa-through-live-eventstore-operations` is correctly `in-progress` rather than `done`:

> `# Corrected 2026-09-09 (architecture update pass, C-7): amended AC require the ConfigurationVersion
> lifecycle bump and AgentLifecycleConfigurationVersionTests, neither of which exist in code (DW-4); was
> incorrectly marked done.`

**This is the "prior round found and fixed one story falsely marked done" item the task asked me to
re-verify. Re-verified fresh against the actual current code: the fix still holds — Story 5.2 is still
correctly `in-progress`, and the underlying code gap it cites is still genuinely present, unmodified since
the fix was applied.** `AgentLifecycleConfigurationVersionTests` does not exist anywhere under `src/` or
`test/` (only referenced in tracking docs), confirming the AC really is unmet, not just under-annotated.

### H3 — No shared `AgentsIdentity` canonicalizer

`grep -rln "IdentityCanonicalizer\|AgentsIdentity\b" src/` returns nothing. AD-29 describes one shared
canonicalizer function; the shipped code derives ids through several separate helpers instead. Unchanged
since v6.

### H4 — Structural seed gaps

`src/Hexalith.Agents.Server/Aggregates/` contains only a `README.md` (no code); `Application/Tools/` contains
only `.gitkeep`; no `Hexalith.Agents.IntegrationTests` project exists yet (owned by Story 5.6, `backlog`).
Matches the spine's own Structural Seed section, which names `test/Hexalith.Agents.IntegrationTests/` as
"created by Story 5.6." Consistent, not a contradiction — the spine describes the target layout, the seed is
partially unbuilt as expected.

### H5 — AD-30 HMAC-tagged extensions and `Agents.PlatformOperator` policy absent

`grep -rln "HMAC" src/` and `grep -rln "Agents.PlatformOperator\|PlatformOperator" src/` both return nothing.
AD-30 requires every reserved command-envelope extension to carry an HMAC tag verified via `EXT-SECRETS-1`,
and the FR-33→FrontComposer-policy map names `Agents.PlatformOperator`. Neither exists in the shipped
authorization gate (`ProviderCatalogAggregate.cs` uses plain string-equality on `actor:agentsProviderAdmin`,
by its own doc comment marked "Authorization is transitional"). Unchanged since v6.

### H6 — AD-7's "retired" Party link/replace commands are still live, but this is explicitly tracked

`AgentAggregate.cs:305` `Handle(LinkAgentPartyIdentity...)` and `AgentAggregate.cs:349`
`Handle(ReplaceAgentPartyIdentity...)` both still return `DomainResult.Success(...)` on valid input — they
are not unconditional rejections, directly contradicting AD-7's normative sentence: "no principal, including
the Tenant Agent Administrator, can link, replace, or clear `hexa`'s Party identity after provisioning in
V1." **This is explicitly tracked, not hidden drift:** the PRD itself (line 149) states "the shipped Party
link and replace commands (Spine AD-7) are on the FR-23 deprecate-and-reject register," and `epics.md`'s
2026-09-09 reconciliation table assigns the fix to Story 5.2 by name: "5.2 | Keep Party link/replace wire
members additive, mark them obsolete, reject every request, and prove FR1 provisioning is the only
identity-creation/link path." Story 5.2 is already correctly `in-progress` (see H2/C1), so this gap does not
change that story's status — it is simply an additional unmet AC on the same already-not-done story, not a
new contradiction anywhere the spine claims completion.

### H7 — Legacy `EnableProductionLikeGeneration` gate bypasses the new `LaunchReadinessGate`/register/`RQ-1` system entirely

`AgentAggregate.cs:637` `Handle(EnableProductionLikeGeneration...)` can return `Success` purely from
`AgentLaunchReadinessPolicy.ComputeLaunchReadinessBlockers` (`src/Hexalith.Agents/Agent/
AgentLaunchReadinessPolicy.cs`), an eight-blocker, Agent-scoped, dependency-free check (content safety
policy present, context policy present, launch metrics recorded, latency targets, cost posture, audit
governance resolved) left over from the historical Epic-4 design. It has no reference to the `system`-tenant
`LaunchReadinessGate` aggregate, the 18 `LR-*` GateIds, `RQ-1`, `OperationGateMatrixVersion`, or any
`EXT-*` dependency `Available` status that AD-17/the launch-readiness register now define as the "normative
readiness authority." None of those newer types exist in `src/` at all. **Already tracked, not a new
finding:** the PRD's own `validation-report-2026-09-09.md` independently reaches the identical conclusion
verbatim ("Code: the only enablement gate is `ProductionLikeGenerationEnabled`... none concerns payload
protection or an external dependency... Classification: CONTRADICTED (as a statement of shipped behaviour)")
and `epics.md` Story 5.7 ("Activate hexa Only When Setup Gates Pass," `backlog`, Result: "Not run — backlog;
prior stories and external commitments are incomplete") is the named replacement. No spine-vs-code
contradiction is hidden here; it is openly acknowledged in three separate places already.

## Medium

### M1 — `sprint-status.yaml`'s Story 5.3 `backlog` entry has no inline annotation explaining a completed, tested prior implementation exists underneath it

`_bmad-output/implementation-artifacts/spec-5-3-govern-provider-models-and-pricing-through-live-operations.md`
carries a full Dev Agent Record: "Latest Release Test Evidence... Run (UTC): 2026-09-08T15:32:15Z... Total
2569, Passed 2569, Failed 0... Result: PASS," with a File List and Suggested Review Order pointing at real,
merged code (`ProviderCatalogAdministrationOrchestrator.cs`, `ProviderCatalogProjectionHandler.cs`, etc.),
and git commit `a191d24` ("feat: govern provider models and pricing through live operations," 2026-09-08
16:37) landed the same day. Yet `sprint-status.yaml:89` reads simply:

```
5-3-govern-provider-models-and-pricing-through-live-operations: backlog
```

— no comment, unlike the neighboring `5-2` line which carries an explicit correction note. This is *not* a
false "done" claim (the direction of error is safe: under-claiming, not over-claiming), and it is fully
explained by cross-referencing `external-dependency-register.md`'s `NC-5.3-PLATFORM-CATALOG-SCOPE` record and
the "Known Consumer Non-Conformance" section (the prior tenant-scoped implementation is retained only as a
migration source, and the numbered Story 5.3 in the current, reopened `epics.md` demands the platform/tenant
split that implementation doesn't have). But a reader of `sprint-status.yaml` alone — the file this task
specifically asked to spot-check — cannot tell that from the file itself; they would have to already know to
cross-reference the dependency register. Recommend a one-line comment on that entry, matching the `5-2`
line's style, e.g. "reopened by sprint-change-proposal-2026-09-09-2 (NC-5.3-PLATFORM-CATALOG-SCOPE); prior
tenant-scoped implementation retained as migration source only." Low risk, easy fix, not a ratification
blocker.

### M2 — Register cross-check: no new mismatch found; the one prior-round fix (LR-AUDIT-PROTECTION-DELETION DeletionRequest symmetry) still holds

Read `launch-readiness-register.md` line 108 fresh: "Includes evidence that a `LegalHold` **or a
`DeletionRequest`** either propagates to (or has a confirmed accepted-risk waiver for) the posted copy of
held **or erased** content in Conversations..." — this is exactly the round-4 `.memlog.md` fix ("Round-4 fix
for round-3 C-3: launch-readiness-register.md LR-AUDIT-PROTECTION-DELETION evidence text... amended to name
DeletionRequest/erasure symmetrically with LegalHold"). Confirmed still present and matching AD-22/ARCH-A-10's
current spine text, which makes the same symmetric claim. **Re-verified: still correctly fixed, no
regression.**

Also spot-checked for new mismatches and found none: `EXT-PROVIDER-1`'s register annotation ("narrowed
2026-09-09 by the architecture update: Story 5.3 published catalog truth adapter-free and is not a consumer")
matches the spine's AD-9 text verbatim in intent; `EXT-HOST-1`'s "Historical prior commitment only" framing
matches the spine's AD-16/AD-23 text; the Live-Seam Matrix's `Deferred` statuses for Dapr Workflow, Provider
invocation, Conversations membership/posting, content safety, payload protection, and audit inspection all
match the actual absence of corresponding code (`src/Hexalith.Agents.Server/Application/Workflows/` contains
only `.gitkeep`; no HMAC, no `DigestKey`, no `IEventPayloadProtectionService` anywhere in `src/`, confirmed
via the same grep the PRD's own validation report used).

## Low

### L1 — No further new drift found

Fresh checks confirmed true and unchanged from the spine's own claims: AD-16's "ships no module-owned
AppHost, Aspire, or ServiceDefaults project" — verified by running
`ForbiddenHostingOwnershipTests` (`dotnet test --filter FullyQualifiedName~ForbiddenHostingOwnershipTests`):
**5/5 tests pass**, including the token-scan test that forbids `Aspire.Hosting`,
`DistributedApplication.CreateBuilder`, `Aspire.AppHost.Sdk`, etc. anywhere in repository-owned text. AD-2's
aggregate list (14 named aggregates) vs. the 3 actually implemented (`Agent`, `AgentInteraction`,
`ProviderCatalog`) — a known, expected gap the task pre-authorized as non-novel. AD-18's Dapr Workflow
ownership — not implemented (`Application/Workflows/.gitkeep` only), matching its `Deferred (Story 6.1)`
Live-Seam Matrix entry exactly.

### L2 — Epics 1–4 "done" status is explicitly scoped as historical-only by the tracking documents themselves, not a live-conformance claim

`epics.md` lines 30–31 state this directly: "Epics 1–4 and every completed story beneath them remain
unchanged historical delivery evidence with status `completed`. Their completion proves only the evidence
recorded at the time; it does not establish live production conformance, current callability, or release
readiness." Spot-checked one instance of apparent tension (Story 4.4 "Define And Enforce Launch Readiness
Gates," `done`, implemented via the now-superseded `AgentLaunchReadinessPolicy`/`AgentLaunchReadinessBlocker`
design rather than the current `LaunchReadinessGate`/register model) and confirmed it is exactly the case
this reinterpretation clause exists to cover — not a fresh drift instance.

## Verdict

**FAILS TO RATIFY, FIXABLE** — same verdict and same underlying reasons as the prior brownfield-drift pass
(v6), because nothing changed in the code between that pass and this one (`git diff a191d24..HEAD -- src/
test/` is empty), and the spine's frontmatter still asserts `status: final` while H1–H7 above remain open
implementation gaps. Per this lens's own brief, none of H1–H7 requires a spine text change — the spine's own
words already describe every one of them as a pending target, each is already routed to a named backlog
story (5.2, 5.3, 5.6, 5.7, 5.10), and the PRD's own validation reporting independently reaches the same
conclusions for H6/H7. The specific defect this lens exists to catch — a tracking artifact silently
contradicting the spine/code state — was not found anywhere this round:

- **The one previously-fixed defect (Story 5.2 falsely marked `done`) is still correctly fixed**, and its
  root cause (DW-4 / AD-4 `ConfigurationVersion` bump) is still genuinely present in code, so the
  `in-progress` status remains accurate.
- **No other story shows the same "done despite unmet amended AC" pattern.** Every `done` epic-1–4 story is
  explicitly scoped as historical-only by `epics.md`'s own reconciliation language; every epic-5+ story with
  an unmet AC (5.2, 5.3, and everything named in the "Required addition" table) is correctly `in-progress` or
  `backlog`, never `done`.
- **The one previously-fixed register/spine text mismatch (LR-AUDIT-PROTECTION-DELETION DeletionRequest
  symmetry) is still correctly fixed.**
- **One new, minor traceability gap found (M1):** `sprint-status.yaml`'s Story 5.3 line lacks the kind of
  inline annotation Story 5.2's line has, even though a completed, passing prior implementation exists
  underneath it. Recommend adding one comment line; does not block ratification and is not a false-done
  defect.

## Summary Table

| # | Severity | AD/Section | Status | One-liner |
|---|---|---|---|---|
| — | Critical | — | none | No critical brownfield-drift findings this round |
| H1 | High | AD-2 | already-tracked, unchanged | Tenant-scoped `ProviderCatalog`; no platform `system` catalog / `TenantProviderEnablement` split |
| H2 | High | AD-4 | already-tracked, unchanged | `ConfigurationVersion` still not bumped by `AgentActivated`/`AgentDisabled` (DW-4); Story 5.2 correctly stays `in-progress` |
| H3 | High | AD-29 | already-tracked, unchanged | No shared `AgentsIdentity` canonicalizer |
| H4 | High | Structural Seed | already-tracked, unchanged | `Aggregates/`, `Application/Tools/` empty; `IntegrationTests` project absent (owned by 5.6) |
| H5 | High | AD-30 | already-tracked, unchanged | No HMAC-tagged extensions; no `Agents.PlatformOperator` policy |
| H6 | High | AD-7 | already-tracked, unchanged | Link/Replace Party-identity commands still succeed live, contradicting the "retired" claim — but PRD + epics.md both already name this as Story 5.2's unmet AC |
| H7 | High | AD-17 | already-tracked, unchanged | Legacy `EnableProductionLikeGeneration` gate can enable generation with zero reference to `LaunchReadinessGate`/register/`RQ-1`; PRD's own validation report already classifies this identically; Story 5.7 (`backlog`) is the named fix |
| M1 | Medium | tracking | **new, minor** | `sprint-status.yaml` Story 5.3 `backlog` line lacks an inline annotation explaining the completed prior implementation it supersedes (safe-direction gap, easy fix) |
| M2 | Medium | registers | confirmed consistent | LR-AUDIT-PROTECTION-DELETION DeletionRequest-symmetry fix still holds; no other register/spine mismatch found |
| L1 | Low | AD-16/AD-2/AD-18 | confirmed | AppHost-free claim verified by a passing test run; aggregate-list and Dapr-Workflow gaps are the pre-authorized known debt |
| L2 | Low | Epics 1–4 | confirmed | "done" is explicitly historical-only per `epics.md`'s own reconciliation language, not a live-conformance claim |

## Files/Commands Consulted

- `ARCHITECTURE-SPINE.md` (full, 798 lines)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (full)
- `_bmad-output/planning-artifacts/external-dependency-register.md` (full)
- `_bmad-output/planning-artifacts/launch-readiness-register.md` (full)
- `_bmad-output/implementation-artifacts/deferred-work.md` (DW-4/DW-5 entries)
- `_bmad-output/planning-artifacts/epics.md` (Epic 5 stories, 2026-09-09 reconciliation table, Story 5.7/5.10)
- `_bmad-output/implementation-artifacts/spec-5-3-govern-provider-models-and-pricing-through-live-operations.md`
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md` (FR-23 register references)
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/validation-report-2026-09-09.md`
  (`EnableProductionLikeGeneration` classification)
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/.memlog.md` (full, round-3/4
  history)
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/reviews/review-2026-09-09-
  brownfield-drift-v6.md` (full, for context on what was already found — re-derived, not trusted)
- `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
- `src/Hexalith.Agents/Agent/AgentAggregate.cs`
- `src/Hexalith.Agents/Agent/AgentLaunchReadinessPolicy.cs`
- `test/Hexalith.Agents.Server.Tests/ForbiddenHostingOwnershipTests.cs` (executed: 5/5 pass)
- `git log -1 --format='%ai %H %s' -- src/`, `git diff a191d24 606712f -- src/ test/`, `git diff f691486 HEAD
  -- ARCHITECTURE-SPINE.md`, `find src -maxdepth 3 -type d`, various targeted `grep -rln`
