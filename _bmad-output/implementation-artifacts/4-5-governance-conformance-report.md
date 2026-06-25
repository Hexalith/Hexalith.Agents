# Agents V1 — Governance & Release-Readiness Conformance Report (Story 4.5)

**Status:** Implementation-time conformance evidence (distinct from the planning-time `implementation-readiness-report-2026-06-23.md`).
**Date:** 2026-06-25
**Scope:** `Hexalith.Agents` module (`hexa` V1). Every Functional Requirement (FR-1..28), Non-Functional Requirement (NFR-1..10), Architecture Decision (AD-1..19), and UX Design Requirement (UX-DR1..41) is mapped to at least one owning story and a verification path. Unresolved governance decisions are recorded as **explicit launch blockers**, never hidden assumptions (AC4).

This report is backed by a machine-checkable manifest (`tests/Hexalith.Agents.Server.Tests/Conformance/traceability-manifest.json`) and enforced by `TraceabilityManifestTests`, which asserts (a) every id in each range appears exactly once, (b) every verification-path file exists on disk, and (c) every unresolved-governance id is flagged `blocker:true` and named in this report's blocker section. The consolidated AD-17 gate suite is `GovernanceConformanceTests` (selectable by `dotnet test --filter "Architecture=AD-17"`); cross-assembly SDK-purity and the single-durable-owner seam are `RuntimeOwnershipConformanceTests`; the UI floor is `UiFloorCoverageTests`.

> **Reading the "verification path" column.** A path is a repo-relative test file under the module root. Most gates are proven by the per-story behavioral suites that introduced each behavior; Story 4.5 adds the consolidation layer (`GovernanceConformanceTests`, `RuntimeOwnershipConformanceTests`, `UiFloorCoverageTests`) and this report. Blocker rows (⛔) carry an unresolved governance decision that fails closed in V1 and blocks production-like enablement — see §5.

---

## 1. Functional Requirements (FR-1..28)

FR→story backbone from the readiness-report coverage matrix (28/28, 100%); verification-path column added from the conformance suite.

| FR | Title | Owning story | Verification path |
| --- | --- | --- | --- |
| FR-1 | Configure `hexa` identity / instructions / lifecycle / tenant | 1.3 | `tests/Hexalith.Agents.Tests/AgentAggregateTests.cs` |
| FR-2 | Link Agent to Party identity | 1.4 | `tests/Hexalith.Agents.Tests/AgentPartyIdentityTests.cs` |
| FR-3 | Manage Agent lifecycle | 1.3 | `tests/Hexalith.Agents.Tests/AgentLifecycleE2ETests.cs` |
| FR-4 | Manage Global Providers Aggregate | 1.2 | `tests/Hexalith.Agents.Tests/ProviderCatalogAggregateTests.cs` |
| FR-5 | Select provider and model per Agent | 1.5 | `tests/Hexalith.Agents.Tests/AgentProviderSelectionTests.cs` |
| FR-6 | Configure response mode | 1.6 | `tests/Hexalith.Agents.Tests/AgentResponseModeTests.cs` |
| FR-7 | Configure Approver Policy | 1.6 | `tests/Hexalith.Agents.Tests/AgentApproverPolicyTests.cs` |
| FR-8 | Call Agent from a Conversation | 2.1 | `tests/Hexalith.Agents.Tests/AgentInteractionAggregateTests.cs` |
| **FR-9 ⛔** | Build V1 Conversation context (bounded-context **values deferred — OQ-10**) | 2.3 | `tests/Hexalith.Agents.Tests/AgentInteractionContextAggregateTests.cs` |
| FR-10 | Handle generation failure | 2.4 | `tests/Hexalith.Agents.Tests/AgentInteractionGenerationAggregateTests.cs` |
| FR-11 | Post automatic response | 2.5 | `tests/Hexalith.Agents.Tests/AgentInteractionPostingAggregateTests.cs` |
| FR-12 | Prevent automatic posting when policy fails | 2.5 | `tests/Hexalith.Agents.Server.Tests/AgentInteractionPostingOrchestratorTests.cs` |
| FR-13 | Create Proposed Agent Reply | 3.1 | `tests/Hexalith.Agents.Tests/AgentInteractionProposalAggregateTests.cs` |
| FR-14 | Preserve all proposal versions | 3.3 | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| FR-15 | Edit Proposed Reply | 3.3 | `tests/Hexalith.Agents.Tests/AgentInteractionProposalEditAggregateTests.cs` |
| FR-16 | Regenerate Proposed Reply | 3.4 | `tests/Hexalith.Agents.Tests/AgentInteractionProposalRegenerationAggregateTests.cs` |
| FR-17 | Approve Proposed Reply | 3.5 | `tests/Hexalith.Agents.Tests/AgentInteractionProposalApprovalLifecycleE2ETests.cs` |
| FR-18 | Reject, abandon, or expire Proposed Reply | 3.6 | `tests/Hexalith.Agents.Tests/AgentInteractionProposalTerminalAggregateTests.cs` |
| FR-19 | Enforce tenant isolation | 2.2 + 4.5 | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| FR-20 | Enforce role and policy authorization | 2.2 | `tests/Hexalith.Agents.Server.Tests/AgentInteractionGateOrchestratorTests.cs` |
| FR-21 | Fail closed on dependency uncertainty | 2.2 + 4.5 | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| FR-22 | Provide Admin UI | 4.3 (+1.8/2.6/3.7) | `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` |
| FR-23 | Provide API and client contracts | 4.1 | `tests/Hexalith.Agents.Server.Tests/AgentsOperationEndpointsTests.cs` |
| **FR-24 ⛔** | Capture Agent audit evidence (**content-bearing audit blocked — OQ-8**) | 4.2 | `tests/Hexalith.Agents.Tests/AgentInteractionAuditTraceabilityE2ETests.cs` |
| FR-25 | Expose operational status | 4.3 | `tests/Hexalith.Agents.Contracts.Tests/AgentOperationalStatusSummaryContractsTests.cs` |
| **FR-26 ⛔** | Configure Content Safety / prompt policy (**categories deferred — OQ-9**) | 1.7 | `tests/Hexalith.Agents.Tests/AgentContentSafetyPolicyTests.cs` |
| FR-27 | Enforce safety before Conversation side effects | 2.4 | `tests/Hexalith.Agents.Tests/AgentContentSafetyActivationTests.cs` |
| **FR-28 ⛔** | Define launch readiness controls (**SM-2/SM-3 thresholds deferred — OQ-11**) | 4.4 | `tests/Hexalith.Agents.Tests/AgentLaunchReadinessTests.cs` |

---

## 2. Non-Functional Requirements (NFR-1..10)

This NFR→gate mapping is newly authored here (it does not exist in any source).

| NFR | Name | Owning story / gate | Verification path |
| --- | --- | --- | --- |
| NFR-1 | Security | AD-12 fail-closed (2.2) | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| NFR-2 | Privacy | Tenant isolation (2.2) | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| NFR-3 | Reliability | AD-3 pure aggregates / deterministic replay (2.1) | `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` |
| NFR-4 | Observability | Operational status (4.3) | `tests/Hexalith.Agents.UI.Tests/OperationalStatusSurfaceTests.cs` |
| NFR-5 | Auditability | Audit traceability (4.2) | `tests/Hexalith.Agents.Tests/AgentInteractionAuditTraceabilityE2ETests.cs` |
| NFR-6 | Provider Safety | Secret non-disclosure + domain purity (4.5) | `tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs` |
| NFR-7 | Content Safety | Generation safety gate (2.4) | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| NFR-8 | Context Bounds | Context-too-large blocking (2.3) | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| **NFR-9 ⛔** | Performance (**latency SLOs deferred — OQ-5**) | 4.4 | `tests/Hexalith.Agents.Tests/AgentLaunchReadinessTests.cs` |
| **NFR-10 ⛔** | Cost Control (**thresholds deferred — OQ-6**) | 4.4 | `tests/Hexalith.Agents.Tests/AgentLaunchReadinessTests.cs` |

---

## 3. Architecture Decisions (AD-1..19)

AD-3/AD-5/AD-11/AD-12/AD-13/AD-17 map to this story's consolidated suite; AD-18/AD-19 to the runtime-ownership seam; the rest to per-story behavioral tests.

| AD | Summary | Verification path |
| --- | --- | --- |
| AD-1 | Agents is a full EventStore domain module | `tests/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs` |
| AD-2 | Aggregate boundaries (Agent, ProviderCatalog, AgentInteraction) | `tests/Hexalith.Agents.Tests/AgentInteractionAggregateTests.cs` |
| AD-3 | Pure aggregates, side effects outside | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| AD-4 | Interaction snapshot at request time | `tests/Hexalith.Agents.Tests/AgentInteractionAggregateTests.cs` |
| AD-5 | Proposal lifecycle append-only / immutable versions | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| **AD-6 ⛔** | Conversations boundary (**AddParticipant membership seam deferred**) | `tests/Hexalith.Agents.Server.Tests/ConversationClientResponsePosterTests.cs` |
| **AD-7 ⛔** | Agent Party identity and membership (**membership seam deferred**) | `tests/Hexalith.Agents.Tests/AgentPartyIdentityTests.cs` |
| AD-8 | Approver policy resolution | `tests/Hexalith.Agents.Server.Tests/AgentApproverPolicyOrchestratorTests.cs` |
| AD-9 | Provider adapter and catalog boundary (SDK/credentials hidden) | `tests/Hexalith.Agents.Contracts.Tests/ContractsSecretNonDisclosureTests.cs` |
| AD-10 | Provider capability floor / standard metadata | `tests/Hexalith.Agents.Tests/ProviderCatalogMetadataValidationTests.cs` |
| AD-11 | Conversation context bounds (no silent truncation) | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| AD-12 | Authorization and dependency uncertainty (fail closed) | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| AD-13 | Idempotent external effects / deterministic ids | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| AD-14 | Sensitive content and secret safety | `tests/Hexalith.Agents.Tests/AgentInteractionAuditTraceabilityE2ETests.cs` |
| AD-15 | Public surface and UI parity | `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` |
| AD-16 | Module-local operational topology | `tests/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs` |
| AD-17 | Contract and test gates | `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` |
| **AD-18 ⛔** | Hybrid runtime ownership (**live owner deferred; seam verified**) | `tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs` |
| **AD-19 ⛔** | Tool and protocol boundaries (**MCP/A2A/tool schemas deferred; seam verified**) | `tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs` |

---

## 4. UX Design Requirements (UX-DR1..41)

UI-story anchors newly authored here. The page-coverage meta-test (`UiFloorCoverageTests`) guarantees no Agents page silently skips the floor.

| UX-DR | Short | Owning UI story | Verification path |
| --- | --- | --- | --- |
| UX-DR1 | Register Agents domain + ordered nav | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentsNavigationTests.cs` |
| UX-DR2 | Agents overview readiness surface | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentsOverviewTests.cs` |
| UX-DR3 | `hexa` configuration form | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs` |
| UX-DR4 | Provider catalog full-width grid | 1.8 | `tests/Hexalith.Agents.UI.Tests/ProviderCatalogTests.cs` |
| UX-DR5 | Approver policy builder rows | 1.8 | `tests/Hexalith.Agents.UI.Tests/ApproverPolicyTests.cs` |
| UX-DR6 | Proposal queue full-width grid + filters | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalQueueTests.cs` |
| UX-DR7 | Proposal detail / editor workspace | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalDetailTests.cs` |
| UX-DR8 | Version history listing | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalVersionHistoryTests.cs` |
| UX-DR9 | Operational status panels by recovery action | 4.3 | `tests/Hexalith.Agents.UI.Tests/OperationalStatusSurfaceTests.cs` |
| UX-DR10 | Audit evidence panels (support-safe) | 4.3 | `tests/Hexalith.Agents.UI.Tests/AuditEvidenceSurfaceTests.cs` |
| UX-DR11 | Fluent semantic status roles | 1.8 | `tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs` |
| UX-DR12 | Status = color + icon + visible text | 1.8 | `tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs` |
| UX-DR13 | Inherit Fluent typography roles | 1.8 | `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` |
| UX-DR14 | Localizable whole strings (en/fr) | 1.8 | `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs` |
| UX-DR15 | Page measures (FullWidth vs Constrained) | 4.5 | `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` |
| UX-DR16 | 4px spacing rhythm | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs` |
| UX-DR17 | Reserved space for badges / action slots | 2.6 | `tests/Hexalith.Agents.UI.Tests/AgentCallStatusFeedbackTests.cs` |
| UX-DR18 | Elevation only for transient overlays | 3.7 | `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` |
| UX-DR19 | Inherit Fluent shapes (no custom radii) | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs` |
| UX-DR20 | `agent-readiness-badge` | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentReadinessMappingTests.cs` |
| UX-DR21 | `provider-status-badge` | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentReadinessMappingTests.cs` |
| UX-DR22 | `proposal-state-badge` | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs` |
| UX-DR23 | Response mode segmented control | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs` |
| UX-DR24 | Conversation Agent Call affordance | 2.6 | `tests/Hexalith.Agents.UI.Tests/ConversationCallTests.cs` |
| UX-DR25 | Canonical Agent readiness states | 4.5 | `tests/Hexalith.Agents.UI.Tests/AgentReadinessMappingTests.cs` |
| UX-DR26 | Canonical Provider/model states | 4.5 | `tests/Hexalith.Agents.UI.Tests/AgentReadinessMappingTests.cs` |
| UX-DR27 | Canonical Agent Call states | 4.5 | `tests/Hexalith.Agents.UI.Tests/AgentCallStatusPresentationTests.cs` |
| UX-DR28 | Canonical Proposal lifecycle states | 4.5 | `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs` |
| UX-DR29 | Canonical Audit availability states | 4.5 | `tests/Hexalith.Agents.UI.Tests/OperationalStatusPresentationTests.cs` |
| UX-DR30 | Grid/list surface states | 4.5 | `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` |
| UX-DR31 | Editing explicit, regeneration distinct, approval on selected | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalEditorTests.cs` |
| UX-DR32 | Keyboard / focus (Esc closes, focus return) | 3.7 | `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` |
| UX-DR33 | FC-A11Y primitives | 1.8 | `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` |
| UX-DR34 | Grid table semantics | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalQueueTests.cs` |
| UX-DR35 | Proposal editor fully keyboard operable | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalEditorTests.cs` |
| UX-DR36 | Live regions announce transitions | 3.7 | `tests/Hexalith.Agents.UI.Tests/ProposalTransitionAnnouncerTests.cs` |
| UX-DR37 | Focus-trapped dialogs with safe escape | 3.7 | `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` |
| UX-DR38 | Reduced-motion independence | 2.6 | `tests/Hexalith.Agents.UI.Tests/AgentCallStatusFeedbackTests.cs` |
| UX-DR39 | Responsive desktop-first behavior | 4.5 | `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` |
| UX-DR40 | Fail closed on constrained viewports | 4.5 | `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` |
| UX-DR41 | Use FrontComposer capabilities intentionally | 1.8 | `tests/Hexalith.Agents.UI.Tests/AgentsNavigationTests.cs` |

---

## 5. Explicit launch blockers / unresolved governance decisions

These are **explicit launch blockers**, not hidden assumptions. Each fails closed in V1 today; production-like enablement requires the named decision. Enumerated and enforced by `TraceabilityManifestTests` (every id below carries `blocker:true`).

| Id(s) | Unresolved decision | Owner | What it blocks | Current fail-closed posture |
| --- | --- | --- | --- | --- |
| **FR-24** (OQ-8; PRD §9 Data Governance) | Audit retention / legal hold / export / deletion | Product + Governance | Content-bearing audit (Story 4.2 ships metadata-only) | `AgentAuditGovernanceReadiness = MetadataOnlyBlocked`, surfaced as the `UnresolvedAuditGovernance` launch-readiness blocker (Story 4.4); production-like enablement fails closed by default. |
| **NFR-9** (OQ-5) | Latency SLOs | Architecture + Release PM | Production-like enablement | Story 4.4 records the structure (per-mode latency targets); concrete SLO values deferred. |
| **NFR-10** (OQ-6) | Cost-control thresholds | Architecture + Release PM | Production-like enablement | Per-call guardrails + usage capture in place; threshold values deferred. |
| **FR-26** (OQ-9) | Content Safety policy categories | Product + Security | Production generation | Generation fails closed at the safety gate; concrete category set deferred. |
| **FR-9** (OQ-10) | Bounded-context behavior values | Architecture | Bounded-context branch | Over-budget context fails closed (context-blocked); branch values deferred. |
| **FR-28** (OQ-11) | Launch metric thresholds for SM-2 / SM-3 | Product + Release PM | Launch metric evaluation | Story 4.4 records the metric definitions; threshold values deferred. |
| **AD-6 / AD-7** | Conversations `AddParticipant` membership seam | Conversations + Agents | Posting / membership | Posting fails closed without the membership seam (`MembershipUnavailable`). |
| **AD-18** | Live runtime owner (Agent Framework workflow / Dapr Workflow) | Architecture | Live durable orchestration | No live owner bound; `DeferredAgentGenerationProvider`/`DeferredAgentCommandDispatcher` fail closed; seam verified (§6). |
| **AD-19** | MCP / A2A / tool-schema contracts | Architecture | Live tool / remote-agent calls | No tool/protocol surface wired; seam verified (§6). |

---

## 6. Deferred-with-seam verifications

Runtime-orchestration items are **deferred per `ARCHITECTURE-SPINE.md#Deferred`** and verified at the **fail-closed seam** (no live owner, no SDK leak) rather than fabricated. Re-verify each when the live binding lands.

| Deferred item | Seam verification (today) | Re-verify when live binding lands |
| --- | --- | --- |
| Agent Framework workflow / session restore (AD-18) | No compiled durable owner in `Server/Application/Workflows`; domain references no workflow SDK (`RuntimeOwnershipConformanceTests`). | Session-restore replay/idempotency tests against the live workflow owner. |
| Dapr Workflow ownership (AD-18) | Exactly one declared owner = `AgentInteraction` commands; no second in-memory worker (no `IHostedService`/`BackgroundService`). | Durable-owner ownership + retry-idempotency tests. |
| MCP / A2A / tool-schema contracts (AD-19) | `Server/Application/Tools` empty; no `ModelContextProtocol`/provider SDK in the domain/contracts surface. | Tool-schema + A2A contract conformance tests. |
| Live generation / posting retries (AD-13) | Deterministic generation attempt-id / version-id and posting MessageId/idempotency-key contract pinned in `GovernanceConformanceTests` + orchestrator tests. | Live retry-loop idempotency tests (no duplicate version/message). |
| Live read-model / projection binding | `Server/Projections` empty (`.gitkeep` only); UI reads the substituted/deferred gateway seam (fail-closed). | Projection freshness + stale/degraded surface tests against the live read model. |

---

## 7. Conformance suite entry points

- **Consolidated AD-17 domain gates:** `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs` — `dotnet test --filter "Architecture=AD-17"`.
- **Cross-assembly SDK-purity + single-durable-owner seam (AD-18/AD-19):** `tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs`.
- **Provider-secret non-disclosure (NFR-6 / AD-9 / AD-14):** `tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs` — the secret-bearing member-name scan over the EventStore **domain** surface (events / projections / status / audit), in addition to the contracts-only `ContractsSecretNonDisclosureTests`. Selectable by `dotnet test --filter "Gate=SecretNonDisclosure"`.
- **FrontComposer UI floor + page-coverage meta-test (AC3):** `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs`.
- **Machine-checked traceability:** `tests/Hexalith.Agents.Server.Tests/TraceabilityManifestTests.cs` + `Conformance/traceability-manifest.json` (this report's backing).
- **Requirement-tagged selection:** every gate carries `[Trait("Requirement"|"Architecture"|"UxRequirement"|"Gate", id)]` so a failing gate names its FR/NFR/UX-DR/AD.
