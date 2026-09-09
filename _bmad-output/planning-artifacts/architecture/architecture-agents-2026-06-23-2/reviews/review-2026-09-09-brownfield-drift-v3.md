# Reviewer Gate — Brownfield Drift Review v3 (2026-09-09)

**Verdict: FAIL TO RATIFY, fixable.** The spine (`status: final`, `updated: 2026-09-09`) still describes a
target architecture that `src/Hexalith.Agents*` does not yet implement in several load-bearing places
(platform-scoped `ProviderCatalog`, `AgentsIdentity` canonicalizer, `ConfigurationVersion` on lifecycle
events, HMAC-tagged trusted extensions, structural seed). None of this is fixable by amending the spine's
prose — the gaps are real implementation debt, already routed to backlog stories (5.2, 5.3, 5.4, 5.6) by the
approved `sprint-change-proposal-2026-09-09.md`. What is new in this pass: (a) the sprint tracker itself now
asserts a "done" status for Story 5.2 that its own amended acceptance criteria and required test do not
satisfy in code — a tracking-integrity problem, not just code debt — and (b) three previously-uncalled-out
gaps (AD-19's tool-folder rule, AD-15's `IProjectionChangeDetailNotifier` nudge, AD-30's five-policy mapping).

**Method:** Read the full spine (758 lines) end to end. Cross-checked every concrete, checkable claim
(class/folder/file existence, version pins, submodule gitlink hashes, gate-id inventory, policy constants)
against the current `main` checkout. Read `review-2026-09-09-brownfield-drift-v2.md`,
`deferred-work.md`, `sprint-change-proposal-2026-09-09.md`, `sprint-status.yaml`, and `epics.md` to determine
tracked vs. untracked status. Confirmed via `git log` that no commit has touched `src/` since `a191d24`
(2026-09-08 16:37:53, "govern provider models and pricing through live operations") — i.e. code is
byte-identical to what v2 reviewed; only the confirmation method (fresh grep/read) is new.

---

## Critical

### C1 — AD-2 platform-scoped `ProviderCatalog` / `TenantProviderEnablement` — [already-tracked, still open]

**Spine claim (AD-2, line 109):** `ProviderCatalog` lives in reserved EventStore tenant `system`, one stream
per (`ProviderId`, `ModelId`); a separate `TenantProviderEnablement` (`TenantId`) aggregate owns which
platform entries a tenant may see/select.

**Code reality:**
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs:17` — doc comment: *"Pure, replay-safe aggregate for the **tenant-scoped** governed provider/model catalog (AD-2, AD-3, AD-9, AD-10)."* Authorization gate at lines 297-299 (`IsProviderAdmin`) is a raw string check on `envelope.Extensions["actor:agentsProviderAdmin"] == "true"` with no tenant/system split.
- `grep -rn "TenantProviderEnablement" src/ test/` → **zero hits**. The aggregate, commands, events, and projection do not exist anywhere in the tree.
- `src/Hexalith.Agents/` contains only `Agent/`, `AgentInteraction/`, `ProviderCatalog/` — none of the other eleven AD-2 aggregate folders exist (expected, their stories are backlog, but `TenantProviderEnablement` specifically is Story 5.3's twin and is the one the spine text repeatedly pairs with `ProviderCatalog`).

**Tracking:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09.md` (approved 2026-09-09) explicitly reopens Story 5.3 for this ("platform catalog migration plus `TenantProviderEnablement`"). `_bmad-output/implementation-artifacts/sprint-status.yaml:89` — `5-3-govern-provider-models-and-pricing-through-live-operations: backlog`. No commit has touched `src/Hexalith.Agents/ProviderCatalog/` since `a191d24` (2026-09-08 16:37), which predates the spine amendment/proposal. **Unchanged since `review-2026-09-09-brownfield-drift-v2.md` Finding 1.**

**Fix:** implementation debt — track in register (already tracked as Story 5.3 / `sprint-change-proposal-2026-09-09.md`). No spine amendment needed; the spine correctly states the target.

---

### C2 — AD-4 `ConfigurationVersion` on lifecycle events, AND the sprint tracker now falsely claims this is done — [already-tracked, still open; tracking-integrity aspect is NEW]

**Spine claim (AD-4, line 121):** *"`ConfigurationVersion` increments on every accepted `Agent` configuration
or lifecycle event."*

**Code reality:** `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/Agent/AgentAggregate.cs:267` — `DomainResult.Success([new AgentActivated(agentId)])`; line 298 — `DomainResult.Success([new AgentDisabled(agentId)])`. Neither event carries a `ConfigurationVersion` argument (contrast line 346 `AgentPartyIdentityLinked(agentId, command.PartyId, state.ConfigurationVersion + 1)`, which does). `AgentState.Apply(AgentActivated)`/`Apply(AgentDisabled)` likewise never touch `ConfigurationVersion`.

**What's new since v2:** `_bmad-output/planning-artifacts/epics.md:1247-1248` — Story 5.2's *current, approved* acceptance criteria (amended by `sprint-change-proposal-2026-09-09.md`, `execution_status: complete` in that proposal's own frontmatter) now explicitly require: *"`ConfigurationVersion` increments exactly once and the evolved `AgentActivated`/`AgentDisabled` schemas carry the new version... This closes DW-4"*, and name `AgentLifecycleConfigurationVersionTests` as required evidence (`epics.md:1263`). Yet:
- `_bmad-output/implementation-artifacts/sprint-status.yaml:88` — `5-2-configure-hexa-through-live-eventstore-operations: done`.
- `find test -iname "*LifecycleConfigurationVersion*"` → **no hits**; `grep -rl "AgentLifecycleConfigurationVersionTests" test/` → **no hits**.
- The code gap above is unchanged.

So the authoritative tracker (`sprint-status.yaml`) currently asserts a completed status for a story whose own current acceptance criteria are not met in code and whose named test does not exist. A reader trusting `sprint-status.yaml: done` (rather than re-deriving from `epics.md` + code) would wrongly conclude AD-4 is satisfied. `_bmad-output/implementation-artifacts/deferred-work.md` DW-4 (status: `open`) correctly keeps this open, so the two tracking artifacts disagree with each other, not just with the spine.

**Fix:** implementation debt — track in register. Additionally: `sprint-status.yaml` should not carry `5-2: done` while its own linked acceptance criteria (`epics.md:1247-1248`) and required test (`AgentLifecycleConfigurationVersionTests`) are unmet — either regenerate `sprint-status.yaml` to reopen 5.2 (or split off the DW-4 patch as its own trackable line) or land the code+test. This is a sprint-tracking correction, not a spine amendment.

---

## High

### H1 — AD-29 single `AgentsIdentity` canonicalizer — [already-tracked, still open]

**Spine claim (AD-29, line 271):** *"One shared `AgentsIdentity` canonicalizer... derives every deterministic id."*

**Code reality:** `grep -rln "AgentsIdentity" src/ test/` → zero hits. Five separate internal helper classes remain under `src/Hexalith.Agents.Server/Application/AgentInteractions/`: `AgentInteractionIdentity.cs`, `AgentProposalIdentity.cs`, `AgentProposalEditIdentity.cs`, `AgentProposalRegenerationIdentity.cs`, `AgentResponsePostingIdentity.cs`, plus `IAgentCommandIdentityFactory.cs` under `Ports/`. Unchanged from v2 Finding 3.

**Tracking:** `sprint-change-proposal-2026-09-09.md` §Technical Impact: *"All deterministic identity helpers converge on `AgentsIdentity`"* — assigned to Stories 7.1–7.3 ("use the shared AD-29 canonicalizer for proposal, generated, edited, regenerated version, and attempt identities") and 6.4 (attempt identities). All backlog.

**Fix:** implementation debt — track in register (already tracked, Stories 6.4/7.1-7.3).

---

### H2 — Structural seed: `Server/Aggregates`, `Application/Tools`, missing `Hexalith.Agents.IntegrationTests` — [already-tracked, still open]

**Spine claim (Structural Seed, lines 488-539):** no `Server/Aggregates` or `Application/Tools` folders listed; `test/Hexalith.Agents.IntegrationTests/` required, "created by Story 5.6."

**Code reality:**
- `src/Hexalith.Agents.Server/Aggregates/` exists, containing only `README.md`.
- `src/Hexalith.Agents.Server/Application/Tools/` exists, containing only `.gitkeep`.
- `find test -maxdepth 1 -type d` → `Hexalith.Agents.Tests`, `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests`, `Hexalith.Agents.Client.Tests`, `Hexalith.Agents.UI.Tests` — no `Hexalith.Agents.IntegrationTests`; also absent from `Hexalith.Agents.slnx`.

**Tracking:** `sprint-change-proposal-2026-09-09.md` §4.2 Story 5.6: *"create and add `test/Hexalith.Agents.IntegrationTests`... and remove `src/Hexalith.Agents.Server/Aggregates` plus `src/Hexalith.Agents.Server/Application/Tools` from source and `StructuralSeedConformanceTests`."* `sprint-status.yaml:92` — `5-6-...: backlog`. Unchanged from v2 Finding 4.

**Fix:** implementation debt — track in register (already tracked, Story 5.6).

---

### H3 — AD-30 HMAC-tagged trusted extensions vs. plain string-equality gate — [already-tracked, still open]

**Spine claim (AD-30, line 277):** *"Every reserved extension carries an HMAC tag issued to the Agents Server
principal through `EXT-SECRETS-1` and verified by the Server command pipeline before the aggregate, which
also rejects an untagged extension."*

**Code reality:** `ProviderCatalogAggregate.cs:297-299` — `IsProviderAdmin` is `envelope.Extensions?.TryGetValue(ProviderAdminExtensionKey, out string? value) == true && string.Equals(value, "true", ...)`. No HMAC, no tag verification. The aggregate's own doc comment (lines 22-27) calls this "transitional... Replace this gate when the Agents authorization model exists." The literal extension key name (`actor:agentsProviderAdmin`) now matches AD-30's prose exactly (a v2-noted lexical convergence), but the substantive HMAC requirement remains unimplemented.

**Tracking:** `sprint-change-proposal-2026-09-09.md` Story 5.4 ("HMAC-tagged trusted ingress... closes DW-2"), `sprint-status.yaml:90` — `5-4-...: backlog`. Unchanged from v2 Finding 5.

**Fix:** implementation debt — track in register (already tracked, Story 5.4 / DW-2).

---

## Medium

### M1 — AD-19 tool-folder rule directly contradicted by `Application/Tools/` — [new framing of already-tracked fact]

**Spine claim (AD-19, line 211):** *"V1 exposes no tools, MCP servers or clients, A2A agents, Python agent
workers, or remote-agent protocol surface, **and the source tree reserves no folder for them**."*

**Code reality:** `src/Hexalith.Agents.Server/Application/Tools/.gitkeep` exists on disk today. This is the
same underlying fact as H2 (structural seed), but v2's review filed it only against the seed table, not
against AD-19's own normative rule text. A reviewer checking AD-19 in isolation (without cross-referencing
the seed section) would independently find the codebase non-conformant with AD-19 itself, not just with the
seed listing. `grep -n "reserves no folder" ARCHITECTURE-SPINE.md` → line 211 only; no cross-reference to the
seed section or Story 5.6 exists next to AD-19's rule.

**Fix:** implementation debt — track in register (folded into Story 5.6's already-scoped folder removal). Consider a spine cross-reference from AD-19 to the Structural Seed section / Story 5.6 so the two do not read as independent claims.

### M2 — AD-15 `IProjectionChangeDetailNotifier` nudge not implemented; shipped polling is a bare timer — [new]

**Spine claim (AD-15, line 187):** *"The UI proves projection confirmation only by polling the authoritative
projection after an accepted write; `IProjectionChangeDetailNotifier` over the FrontComposer
projection-change channel is the only nudge, is filtered to the register's projection ids, triggers an
immediate poll, never replaces it, and Agents owns no hub."*

**Code reality:** `grep -rn "IProjectionChangeDetailNotifier\|ProjectionChangeDetail" src/ test/` → zero hits
outside compiled reference XML docs (`src/Hexalith.Agents.UI/bin/.../Hexalith.FrontComposer.Contracts.xml`,
not first-party code). The interface does exist upstream in
`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication/IProjectionChangeNotifier.cs:49`,
so it is available to consume, but Agents does not reference it anywhere. The shipped Story 5.2 polling
implementation (`src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:222-223,364,383`) is a bare
fixed-interval loop — `ProjectionPollingInterval = 250ms`, `ProjectionPollingTimeout = 5s`,
`await Task.Delay(ProjectionPollingInterval, TimeProvider, ...)` — with no notifier/hub integration of any
kind. `ProviderCatalog.razor` has the same shape.

**Tracking:** Not mentioned in `deferred-work.md`, `spec-setup-projection-polling.md`, or
`spec-5-2-enforce-complete-launch-readiness-before-callability.md` (checked all three for
"notifier"/"signalr"/"nudge" — no hits). DW-3 in `deferred-work.md` ("Optional post-write projection
polling") is marked `status: done 2026-09-08` and covers only the *fallback re-read*, not the notifier
requirement — so this specific AD-15 clause (the notifier as accelerant) appears genuinely untracked.

**Fix:** track as new implementation debt (candidate home: Story 5.6 or a new UI-polish follow-up alongside
DW-3's resolution) — the bare-poll behavior is functionally acceptable per AD-15 ("triggers an immediate
poll, never replaces it" — polling remains required), so this is a latency/UX gap, not a correctness gap, but
the spine's specific mechanism claim is currently false as a description of shipped code.

### M3 — AD-30's five-policy FR-33 mapping: `Agents.PlatformOperator` does not exist in code — [new]

**Spine claim (AD-30, line 277):** *"FR-33 roles map one-to-one to FrontComposer policies
`Agents.PlatformOperator` (Platform Operator), `Agents.Administrator` (Tenant Agent Administrator),
`Agents.Approver`, `Agents.AuditOperator` (Compliance Inspector), and `Agents.Operator` (Release Operator)."*

**Code reality:** `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs` defines exactly four
policy constants: `AgentsAdministratorPolicy = "Agents.Administrator"` (line 23), `AgentsApproverPolicy =
"Agents.Approver"` (line 35), `AgentsOperatorPolicy = "Agents.Operator"` (line 48),
`AgentsAuditOperatorPolicy = "Agents.AuditOperator"` (line 61). `grep -rn "Agents.PlatformOperator\|
PlatformOperatorPolicy" src/ test/` → zero hits anywhere in the tree. Plausibly expected at this stage — the
Platform Operator provider-catalog admin UI surface is part of the still-backlog Story 5.3/5.4/5.5 work — but
it is a specific, checkable overstatement in AD-30's "one-to-one" claim that no prior review flagged.

**Fix:** implementation debt — track in register (fold into Story 5.3/5.4/5.5's FrontComposer registration
scope, whichever ships the Platform Operator catalog UI). Not a spine error; the target state is correct.

---

## Low

### L1 — Correct / ratified claims confirmed this pass — [confirmed correct]

- Submodule gitlink hashes match the spine's `Stack` table exactly: `git submodule status` shows
  `73bcee6f...` Conversations, `1b6f08d4...` EventStore, `fa423985...` Parties, `54fc4040...` Tenants,
  `053b2008...` FrontComposer — all matching the pinned prefixes at spine lines 469-473.
- Version pins all match: `global.json` SDK `10.0.301`/`rollForward: latestPatch`; `Directory.Build.props`
  `TargetFramework net10.0`, `LangVersion 14`; `Directory.Packages.props` `xunit.v3 3.2.2`, `Shouldly 4.3.0`,
  `NSubstitute 5.3.0`, `bunit 2.9.0`, `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1`; Dapr
  `1.18.5` confirmed in the imported `references/Hexalith.Builds/Props/Directory.Packages.props` catalog.
- No module-owned AppHost/Aspire/ServiceDefaults project exists in `src/` (only a test file,
  `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs`) — consistent with AD-16.
- `launch-readiness-register.md` carries `OperationGateMatrixVersion = 2` (line 110), matching the spine and
  the approved sprint-change proposal's Story 5.7 requirement.
- All seven `LR-*` gate ids the spine cites (`LR-RECOVERY`, `LR-CAPACITY-FAIRNESS`, `LR-UI-CONFORMANCE`,
  `LR-UI-PERFORMANCE`, `LR-AUDIT-PROTECTION-DELETION`, `LR-TENANT-ACCESS`, `LR-PROVIDER`) are present in
  `launch-readiness-register.md`.
- AD-31's `/agents/conversation-call` harness (`src/Hexalith.Agents.UI/Components/Pages/ConversationCall.razor`)
  still exists and is still registered — expected and correct, since its removal deadline is "before Story
  6.7 closes" and Story 6.7 is backlog, not yet started. Not a finding.

### L2 — No further new undocumented drift found beyond Findings M1-M3 — [confirmed]

Extended the v2 review's scope (which covered AD-2/AD-4/AD-29/AD-30/structural-seed) into AD-15, AD-16,
AD-19, AD-30's policy list, the Stack table, and submodule pins. No additional contradictions found beyond
Critical/High items C1-C2, H1-H3, and Medium items M1-M3.

---

## Summary Table

| # | Severity | AD/Section | Status | One-liner |
|---|---|---|---|---|
| C1 | Critical | AD-2 | already-tracked, still open | Tenant-scoped `ProviderCatalog`; `TenantProviderEnablement` absent entirely |
| C2 | Critical | AD-4 | already-tracked, still open (tracking-integrity aspect new) | `ConfigurationVersion` not bumped by lifecycle events; `sprint-status.yaml` marks Story 5.2 "done" despite unmet amended AC and missing named test |
| H1 | High | AD-29 | already-tracked, still open | No `AgentsIdentity` canonicalizer; 5+ separate identity helpers |
| H2 | High | Structural Seed | already-tracked, still open | `Server/Aggregates`, `Application/Tools` present; `Hexalith.Agents.IntegrationTests` absent |
| H3 | High | AD-30 | already-tracked, still open | Plain string-equality provider-admin gate, no HMAC |
| M1 | Medium | AD-19 | new framing of tracked fact | AD-19's own "reserves no folder" rule directly contradicted by `Application/Tools/` |
| M2 | Medium | AD-15 | new | `IProjectionChangeDetailNotifier` unused anywhere; shipped polling is a bare timer loop |
| M3 | Medium | AD-30 | new | `Agents.PlatformOperator` policy constant does not exist anywhere in code |
| L1 | Low | multiple | confirmed correct | Submodule pins, version pins, no AppHost, register gate ids, matrix v2 all verified correct |
| L2 | Low | — | confirmed | No further new drift found beyond M1-M3 |

## Files Consulted

- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (full, 758 lines)
- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/reviews/review-2026-09-09-brownfield-drift-v2.md`
- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09.md`
- `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/deferred-work.md`
- `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/sprint-status.yaml`
- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/epics.md` (Story 5.2 section, lines ~1214-1263)
- `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/launch-readiness-register.md`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents/Agent/AgentAggregate.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Application/AgentInteractions/*.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Aggregates/README.md`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.Server/Application/Tools/.gitkeep`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor`
- `/home/administrator/projects/hexalith/agents/src/Hexalith.Agents.UI/Components/Pages/ConversationCall.razor`
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication/IProjectionChangeNotifier.cs`
- `references/Hexalith.Builds/Props/Directory.Packages.props`
- `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `Hexalith.Agents.slnx`
- `git submodule status`, `git log --oneline -- src/ test/`, `find src test -maxdepth 3 -type d`
