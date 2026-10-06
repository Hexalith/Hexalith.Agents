---
title: '5.4 Prove Trusted Principal Tenant Party And Approver Readiness'
type: 'feature'
created: '2026-09-27'
status: 'draft'
human_approval: 'accepted'
approved_on: '2026-10-04'
implementation_entry: 'blocked-external-commitments'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-recheck.md'
  - '_bmad-output/specs/spec-story-5-4-dependency-unblock/SPEC.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Setup trusts claim-derived roles and unsigned flags. Tenant, Party, and approver readers are deferred; replay admission and durable denial recording are absent.

**Approach:** Implement current-evidence authorization, Agent Party and approver resolution, AD-30 trusted ingress, replay admission, and durable security observations. Prove fail-closed behavior and safe API/UI basis against accepted live contracts.

## Boundaries & Constraints

**Always:** Select one operation-specific `User`, `Administrator`, `Platform`, or `Workflow` principal from fresh Tenants/Parties evidence; check Platform in `global-administrators`. Verify immutable Agent Party by tenant/id and AI type only under Branch A. Keep Caller deserializable but reject it before mutation. Strip client-reserved keys; sign/verify AD-29 canonical bytes using the committed secrets profile. Register (`Issuer`, `DeliveryNonce`) in reserved `system` before target-command construction; exact replay preserves stored first-seen times. Durably spool each denial before reporting it processed, then append one acknowledged daily `SecurityEventLog` event. Use EventStore for domain state and private exact-target credentials for both pre-command exceptions.

**Never:** Trust JWT roles, old projections, Party type alone, or client extensions. Never persist Party PII, secrets, or claimed target identity. Do not replace the Party, create another Agent, invent an Agents host/spool, execute a seam before `Available`, or claim live readiness from mocks.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current authority | Contiguous tenant events; active Party and approver basis | Same safe API/UI basis | Revoked, gapped, stale, ambiguous, or unavailable blocks |
| Trusted delivery | Valid principal, scope, tag, nonce | One command; exact replay reaches AD-29 idempotency | Forged/changed/cross-tenant nonce rejects and audits |
| Denial recovery | EventStore or worker outage | Durable spool item drains once | Spool outage blocks before target command |

</frozen-after-approval>

## Open Questions

- **Owner packets (delivery blocker; rechecked 2026-10-06):** Complete owner-accepted commitments for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1 remain pending. Each needs the full contract, immutable target, integration date and executable verification command. Parties and Platform sibling implementation has advanced, but installed references and acceptance gates have not. Supply packet links to reconcile the register; until delivery, retain draft. The user approved this spec on 2026-10-04; full scope and Branch B are settled and need no further spec approval.

## Code Map

- `src/Hexalith.Agents.Server/Ports/HttpAgentAdministrationContextProvider.cs` — replace JWT role authority with fresh operation-specific evidence; Tenants `TenantProjectionEventHandler` still accepts sequence gaps/equal conflicts and has no global-administrator consumer.
- `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs` — verify explicit tenant/ID/freshness and preserve Organization ID. The installed Parties reference lacks the identity/history contract; sibling `IPartiesIdentityClient` and `PartyIdentityQueryService` are now committed, but production custody/retained-history and the Live verifier remain incomplete. Map provisioned Organization type and safe Agent classification only through the final accepted contract.
- `src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs` and `src/Hexalith.Agents.Server/Application/Agents/ApproverPolicyVerdict.cs` — add actor/binding/source correspondence; reject unknown outcomes and Caller. Preserve historical deserialization/folds.
- `src/Hexalith.Agents.Server/Application/Queries/{AgentSetupQueryHandlerBase,AgentInteractionAuditQueryHandlerBase}.cs` — replace global-admin flag bypasses before protected reads; `AgentInteractionGateOrchestrator` must stop downstream reads on authority denial.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` — replace unsigned flags; existing digest/identity primitives do not supply AD-30 custody/replay. Preserve Story 5.3 outcomes.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Agents.Server/Ports/ProjectedTenantAccessReader.cs`, `PartiesAgentPartyDirectory.cs`, `ProjectedApproverPolicyResolver.cs`, and `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs` — bind contiguous tenant events and current Party, actor, and approver sources.
- [ ] `src/Hexalith.Agents/Agent/AgentAggregate.cs` and `src/Hexalith.Agents.UI/Components/Pages/ApproverPolicy.razor` — reject Caller and render exact safe basis.
- [ ] `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`, `src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs`, and `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` — authorize before protected reads or effects.
- [ ] `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` and `src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs` — verify signed operation/principal/scope and separate logical/delivery ids.
- [ ] `src/Hexalith.Agents/TrustedEnvelopeReplay/TrustedEnvelopeReplayAggregate.cs` and `src/Hexalith.Agents/SecurityEventLog/SecurityEventLogAggregate.cs` — implement pure folds; bind private exact-target replay/spool/worker ports in `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs`.
- [ ] `test/Hexalith.Agents.Tests/AgentApproverPolicyTests.cs`, `test/Hexalith.Agents.UI.Tests/ApproverPolicyTests.cs`, and `eng/verify-story-5.4.ps1` — cover matrix edge cases, persisted replay, ACL, isolation, and UI; execute external seams only at `Available`.

**Acceptance Criteria:**
- Given active tenant/Party/approver evidence, when setup is queried, then API/UI expose the same safe basis; revocation or uncertainty blocks before downstream effects.
- Given a signed scoped envelope, when replay admission runs, then only its permitted principal reaches dispatch; exact replay preserves stored first-seen facts and changed fields reject/audit.
- Given a denial during EventStore or worker outage, when recovery runs, then one spool item reaches one acknowledged daily security event; spool outage constructs no target command.
- Given tenant B targets tenant A, when any public or application path denies, then no lookup, effect, count, message, log, or accessible output discloses A.

## Implementation Notes

- 2026-10-04: Recorded the user's "I accept" as specification approval. No owner target, integration date or executable command was supplied by that response, and no dependency gate was waived. Preserve the approved frozen intent and acceptance criteria; retain draft/backlog pending complete owner commitments. Resume from this approval when the register's entry gate is satisfied.
- 2026-10-06: Rechecked owner issues, installed references and sibling source. Parties identity/history and Platform identity hosting are now committed in sibling repositories; full live contracts and owner packets remain missing. Preserve prior approval, frozen intent, acceptance criteria, full scope and Branch B; retain draft/backlog. No implementation or consumer seam execution occurred.

## Spec Change Log

- 2026-09-27: User directed preservation of full scope. Added the Parties-first implementation proposal and complete dependency-ordered owner work. No frozen intent, acceptance criterion, dependency status or readiness gate changed.
- 2026-09-27: User selected Branch B as the implementation path. Product and Parties Maintainer acceptance and every complete dependency gate remain pending.
- 2026-10-03: Updated source/owner recheck and Code Map after reference updates. All four records remain Uncommitted; no complete new contract or owner acceptance was found. KEEP full scope, Branch B direction, frozen intent, original acceptance and draft/backlog gates.
- 2026-10-03: Rechecked latest references and sibling identity progress; history custody/live verification remain incomplete. KEEP all prior scope, intent, acceptance and dependency gates.
- 2026-10-04: Rechecked four owner requests and reference/sibling contracts. New Tenants/Conversations owner revisions add no required authority/six-seam source contract; Parties Live verification and complete custody/host commitments remain missing. Expanded the Code Map; KEEP frozen intent, original acceptance, full scope, Branch B and draft/backlog.
- 2026-10-06: Corrected the Code Map's stale description of sibling Parties work as uncommitted; its identity implementation and Platform identity hosting have since been committed. No complete acceptance packet or live verifier was delivered. KEEP approved frozen intent, original acceptance criteria, full scope, Branch B and all entry/execution gates.

## Review Triage Log

## Design Notes

[Recheck](story-5-4-dependency-recheck.md#owner-and-contract-gate-recheck--2026-10-06). No new intent decision or irreversible planning action. Footprint: domain, Server, EventStore, UI, tests. Contract entry remains blocked; source inspection cannot supply owner acceptance.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.4.ps1` — planned verifier; absent and unexecuted. Live lanes require Available targets.
