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

- **Complete owner targets and commands:** The [owner packet](story-5-4-owner-commitments-2026-10-07.md) records project ownership and the accepted 2026-10-08 integration date for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1. Each register record still has `TBD` for its immutable complete target and accepted executable full compatibility command, and remains `Uncommitted`. Is a complete target and command now accepted for each record? Options: provide all four accepted target/command pairs with their reproducible setup and evidence contract, permitting an exact-target entry check; or confirm they are still in delivery, keeping this story draft/backlog while the already authorized owner-prerequisite work continues. Partial checkout revisions and local/synthetic passes cannot serve as complete targets. Full scope, Branch B, specification approval, named decision-role approval, numeric envelope bounds and the accepted 365-day policy need no repeat decision.

## Code Map

- `src/Hexalith.Agents.Server/Ports/HttpAgentAdministrationContextProvider.cs` — replace JWT-derived administration roles with current operation-specific authority. Installed Tenants `Authorization/TenantsGlobalAdministratorVerifier.cs` already checks the global-administrators read model; it does not establish source-head freshness. `Tenants.Client/Handlers/TenantProjectionEventHandler.cs` accepts gaps and equal-sequence conflicts; a global-administrator client consumer and contiguous authority proof remain missing.
- `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs` — replace the ambient-tenant `PartyDetail`/legacy provisioning path with accepted explicit tenant/immutable-ID evidence. Installed Parties has `IPartiesIdentityClient` in `src/Hexalith.Parties.Client/Abstractions/IPartiesIdentityClient.cs`, Branch-B Organization classification, SDK sidecar security and bounded current/history queries in `Queries/{PartyIdentityQueryService,PartyIdentityQueryDeadline}.cs`. Reuse completion-time source/custody/authority checks, `PartyIdentitySourceFold.cs` observation validation and `Domain/PartyDomainProcessor.cs` admission/policy revalidation after unprotection and replay/custody. Qualified production custody/current actor authority, actor-free expired-predecessor continuation, irreversible all-copy receipts, nonrollback restore and complete P-01–P-10 Live qualification remain missing; local HTTP/deadline/admission evidence does not fill the entry fields.
- `src/Hexalith.Agents.Server/Ports/{DeferredTenantAccessReader,DeferredApproverPolicyResolver}.cs` and `Composition/AgentDomainHostComposition.cs` — current fail-closed placeholders; `ProjectedTenantAccessReader.cs` and `ProjectedApproverPolicyResolver.cs` in the tasks are planned new files. Add current actor/binding/source correspondence through `IApproverPolicyResolver` and `Application/Agents/ApproverPolicyVerdict.cs`; reject unknown outcomes and Caller without changing historical folds.
- `src/Hexalith.Agents.Server/Application/Queries/{AgentSetupQueryHandlerBase,AgentInteractionAuditQueryHandlerBase}.cs` — replace global-admin flag bypasses before protected reads; `AgentInteractionGateOrchestrator` must stop downstream reads after tenant denial.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` — existing Dapr caller checks and canonical enum/flag validation do not deliver signed AD-30 envelopes or private replay admission. Preserve Story 5.3 outcomes.
- Installed Conversations `src/Hexalith.Conversations.Server/Agents`, `src/Hexalith.Conversations.Contracts/Agents`, `src/Hexalith.Conversations.Client/IConversationClient.cs` and pure `src/Hexalith.Conversations/Aggregates/ConversationAggregate.Agents.cs` — reuse restricted local seams, the Facilitator roster, contiguous source reads, event-atomic approved-deletion publication and cancellation/replay checks. Installed acknowledgement quarantine in the aggregate and receipt/retained-transition guards in `State/ConversationState.Agents.cs` preserve accepted receipts. The owner accepted the creation-window count rule and dedicated service Party; production authority/source compare-append/catalogue/approval bindings and automatic C4 discovery/backfill/pump/authenticated receiver/independent acknowledgement lookup remain missing. Historical Local evidence does not establish complete current-target qualification.
- Installed Platform `src/Hexalith.Platform.Custody`, `apphost.cs` and `eng/{verify-ext-secrets-1.ps1,verify-agents-host.sh}` — reuse S1/S2 primitives and preserve closed production defaults. The owner accepted the numeric envelope timing bounds and named decision-role approval; exact issuer/audience/profile validity, actor/role identities, independent signer enrollment, public trust/revocation and worker enrollment remain unbound. Sibling Platform additionally has committed bounded `IdentityHistoryCleanup.cs` and scoped optional-custody registration, absent from the installed reference; provider confirmation supplies no production custody, all-copy destruction or restore qualification. Full S3/S4/v23, replicated spool/worker/private credentials and H1–H4 remain undelivered. Current Live/Full branches refuse before calls.

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
- 2026-10-07: Recorded the user's project-owner declaration and instruction to make the four complete commitments. Updated owner accountability and selected Hexalith.Platform for secret custody. [Commitment packet](story-5-4-owner-commitments-2026-10-07.md) retains missing target/date/complete-command fields explicitly; no current partial commit, delivery date or live success was invented. Direct owner instruction supersedes the need for another owner to respond on GitHub, while complete record and execution gates remain unchanged.

- 2026-10-07 entry recheck: Three independent read-only investigations found no complete accepted target/date/command packet. Refreshed the Code Map against the installed clean owner references, distinguishing Tenants' existing projection verifier from missing authoritative freshness and client consumption. [Current evidence](story-5-4-entry-recheck-2026-10-07.md) records exact source observations and unchanged gate hashes. Full scope, prior approval, Branch B, accepted 365-day policy, original acceptance and draft/backlog remain unchanged; no source implementation or live seam ran.

- 2026-10-07 build resume: Three fresh read-only investigations confirmed all four delivery records remain incomplete and Uncommitted. Installed Parties now includes the reviewed SDK security and query-deadline prerequisites; corrected the exact Conversations aggregate path. Sibling owner changes and later conformance/cleanup work remain source observations. [Resume evidence](story-5-4-build-entry-recheck-2026-10-07.md) preserves canonical revisions, selected source hashes and unchanged frozen intent/tasks/register/sprint/owner-prerequisite gates. Existing full-story approval and scope acceptance carry forward; implementation entry still requires accepted complete target/date/command packets.

- 2026-10-08 build resume: Three independent read-only investigations found new installed Parties admission/source-observation validation and Conversations acknowledgement/replay guards, plus sibling-only Platform cleanup. Updated the Code Map without treating historical Local receipts or checkout revisions as complete owner deliveries. [Resume evidence](story-5-4-build-entry-recheck-2026-10-08.md) records canonical revisions, selected source hashes and preserved approval/intent/tasks/register/sprint/owner gates. All four records still lack complete accepted target/date/command packets; retain draft/backlog. No source implementation, fresh tests or live seam execution occurred.

- 2026-10-09 build resume: Rechecked the authoritative register and installed Parties, Tenants, Conversations and Platform sources. The four integration dates are accepted as 2026-10-08; immutable complete targets and accepted full executable compatibility commands remain `TBD`, and all four records remain `Uncommitted`. Corrected the planning question and Code Map for accepted count, service-Party, numeric envelope and decision-role inputs. The approved frozen intent, tasks and acceptance criteria remain unchanged. No live seam was executed.

## Spec Change Log

- 2026-09-27: User directed preservation of full scope. Added the Parties-first implementation proposal and complete dependency-ordered owner work. No frozen intent, acceptance criterion, dependency status or readiness gate changed.
- 2026-09-27: User selected Branch B as the implementation path. Product and Parties Maintainer acceptance and every complete dependency gate remain pending.
- 2026-10-03: Updated source/owner recheck and Code Map after reference updates. All four records remain Uncommitted; no complete new contract or owner acceptance was found. KEEP full scope, Branch B direction, frozen intent, original acceptance and draft/backlog gates.
- 2026-10-03: Rechecked latest references and sibling identity progress; history custody/live verification remain incomplete. KEEP all prior scope, intent, acceptance and dependency gates.
- 2026-10-04: Rechecked four owner requests and reference/sibling contracts. New Tenants/Conversations owner revisions add no required authority/six-seam source contract; Parties Live verification and complete custody/host commitments remain missing. Expanded the Code Map; KEEP frozen intent, original acceptance, full scope, Branch B and draft/backlog.
- 2026-10-06: Corrected the Code Map's stale description of sibling Parties work as uncommitted; its identity implementation and Platform identity hosting have since been committed. No complete acceptance packet or live verifier was delivered. KEEP approved frozen intent, original acceptance criteria, full scope, Branch B and all entry/execution gates.
- 2026-10-07: Reconciled direct project-owner accountability and the custody repository into the register, and corrected the historical installed-Parties API description. KEEP frozen intent, original acceptance criteria, full scope, Branch B and draft/backlog until immutable targets, dates and complete verification commands are accepted.

## Review Triage Log

## Design Notes

Full-story intent and scope are settled. Unresolved delivery fields are owner commitments the code cannot supply. No irreversible operation is planned before those inputs and the applicable execution gates are satisfied. Implementation footprint remains domain, Server, EventStore, UI and tests; shared infrastructure belongs to its technical owner. [Current recheck](story-5-4-entry-recheck-2026-10-07.md) distinguishes implementation gaps from historical local evidence. No repeat scope, Branch-B or retention approval is required.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.4.ps1` — planned verifier; absent and unexecuted. Live lanes require Available targets.
