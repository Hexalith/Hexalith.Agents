# Architecture Reviewer Gate - Rubric Walker

- **Reviewed:** `ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`, `status: final`, altitude `initiative`, purpose `build-substrate`; file revision md5 `60df557cce2c6a1a851e85547c6aa26d`, mtime 2026-09-09 02:10:56 +0200)
- **Date:** 2026-09-09
- **Lens:** BMad good-spine checklist, nine items, independent re-walk; the 2026-09-08 rubric review was read afterwards for regression comparison only
- **Inputs:** spine, `.memlog.md` (updated 02:01), `IMPLEMENTATION-CONVENTIONS.md`, `launch-readiness-register.md` (2026-09-09), `external-dependency-register.md` (2026-09-09), PRD (FR-1..FR-33, NFR-1..NFR-14, OQ-1..OQ-23, section 8 and 8.1), `epics.md` story index, UX `EXPERIENCE.md` component table, `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, `global.json`, root and imported `Directory.Packages.props`, sibling `global.json` files, `git submodule status`, the `src/` and `test/` trees, `StructuralSeedConformanceTests.cs`, `AgentsOperationEndpoints.cs`, `ProviderCatalogAggregate.cs`, `AgentInteractionGenerationOrchestrator.cs`, and an empirical mermaid 11 parse of all four diagram blocks
- **Not done here:** web verification of named technology (item 4 is noted only, per the gate split)

## Gate Verdict

**PASS WITH FINDINGS.** No critical finding. Two high findings: the spine's own architecture assumptions have no index and are invisible to the `UnretiredAssumption` release blocker AD-17 defines, so `RQ-1` can record READY while the AD-30 authorization model is still tagged hypothetical; and AD-2 assigns proposal-expiry duration and the regeneration ceiling to `TenantGovernancePolicy` while the PRD (FR-33, OQ-3) and the binding UX spine put them on the `Agent` under `AgentSetupMutation`, which is exactly the two-owners-for-one-entity divergence AD-2 exists to prevent. Everything the 2026-09-08 review asked for was applied; the sequence diagram had a mermaid parse failure in the 02:07 revision that was corrected at 02:10:56 during this gate run.

## Deterministic Gate

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py \
  --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `{"ok": true, "total_findings": 0}`.

Mermaid (mermaid 11 under jsdom, `mermaid.parse`): block 1 `flowchart LR` OK, block 2 `flowchart TB` OK, block 3 `sequenceDiagram` OK, block 4 `classDiagram` OK, against the current revision. See R-17 for the revision that failed.

## Tiered Findings

### Critical

None.

### High

#### R-1 - The spine's architecture assumptions have no index and cannot block `RQ-1`

- **Where:** AD-17 Rule ("records an `UnretiredAssumption` blocker for every unretired Product, Architecture, or Governance assumption in PRD section 8.1"); `[ASSUMPTION]` tags in AD-27 (7-day purge window, no id), AD-30 (caller `PartyId` ingress resolution, "pending platform identity confirmation"), Stack xUnit row (Story 5.6 aligns the test stack); memlog line 128 lists seven open spine assumptions, of which only three carry a tag in the spine. The register says the `Platform` scope of `LR-UI-CONFORMANCE`, `LR-RUNTIME-PERFORMANCE`, and `LR-UI-PERFORMANCE` "is an architecture assumption (spine AD-17, 2026-09-09)", but AD-17 carries no tag for it; the Story 5.5 catalog-migration owner, the Story 5.5 route-prefix fix, and the `Unreconciled` period-close settlement are assumptions in the memlog only.
- **Problem:** PRD FR-28 says "READY cannot be recorded while the authorization model or a launch threshold is still hypothetical", and AD-17 implements that by scanning PRD section 8.1. The spine's own assumptions are outside that set. The AD-30 ingress `PartyId` resolution is the authorization model's root (every `User` principal depends on it) and it is tagged hypothetical, yet no register record, blocker code, or index can name it. Two units diverge on it trivially: the Release Operator evaluating `RQ-1` reads 8.1 and sees nothing; the Story 6.1 implementer reads AD-30 and treats the tag as decided.
- **Why it matters:** This is a release-gate hole in the invariant the spine itself declares, and it also breaks retirement: an assumption with no id and no owner has no "retired when" condition, so the tag can stay forever without anyone being wrong.
- **Disposition:** **Autofix.** Add a section `## Architecture Assumptions` (after Deferred Beyond V1) with rows `AA-n | Assumption | Where | Owner | Retired when` for: AA-1 workflow purge window 7 days (AD-27, Architecture + Platform Maintainer, retired when Platform confirms the state-store retention); AA-2 ingress caller `PartyId` resolution through Parties + Tenants membership (AD-30, Platform Maintainer, retired when `EXT-HOST-1` names the identity seam); AA-3 `Platform` scope of the three UI/runtime gates (AD-17, Product + Release PM, retired when the register row is confirmed); AA-4 root test-stack overrides until Story 5.6 (Stack, Agents Runtime Maintainer); AA-5 Story 5.5 owns the `system`-catalog migration and the `/api/v1/agents` route fix (Structural Seed / Errors convention, Architecture); AA-6 `Unreconciled` reservations settle `ChargedAtMaximum` at period close (AD-21, Architecture + Product). Replace every bare `[ASSUMPTION]` with `[ASSUMPTION AA-n]`. Amend the AD-17 sentence to: "…records an `UnretiredAssumption` blocker for every unretired assumption in PRD section 8.1 or in this spine's Architecture Assumptions index whose owner is Product, Architecture, Platform, or Governance."

#### R-2 - Proposal-expiry duration and the regeneration ceiling have two owners

- **Where:** AD-2 Rule: `TenantGovernancePolicy` (`TenantId`; "cost caps, rate limits, regeneration ceiling, expiry defaults, tenant safety restrictions, calling restriction, per-Conversation Agent blocks"); class diagram `Agent.ResponsePolicy` and `TenantGovernancePolicy.ExpiryDefaults`/`RegenerationCeiling`; Time convention ("default proposal lifetime is 24 hours, configurable from 1 hour through 30 days for future proposals" with no owner); PRD FR-33 row "Configure `hexa`: … proposal expiry duration, regeneration ceiling - Tenant Agent Administrator"; PRD OQ-3 "configurable per Agent"; UX `EXPERIENCE.md` `agent-config-form` ("Proposal expiry duration: default 24 hours … Regeneration ceiling per proposal: default 3 … other writes are `AgentSetupMutation`"); AD-21 ("the regeneration ceiling (default 3, range 1 to 10 [ASSUMPTION A-6]) … yield typed rejections" read from `TenantGovernancePolicy`).
- **Problem:** The PRD and the UX spine (binding launch authority under NFR-13) make both values per-Agent configuration written through `AgentSetupMutation` and snapshotted with the Agent's `ConfigurationVersion`; AD-2 makes them tenant governance written through whichever family governs `TenantGovernancePolicy` (the spine never says which: `PolicyPublication`, `TenantBudgetUpdate`, or an unnamed family) and snapshotted through the "safety version pair". Story 5.2/5.7 (Agent setup, already shipped against `Agent`) and Story 8.4 (governance policies) can each satisfy the spine literally and produce two ceilings, and the UI conformance suite (AD-25, "every interactive V1 route") will test the form the UX spine specifies, not the aggregate AD-2 names.
- **Why it matters:** AD-2's stated purpose is "two owners for one entity". The regeneration ceiling is a cost control that AD-21 enforces before reservation; if the value the UI writes is not the value AD-21 reads, the limit is silently unenforced. The word "defaults" in AD-2 may have intended a tenant default with a per-Agent override, but the spine never says so and `hexa` is one Agent per tenant, so an override layer is pure ambiguity.
- **Disposition:** **Autofix (AD-2 sentence plus class diagram).** In AD-2 move "regeneration ceiling, expiry defaults" out of `TenantGovernancePolicy` and add to `Agent`: "`Agent` (…; owns `ResponsePolicy` including proposal expiry window and regeneration ceiling, written as `AgentSetupMutation` by the Tenant Agent Administrator and pinned by the interaction snapshot's `ConfigurationVersion`)". Keep `TenantGovernancePolicy` to cost caps, rate limits, concurrent-interaction maximum, tenant safety restrictions, calling restriction, per-Conversation blocks, and the kill switch (see R-7), and name its write families: caps and limits `TenantBudgetUpdate`, restrictions and calling restriction `PolicyPublication`, blocks `AgentSetupMutation`, kill switch `TenantKillSwitch`. Update AD-21 to read the ceiling from the snapshot's Agent configuration. Remove `ExpiryDefaults` and `RegenerationCeiling` from the `TenantGovernancePolicy` class in the diagram.

### Medium

#### R-3 - Shipped non-conformant streams have no migration or schema-evolution rule

- **Where:** AD-2/AD-10 (`ProviderCatalog` under reserved tenant `system`, `TenantProviderEnablement`); AD-29 (`AttemptId` derivation); Errors/API conventions (`/api/v1/agents/...`); AD-22 ("EventStore history is never rewritten"); memlog lines 91, 97, 115 (shipped `ProviderCatalogAggregate` keys the stream by tenant id; shipped `AgentInteractionGenerationOrchestrator` derives `attempt-` + interaction id; shipped endpoints map `/api/agents/operations`; "[ASSUMPTION] Story 5.5 owns the migration"). Verified in code: `ProviderCatalogAggregate.cs` uses `envelope.AggregateId` as the catalog id; `AgentInteractionGenerationOrchestrator.cs` line 100 `DeriveAttemptId(request.AgentInteractionId)`; `AgentsOperationEndpoints.cs` line 22 `MapGroup("/api/agents/operations")`.
- **Problem:** The spine now contradicts three shipped shapes on purpose and (correctly, after 2026-09-08 R-4) carries no status notes, but it also carries no rule for how a shipped stream, event, or route reaches the amended shape. Story 5.5 can re-key the catalog by replaying tenant streams into `system` streams, by dual-reading, or by rewriting; Story 6.4 can bump `AttemptOrdinal` semantics for in-flight interactions or not; nothing prevents a rewrite except an AD-22 sentence written for audit history. Event payload evolution (adding fields, `Unknown = 0` on enums) is bound for public contracts by FR-23 but not for durable events and state snapshots.
- **Why it matters:** A migration is the one place where "history is never rewritten" and "the aggregate key is `system`" collide, and the spine leaves the collision to the story.
- **Disposition:** **Autofix (one convention row).** "Schema and stream evolution: durable events and state snapshots evolve additively under the FR-23 rules; a re-keyed or re-scoped aggregate is migrated by replaying the old streams into new streams under the new key with a `MigratedFrom` reference, the old streams are frozen read-only, never rewritten or deleted before their retention expires, and the story that changes the key owns the migration and its idempotent re-run. In-flight interactions keep their snapshot identities; a new derivation rule applies to interactions requested after the change." Move the memlog's "Story 5.5 owns the migration" into the Architecture Assumptions index (R-1).

#### R-4 - The prerequisites table copies register columns the spine says it does not track, and the `EXT-CONV-AI-1` consumer set is incomplete against the six seams AD-6 binds

- **Where:** External V1 Prerequisites ("commitment status … is read from it at evaluation time; the spine does not track it") followed by a table whose Consumers column repeats the register's `ConsumingStories`; Stack row `Hexalith.Platform@a66cdf34` (the register's `TargetVersionOrCommit`); AD-6 (six seams: membership and removal, posting, Facilitator resolution, active-Conversation count, tenant-scoped content/roster/existence reads, deletion signal); register `EXT-CONV-AI-1` `ConsumingStories = 6.6, 7.4; RQ-1`.
- **Problem:** Story 6.2 consumes seam 5 (complete authorized reads, AD-11), Story 5.4/7.1 consume seam 3 (Facilitator resolution, AD-8), Story 8.3 consumes seam 6 (deletion signal, AD-22), and Story 8.5 consumes seam 4 (SM-2 denominator, AD-28) - none is listed, so under FR-21 those stories are not blocked by an `Uncommitted` record that AD-6 says blocks them. The spine's copy inherits the gap and adds a second place to edit.
- **Why it matters:** The spine's rule is stronger than the register's record; a story writer reading the register alone proceeds. Two sources for one column is the R-3 (2026-09-08) pattern in miniature.
- **Disposition:** **Autofix.** Drop the Consumers column from the spine table (keep Dependency and Governing decisions) and drop the commit from the Stack hosting row ("platform-owned through `EXT-HOST-1`; target pinned in the register"). Raise a register correction through the sprint-change path: `EXT-CONV-AI-1` consumers become `5.4, 6.2, 6.6, 7.1, 7.4, 8.3, 8.5; RQ-1`.

#### R-5 - The runtime sequence diagram orders acceptance differently from FR-8 and AD-13

- **Where:** Sequence diagram lines "Workflow->>Conv: authorized complete Conversation read (inside activity)" then "Workflow->>Interaction: rate limits, lifecycle, enablement, block, kill switch re-read" then "Workflow->>Ledger: reserve"; AD-13 ("Acceptance follows the FR-8 order (authorization, lifecycle, provider eligibility, rate limits, context measurement, reservation, pre-Provider safety, Eligible Approver resolution, membership)"); PRD FR-8 steps 1-9.
- **Problem:** The diagram performs the Conversations read (FR-8 step 5) before lifecycle (step 2) and rate limits (step 4), and shows no provider-eligibility step (step 3) at all. A rate-limited or lifecycle-blocked caller therefore still costs a full tenant-scoped Conversations read, and a workflow author following the picture (the spine says shape lives in diagrams) builds the activity chain in that order while the AD-13 text and the `AgentCallAcceptance` gate order say otherwise.
- **Why it matters:** Same class as 2026-09-08 R-8: two normative shapes for one protocol. FR-8's order is testable ("accepted only after every pre-Provider check passes, in this order").
- **Disposition:** **Autofix (diagram).** Reorder to: `Workflow->>Interaction: lifecycle, enablement, block, kill switch re-read` → `Workflow->>Interaction: provider eligibility (AD-10 floor)` → `Workflow->>Interaction: rate limits and concurrent-interaction maximum` → `Workflow->>Conv: authorized complete Conversation read + exact measurement (inside activity)` → `Workflow->>Ledger: reserve` → safety → approver → membership.

#### R-6 - AD-30 lets a `Workflow` principal dispatch "readiness evidence", which AD-17's producer rule rejects

- **Where:** AD-30 ("`Workflow` may dispatch only `AgentInteraction` lifecycle-result commands, `BudgetLedger` reserve and settle, and readiness evidence for its own interaction"); AD-17 ("Submissions from a principal lacking the gate's `AuthorizedProducer` role are rejected before append"); register Gate Scope Kinds table (producers are Platform Maintainer or Release Operator only); AD-28 (`runtime-metrics` derives from commit timestamps of named events, not from submissions).
- **Problem:** A `Workflow` principal holds no FR-33 role, so any `LaunchReadinessGate` command it dispatches is rejected by AD-17; the allowance is either dead text or an invitation to grant the workflow a producer role. If "readiness evidence" meant the NFR-9 source observations, AD-28 already says those are derived, not submitted.
- **Why it matters:** Two ADs on the trusted-envelope allowlist disagree; Story 6.1 (workflow activities) and Story 8.7 (evidence inspection) resolve it differently.
- **Disposition:** **Autofix.** Replace "and readiness evidence for its own interaction" with "and nothing else; readiness observations are submitted only by the register's `AuthorizedProducer` roles, and runtime latency derives from the interaction's own events under AD-28".

#### R-7 - The kill switch has an owner in the class diagram but not in AD-2

- **Where:** Class diagram `TenantGovernancePolicy.KillSwitch`; AD-2 `TenantGovernancePolicy` list (no kill switch); AD-1 ("operational status"); AD-4 and AD-12 ("the per-tenant kill switch … never frozen: every side-effecting step re-reads them"; "`agent-setup` and `launch-readiness` expose the stop state"); AD-12 family `TenantKillSwitch`.
- **Problem:** AD-12 requires every side-effecting step to re-read the switch and forbids "side effects on stale projections", so the re-read must target an aggregate at an expected revision; the Rule text of the only AD that assigns aggregate ownership does not name one. The diagram's answer (`TenantGovernancePolicy`) is plausible but a diagram attribute is not a rule.
- **Disposition:** **Autofix.** Add "kill-switch state" to the `TenantGovernancePolicy` clause in AD-2 (folded into the R-2 rewrite) and state in AD-12 that the re-read is of `TenantGovernancePolicy` at an expected revision, with `agent-setup` and `launch-readiness` as display projections only.

### Low

#### R-8 - AD-21 "before … authorization" is ambiguous against AD-12

- **Where:** AD-21 ("After context measurement and before safety, admission, and authorization, one atomic `BudgetLedger` command reserves…"); AD-12 ("Authorization gates run before every side effect"); Cost convention row ("precedes safety, admission, and Provider authorization").
- **Problem:** A reservation is a side effect; read as caller authorization the sentence contradicts AD-12 and FR-8 step 1. The convention row shows the intended meaning.
- **Disposition:** **Autofix.** "…before safety, admission, and Provider authorization (AD-13 step 4)…".

#### R-9 - `TenantId` of platform-scoped aggregates is stated for two of three

- **Where:** AD-2 ("`TenantId` is a mandatory component of every aggregate identity"; `ProviderCatalog` and `ContentSafetyPolicy` under `system`; `LaunchReadinessGate` keyed by `GateId`, `TenantScope`, `EnvironmentProfile` with no tenant named); AD-17 (`TenantScope` grammar `tenant:<TenantId>` or `platform`).
- **Disposition:** **Autofix.** In AD-2: "`LaunchReadinessGate` (`TenantId` = the scoped tenant for `tenant:<id>` records and `system` for `platform` records; `GateId`, `TenantScope`, `EnvironmentProfile`)".

#### R-10 - The Trusted-verdicts convention introduces `*:validation` extensions and the shipped `actor:*` names that AD-30 does not enumerate

- **Where:** Trusted verdicts row ("reserved `actor:*` and `*:validation` envelope extensions under AD-30"); AD-30 (names only `actor:*`, "Each reserved extension is bound to an allowlisted command set"); shipped code uses `actor:agentsAdmin`, `actor:agentsProviderAdmin`, `actor:globalAdmin` (memlog says ratified as `Administrator` extensions; the spine does not say which maps to `Platform`).
- **Disposition:** **Autofix.** In AD-30 add: "Reserved namespaces are `actor:*` (principal) and `*:validation` (dependency verdicts); `actor:globalAdmin` carries the `Platform` principal, `actor:agentsAdmin` and `actor:agentsProviderAdmin` carry `Administrator`; any other reserved key is rejected."

#### R-11 - Story and schedule references inside rules; one title tag survived

- **Where:** AD-15 ("owned by Stories 5.5 and 5.7"), AD-31 ("removed before Story 6.7 closes"), AD-17 (`Hexalith.Agents.IntegrationTests` "the story that flips a seam"), Structural Seed ("created by Story 5.6"), Stack ("siblings are on `10.0.400`", "until Story 5.6 aligns"), AD-1 title `[ADOPTED]` (2026-09-08 R-11 asked for all tags to go; memlog line 113 says they went).
- **Problem:** None of these is false today, but each is a schedule fact that will be, which is how the 2026-09-08 R-4 stale notes were born. AD-17's reference is fine (it binds the project name, not a story).
- **Disposition:** **Autofix.** Strip `[ADOPTED]`; rephrase AD-15 to "before its route ships"; AD-31 to "before the `EXT-CONV-UI-1` seam goes live"; seed comment to "created at the first live seam binding"; Stack to "accepted deviation until aligned with the catalog (AA-4)"; drop "siblings are on `10.0.400`" (verified-current material for the memlog).

#### R-12 - NFR-1..NFR-9 appear in no AD Binds line

- **Where:** frontmatter `binds: PRD NFR-1..NFR-14`; Binds lines name NFR-10..NFR-14 and "data governance NFRs" only.
- **Problem:** Coverage exists (see item 6) but is not declared; a coverage tool keyed on Binds reports nine gaps.
- **Disposition:** **Autofix.** Add NFR-1/NFR-2 to AD-12, NFR-3 to AD-13, NFR-4 to AD-17, NFR-5 to AD-22, NFR-6 to AD-9, NFR-7 to AD-20, NFR-8 to AD-11, NFR-9 to AD-28.

#### R-13 - AD-5 system-abandonment reasons mix identifiers and prose

- **Where:** AD-5 ("`NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`, kill switch").
- **Disposition:** **Autofix.** "…`RemovedInConversations`, `TenantKillSwitch`)" so the reason code matches the family name.

#### R-14 - Notes for other lenses (no spine change requested here)

- **Register drift:** `LR-PRODUCT-METRICS` and the `product-metrics` projection say SM-1 through SM-6; the PRD now has SM-7 (OQ-11, OQ-19) and OQ-22 splits gate metrics from launch-health metrics. AD-17 and AD-28 defer to the register's measurement contracts, so the register must add SM-7 and the split.
- **Epics drift:** Story 8.4 names `TenantBudgetPolicyAggregateTests`; the spine has `BudgetLedger` and `TenantGovernancePolicy`. Story 6.7 wording about "no new external seam" was flagged in 2026-09-08 R-2 and is now contradicted by AD-31.
- **Brownfield (code lens):** `test/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs` still requires `Aggregates`, `Application/Tools`, `Application/Workflows`, `Application/Activities` under `Server`; the seed dropped `Aggregates` and `Tools`, and AD-19 forbids a reserved tools folder. `src/Hexalith.Agents.Server/Application/Tools/` exists (empty). The test contradicts the spine and will keep passing while the tree is non-conformant. Memlog line 110 already says the test must change; no story is named.
- **Verified-current (item 4):** every Stack pin re-checked against the checked-in files matches (`global.json` 10.0.301 latestPatch; siblings 10.0.400; imported catalog Dapr.Client/Dapr.Workflow 1.18.5, MediatR 14.2.0, FluentValidation 12.1.1, OpenTelemetry 1.18.0, Fluent UI 5.0.0-rc.5-26219.1, xunit.v3 4.0.0, NSubstitute 6.2.0; root overrides xunit.v3 3.2.2, NSubstitute 5.3.0, Shouldly 4.3.0, bunit 2.9.0; gitlinks EventStore `1b6f08d4`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `54fc4040`, FrontComposer `053b2008`). Web-facing claims to verify: AD-18 "Dapr 1.18 never re-creates a non-terminal instance id"; Agent Framework 1.20.0 GA; Fluent UI rc.5 as latest.
- **Mermaid authoring gotcha:** a `;` inside a `sequenceDiagram` message terminates the statement (empirically confirmed with mermaid 11: `A->>B: hello; world` fails). See R-17.

#### R-15 - Altitude note (no action)

AD-13 (fingerprint field inventory, six-step protocol), AD-21, AD-22, and AD-29 (hash recipes) remain dense. Each enumerated element is itself a divergence point (which fields invalidate a retry; which tuple derives an id), so the density is earned; AD-17 is now invariants plus register pointers and every pointer resolves. No change requested.

#### R-16 - `EXT-HOST-1` and `EXT-TOPOLOGY-1` boundaries are stated once each; Deferred row 3 is safe

Residency "inherited from the platform host and the catalog entry's `ProcessingRegion` flag": `ProcessingRegion` is an optional safe capability flag in AD-10 and gates nothing, so no unit can build a residency control from it. Safe as deferred. No action.

#### R-17 - Revision note: the 02:07 revision's sequence diagram did not parse

- **Where:** Sequence diagram lines 358, 382, 389: "safe blocked response; no interaction or Provider call", "RecordCapacityBlocked; no Provider call" (twice) in the revision read at the start of this walk (mtime 02:07); the file was changed at 02:10:56 to use `,` on exactly those three lines (diff of the persisted first read against the current file shows no other change).
- **Problem:** mermaid's sequence lexer ends message text at `;`, so the 02:07 revision failed with a parse error; the current revision parses.
- **Disposition:** **Ignore (already fixed).** Recorded so the memlog can attribute the fix; the diagram was introduced by the 2026-09-09 redraw (2026-09-08 R-8), so this was a regression inside the update run, not a carried defect.

## Good-Spine Checklist Walk

### 1. Fixes the real divergence points for the level below and misses none

**Verdict: Conditional pass.** Fixed since 2026-09-08: full aggregate inventory with keys and tenant scope (AD-2), reverse UI seam (AD-31), register altitude (AD-10/17/23/24/25/26 point to named sections that all resolve), time authorities (AD-28), identity derivation (AD-29), principals (AD-30), execution-state content (AD-27). Remaining divergence pairs: expiry/ceiling owner between Agent setup and governance stories (R-2); migration mechanism between Story 5.5 and the catalog consumers (R-3); `EXT-CONV-AI-1` consumers between reads/Facilitator/deletion stories and the register (R-4); acceptance order between the diagram and AD-13 (R-5); kill-switch owner (R-7).

### 2. Every AD's Rule is enforceable and prevents its stated divergence

**Verdict: Conditional pass.** Binds/Prevents/Rule are coherent and enforceable for AD-1, AD-3..AD-11, AD-13..AD-16, AD-18..AD-20, AD-22..AD-29, AD-31. Flags: AD-2 creates a second owner for expiry/ceiling (R-2) and omits the kill switch (R-7); AD-17's `UnretiredAssumption` scan cannot see spine assumptions (R-1); AD-30's `Workflow` allowlist contains a clause AD-17 rejects (R-6); AD-21's "authorization" is ambiguous (R-8); AD-12/AD-15/AD-31 carry schedule words (R-11). AD-17's "Tests cover …" sentence remains a subject list rather than a rule; acceptable as a floor.

### 3. Nothing under Deferred could let two units diverge

**Verdict: Pass.** Six rows. Owner field (AD-8 now has no escape hatch; adoption needs an amendment and a new `ApproverPolicyVersion`); two-person rule (AD-22 binds single-actor with justification and hold interlock, so V1 has one behavior); residency (R-16); Dapr Conversation API (excluded by AD-18/19); retraction metric (needs a seam `EXT-CONV-AI-1` does not name; SM-C4 covers the interval); memory/tools/MCP/A2A/multiple Agents (hard-excluded by AD-19 and OQ-13/OQ-20). Every row names what it will not decide and why the wait is safe.

### 4. Named technology is verified-current

**Verdict: Consistent with the checked-in files (see R-14); web currency not verified here.** `Provider SDK` and `Agent Framework SDK` = `Unselected` remain correct while `EXT-PROVIDER-1` is `Uncommitted`. Aspire/CommunityToolkit rows correctly dropped under AD-16.

### 5. Ratifies rather than contradicts the brownfield codebase and platform conventions

**Verdict: Conditional pass.** Ratified: EventStore domain module and DomainService SDK host; no module-owned AppHost/Aspire/ServiceDefaults (tree confirms six `src/` projects, none of the three); FrontComposer + Fluent UI V5; xUnit v3/Shouldly/NSubstitute; `.slnx`; CPM through the imported `Hexalith.Builds` catalog; ULID identifiers; sibling module shape declared as an accepted deviation from the per-layer reference layout with the boundary rule and aggregate-organised tests carried over (Package layout row); domain assembly `Hexalith.Agents` with `InternalsVisibleTo` `Hexalith.Agents.Tests` and `Hexalith.Agents.Server` only (csproj confirms; matches the companion); `/api/v1/...` prefix matches the sibling Conversations `MapGroup("/api/v1/conversations")`; shipped policy names `Agents.Administrator`, `Agents.Approver`, `Agents.AuditOperator`, `Agents.Operator` match AD-30 (`Agents.PlatformOperator` is not yet in code, which is expected). Deliberate contradictions with shipped code, all recorded in the memlog with assumed owners but with no migration rule in the spine: tenant-keyed `ProviderCatalog`, `attempt-` + interaction id, `/api/agents/operations` (R-3). One test contradicts the seed (R-14 brownfield note).

### 6. Covers the driving spec's capabilities

**Verdict: Pass with declared-coverage gaps (R-12) and two ownership gaps (R-2, R-7).**

| Requirement | Binding ADs | Note |
| --- | --- | --- |
| FR-1..FR-3 | AD-1, AD-2, AD-4, AD-7, AD-15, AD-30 | |
| FR-4, FR-5 | AD-2, AD-9, AD-10, AD-14, AD-21 | |
| FR-6, FR-7 | AD-2, AD-4, AD-8 | expiry/ceiling owner - R-2 |
| FR-8 | AD-6, AD-11, AD-12, AD-13, AD-31 | diagram order - R-5 |
| FR-9, FR-10 | AD-10, AD-11, AD-13, AD-20, AD-21, AD-24 | |
| FR-11, FR-12 | AD-6, AD-7, AD-13, AD-20, AD-31 | |
| FR-13..FR-18 | AD-4, AD-5, AD-8, AD-13, AD-18, AD-28, Time row | |
| FR-19..FR-21 | AD-2, AD-10, AD-12, AD-29, AD-30 | |
| FR-22, FR-23 | AD-14, AD-15, AD-17, AD-25, AD-26, AD-31, API row | |
| FR-24, FR-25 | AD-1, AD-4, AD-5, AD-13, AD-14, AD-17, AD-22, AD-23 | |
| FR-26, FR-27 | AD-2 (`ContentSafetyPolicy`), AD-20 | |
| FR-28 | AD-12, AD-17, AD-20..AD-24, AD-26 | kill-switch owner - R-7; spine assumptions - R-1 |
| FR-29 | AD-15, Trusted verdicts row | |
| FR-30 | AD-2, AD-6, AD-22 | |
| FR-31 | AD-11, AD-20 | |
| FR-32 | AD-2, AD-21 | |
| FR-33 | AD-2, AD-7, AD-12, AD-15, AD-22, AD-30 | |
| NFR-1..NFR-9 | AD-12, AD-13, AD-17, AD-22, AD-9, AD-20, AD-11, AD-28 (respectively) | not declared in Binds - R-12 |
| NFR-10..NFR-14 | AD-21, AD-23, AD-24, AD-25, AD-26 | |
| OQ-1 | AD-31 | |
| OQ-2, OQ-3, OQ-5..OQ-10, OQ-12..OQ-14, OQ-16, OQ-17, OQ-19..OQ-22 | AD-18; AD-5 + Time row; AD-28; AD-21; AD-10; AD-22; AD-20; AD-11; AD-11; Naming; AD-8; AD-7; prerequisites; AD-17; AD-2; AD-22; AD-17 | |
| OQ-4 | Notifications row | |
| OQ-11 | AD-28 (calculators) | SM-7 missing from the register - R-14 |
| OQ-15, OQ-18, OQ-23 | Deferred; AD-20 (single failing historical message blocks); Deferred | |

### 7. Parent-spine inheritance

**Verdict: N/A.** No parent declared. AD ids are stable across revisions; no later AD weakens an earlier one.

### 8. Every altitude-owned dimension is decided, deferred, or open

**Verdict: Pass with two partials.**

| Dimension | Status | Where |
| --- | --- | --- |
| Deployment and environments | Decided/gated | AD-16 (`EXT-HOST-1`, local/test/deployed), AD-17 `EnvironmentProfile` |
| Infra / provider strategy | Decided/gated | AD-9, AD-16, Stack `Unselected` rows |
| Operations / observability | Decided | AD-14, AD-16 (health, telemetry, secrets), Observability row, AD-12 kill switch, AD-17 projections |
| Backup / restore | Decided | AD-23 (EventStore only truth; rebuildables never restored past it; fences invalidated; one restore exercise in `LR-RECOVERY`) |
| Security / identity | Partially decided | AD-12, AD-14, AD-20, AD-30; ingress `PartyId` resolution is an untracked assumption - R-1 |
| Data lifecycle | Decided | AD-22, AD-27; migration unbound - R-3 |
| Testing strategy | Decided | AD-17 floor, live-seam matrix, conventions companion |
| UI composition | Decided | AD-15, AD-25, AD-31 |
| Versioning / compatibility | Decided | API and contract row; durable-event evolution - R-3 |
| Multi-tenancy | Decided | AD-2 (`TenantId` everywhere, `system` tenant); platform gate tenant - R-9 |
| Paradigm / decomposition | Decided | Design Paradigm, AD-1..AD-3; ownership drift - R-2, R-7 |
| Mutation / consistency / time | Decided | AD-3, AD-13, AD-28, conventions |
| External boundaries | Decided/gated | AD-6/7/9/11/20/31 + register |
| Cost / capacity / recovery | Decided | AD-21/23/24 |
| Beyond-V1 | Deferred safely | AD-19, Deferred table |

### 9. Spine hygiene

**Verdict: Pass with low findings.**
- **Altitude:** invariants, not spec restatement; register enumerations are now pointers (R-15 notes earned density).
- **Placeholders / template comments:** none; lint clean.
- **Stale status notes:** none dated; schedule words remain in five places and one title tag survived (R-11).
- **Diagrams:** all four parse (mermaid 11); the flowcharts carry the module and host boundary shape, the sequence carries the protocol (order defect R-5), the class diagram carries the aggregate inventory (ownership drift R-2/R-7).
- **Conventions vs ADs:** consistent except `*:validation` (R-10) and AD-21 wording (R-8); the Package layout row correctly cites `hexalith-llm-instructions.md`.
- **AD contradictions:** AD-30 vs AD-17 producer rule (R-6); AD-2 vs UX/PRD ownership (R-2).
- **Frontmatter:** `binds` correct; every `sources` path exists on disk (eighteen checked); `companions` resolves; `status: final` with `updated: 2026-09-09`.
- **Cross-references:** every AD number, register section name (Provider Readiness Contract, NFR-11 Recovery Evidence Contract, NFR-12 Capacity And Fairness Profile, NFR-13 UI Conformance Contract, NFR-14 Browser Monotonic Timing Contract), projection id (`agent-setup`, `provider-catalog`, `launch-readiness`, `browser-ui-metrics`, `runtime-metrics`, `product-metrics`, `export`, `workflow-execution-state`), gate id, operation family, `EXT-*` id, story number, and PRD assumption key cited in the spine resolves. `ObservationId` and `SampleId` recipes match the register verbatim.

## Regression Versus The 2026-09-08 Rubric Review

| 2026-09-08 | Status on 2026-09-09 | Evidence |
| --- | --- | --- |
| R-1 aggregate inventory | Closed | AD-2 names eleven aggregates with key and scope; ledger bounded by one reservation event per attempt |
| R-2 Call hexa seam | Closed | AD-31; `EXT-CONV-UI-1` in register with consumer 6.7 |
| R-3 register restated | Closed | AD-10/17/23/24/25/26 keep invariants and point to named register sections; AD-13 steps 3-5 collapsed to AD-24 |
| R-4 stale status notes | Closed, one remnant | no dated notes; `[ADOPTED]` on AD-1 remains (new R-11) |
| R-5 seed vs conventions | Closed in the spine | seed matches the tree and the companion; the conformance test still requires dropped folders (new R-14) |
| R-6 platform layout deviation | Closed | Package layout row; instruction file in `sources` |
| R-7 NFR-9 source | Closed | AD-28 |
| R-8 sequence diagram | Closed, then regressed and re-fixed | lease released after outcome; membership before both appends; `;` parse failure introduced and fixed within the run (new R-17); acceptance order remains off (new R-5) |
| R-9 catalog scope | Closed | `system` tenant, `TenantProviderEnablement`, `PlatformNotReady` |
| R-10 AD-8 escape hatch | Closed | conditional removed |
| R-11..R-13, R-16 | Closed | binds, paradigm, lock-bearing subset, Notifications row |
| R-14 versioning mechanism | Closed | API and contract versioning row |
| R-15 PartyId resolution | Closed as assumption | AD-30 `[ASSUMPTION]`; the assumption itself is untracked (new R-1) |
| R-17 verified-current | Closed | Stack re-pinned; all pins match the files |

New this revision: R-1 (assumption index), R-2 (expiry/ceiling owner), R-3 (migration), R-4 (copied register columns, consumer gap), R-5 (acceptance order), R-6 (Workflow readiness clause), R-7 (kill-switch owner), R-8..R-13 (low), R-17 (parse regression, fixed). No 2026-09-08 finding reopened.

## Disposition Summary

| ID | Severity | Where | Disposition |
| --- | --- | --- | --- |
| R-1 | High | AD-17, AD-27, AD-30, Stack, new Architecture Assumptions section | Autofix |
| R-2 | High | AD-2, AD-21, class diagram | Autofix |
| R-3 | Medium | new convention row; Architecture Assumptions | Autofix |
| R-4 | Medium | prerequisites table, Stack hosting row; register (sprint-change path) | Autofix spine; discuss register |
| R-5 | Medium | sequence diagram | Autofix |
| R-6 | Medium | AD-30 | Autofix |
| R-7 | Medium | AD-2, AD-12 | Autofix |
| R-8 | Low | AD-21 | Autofix |
| R-9 | Low | AD-2 | Autofix |
| R-10 | Low | AD-30 | Autofix |
| R-11 | Low | AD-1 title, AD-15, AD-31, seed, Stack | Autofix |
| R-12 | Low | Binds lines | Autofix |
| R-13 | Low | AD-5 | Autofix |
| R-14 | Info | register, epics, conformance test, web currency | Other lenses |
| R-15 | Info | AD-13, AD-21, AD-22, AD-29 | Ignore |
| R-16 | Info | Deferred row 3 | Ignore |
| R-17 | Info | sequence diagram revision | Ignore (fixed) |

After R-1 and R-2, plus the R-3..R-7 autofixes, no rubric item blocks handoff.
