# Architecture Spine Update Report - Hexalith Agents - 2026-09-09

- **Spine:** `ARCHITECTURE-SPINE.md` (status `final`, updated 2026-09-09, AD-1..AD-31)
- **Change signals:** validation report of 2026-09-08 (3 critical, 21 high, 33 medium, 25 low), PRD update of 2026-09-09, UX reconcile of 2026-09-09
- **Path:** autonomous application; product-level calls carry `[ASSUMPTION]` keys (PRD `A-n` or spine `ARCH-A-n`)

## What changed

Every AD-1..AD-26 rule was re-distilled in place with stable ids; AD-27..AD-31 were added. The launch-readiness register and the external dependency register were amended in the same run so the spine can point to them instead of restating them; the UX platform policy name was aligned to AD-30.

| New decision | Closes |
| --- | --- |
| AD-27 Execution-State Content Boundary | C-1 (content in Dapr Workflow history and Agent Framework session state) |
| AD-28 Time Authorities | H-3 (three clocks for one deadline), R-7 (NFR-9 clock) |
| AD-29 Deterministic Identity Derivation | H-5, S-12, A-14 (two derivations of one id; tenant-unbound idempotency) |
| AD-30 Principals And Trusted Envelope | H-2, S-3, B-13 (no principal model; forged `actor:*` extensions) |
| AD-31 Conversation Surface Contribution | H-12, B-11 (the only V1 entry point had no AD and no register record) |

Major amendments: platform-scoped `ProviderCatalog` under the reserved `system` tenant plus `TenantProviderEnablement` (C-2); a complete aggregate inventory of fourteen aggregates including `BudgetLedger`, `TenantGovernancePolicy`, `ContentSafetyPolicy`, `ConversationAgentState`, `AuditInspection`, `SecurityEventLog`, `LegalHold`, `AuditExport`, `ProtectedDeletion` (H-1); one discriminated `Freshness` value (H-4); readiness-registry key grammar, `ObservationId`, `AuthorizedProducer`, and operation-gate matrix version 2 (H-6); a safety-policy lineage test replacing a rank (H-7 and its own reviewer regression); bounded reservation settlement, rate limits enforced atomically by the ledger, and kill-switch and Disable semantics per state (H-8, H-11); field-level `ProtectedContent` envelope with per-interaction keys, two-phase hold pinning, sealed transport, custodian key operations, and a new `EXT-PROTECTION-1` record (H-9, security C-1, H-1, H-2); audit envelope, change evidence, and export manifest sealing (H-10); the altitude cut that moves enumerations to the register (H-13); ratified shipped projection ids (H-14); narrowed `EXT-PROVIDER-1` consumers (H-15); and every dated status note removed (H-16).

## Reviewer gate

| Lens | Verdict before fixes | Critical | High | Applied |
| --- | --- | --- | --- | --- |
| Reconcile PRD | reconciled with gaps | - | 4 gaps, 2 contradictions | all 5 contradictions and 22 gaps |
| Reconcile UX | mostly aligned | - | - | 16 spine-side gaps; 12 UX-side drifts returned to the UX memlog |
| Closure matrix (75 prior findings) | 52 closed, 14 partial, 7 open, 2 diverged | - | - | partials and opens applied except memlog supersession markers (an append-only event lists them) |
| Rubric | pass with findings | 0 | 2 | both highs and the mediums |
| Verified-current | pass with findings | 0 | 1 | all |
| Adversarial | fail | 1 | 8 | all critical and high plus 14 of 16 mediums |
| Brownfield drift | fails to ratify | 0 | 4 | gaps recorded with owners in the memlog; epics do not yet carry the obligations |
| Security / data integrity | fail | 2 | 6 | all critical and high plus the actionable mediums |

Deterministic checks after the final batch: `lint_spine.py` 0 findings; all four mermaid diagrams parse under mermaid 11.

## Implementation follow-ups this update creates

Owners are assumptions (`ARCH-A-5`) until sprint planning assigns them.

1. Migrate the tenant-scoped `ProviderCatalogAggregate` and its read model to the `system`-tenant platform catalog plus `TenantProviderEnablement` (Story 5.5).
2. Derive `AttemptId`, generated and edited version ids, and `ProposalId` under AD-29 (Stories 6.4, 7.1, 7.2, 7.3).
3. Version the public route prefix to `/api/v1/agents` (Story 5.5).
4. Replace JWT-only administrator context and the tenant stamping of `actor:agentsProviderAdmin` with the AD-30 ingress and HMAC-tagged extensions (Story 5.4, deferred-work DW-2).
5. Move safety configuration off `AgentAggregate` into `ContentSafetyPolicy` plus `TenantGovernancePolicy`, and retire the Epic 4 `RecordAgentLaunchReadiness` value in favour of `LaunchReadinessGate` (Stories 8.4, 5.5).
6. Bump `ConfigurationVersion` on lifecycle events (DW-4).
7. Align the test stack with the workspace catalog, including the Microsoft Testing Platform v2 `test.runner` entry, and create `Hexalith.Agents.IntegrationTests` (Story 5.6).
8. Drop `Server/Aggregates` and `Application/Tools` from the tree and from `StructuralSeedConformanceTests` (Story 5.6).
9. Remove the `/agents/conversation-call` harness before Story 6.7 closes.

## Upstream items

- **UX:** twelve drifts listed in the UX memlog (direction entry of 2026-09-09), notably caller exclusion from approval, the `Caller` source as a rejected legacy value, the platform/tenant split of the safety-policy surface, who may set caps, lazy removal detection, and missing surfaces for the kill switch, tenant enablement, administrative retry/abandon, and deletion requests.
- **Epics and sprint status:** none of the obligations above appear in `epics.md` or `sprint-status.yaml`; run `bmad-correct-course` or `bmad-create-epics-and-stories` before the next sprint planning.
- **PRD:** the spine's `ARCH-A-1..ARCH-A-7` assumptions should be registered in PRD section 8.1 so `RQ-1` names them from one index.
- **Registers:** `EXT-PROTECTION-1` is new and `Uncommitted`; `EXT-CONV-AI-1` seam 2 now requires verbatim persistence of the Agents-supplied `MessageId`.

## Next steps

1. `bmad-spec` to adopt or refresh the spine as a spec companion so stories cite AD-27..AD-31.
2. `bmad-correct-course` for the follow-ups and register changes above.
3. `bmad-architecture validate` once Stories 5.4 to 5.6 land, to re-run the gate against code.
