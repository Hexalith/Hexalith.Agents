# Story 5.4 dependency recheck

Checked 2026-09-27 during `bmad-build 5.4`. Full Story 5.4 remains in planning: no accepted external target or executable compatibility command was found. This report records observations, not owner acceptance or live readiness evidence.

## Inspected revisions

| Repository | Full HEAD |
| --- | --- |
| Agents | `ce129c60cc0b5d02764506c8d168cfa2f261015a` |
| Tenants reference | `49ca5b46c6cab066b5a939a5551a27aa8db1e541` |
| Parties reference | `c069af07af5f1e42bdfa1287705d943b8b4cadf7` |
| Conversations reference | `30fffc2cd427e837c719b87cb33475e34a6fc8de` |
| EventStore reference | `68492519b868899e6ab6bf64931f19f0cb1ca6a7` |
| Builds reference | `0610f7837221c9859e280f06f333d7452a40f5b1` |
| Sibling Platform | `98db8cf9fd6058c17e36d45b1935ec5301a68e00` |

Agents started clean on `main`. The sibling Platform worktree contains pre-existing edits and is not an immutable acceptance target; it was inspected read-only.

## Owner requests

Read through `gh issue view --json title,state,url,body,comments`; each issue is open with an empty comments collection:

| Dependency | Request | Missing acceptance |
| --- | --- | --- |
| `EXT-CONV-AI-1` | [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4) | Complete six-seam target, date, final typed public contract and live verification command |
| `EXT-PARTIES-1` | [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54) | Product/maintainer Branch A or B decision plus complete identity and current/historical human-binding target, date and live command |
| `EXT-SECRETS-1` | [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) | Owning repository, full custody target/date/command and numeric signing/replay profile |
| `EXT-HOST-1` | [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) | Complete host target/date/command, replicated security spool and restricted replay/recorder capabilities |

The authoritative [dependency register](../planning-artifacts/external-dependency-register.md) still marks all four `Uncommitted`, with target, date and command `TBD`. Local implementation, a partial test, or a newer submodule revision cannot establish acceptance. No messages were sent to owners.

## Code evidence

- **Tenants:** `references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs` and `TenantProjectionEventMetadata.cs` supply status, membership and sequence metadata. `Handlers/TenantProjectionEventHandler.cs` does not prove contiguous delivery: it accepts sequence jumps and equal-sequence events. Agents still binds `DeferredTenantAccessReader` in `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs`.
- **Parties:** `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs` supplies type/liveness and nullable freshness; no stable authenticated human actor ID, binding version/interval or historical binding query was found. `ValueObjects/PartyType.cs` has only Unknown, Person and Organization. Existing-state `CreateParty` handling in `src/Hexalith.Parties/Domain/PartyAggregate.cs` returns no-op before divergent-request validation. Agents' `PartiesAgentPartyDirectory` relies on ambient tenant scope, does not verify the returned ID, and accepts absent freshness.
- **Conversations:** `IConversationClient.GetConversationAsync`, `ConversationDetailsV1.Participants` and `ParticipantRole.Facilitator` are reusable public primitives. `ConversationDetailResult.Hidden` does not distinguish `ConversationDeleted` from `PrincipalRemovedFromConversation`; the complete six-seam contract is absent.
- **Approvers:** Agents binds `DeferredApproverPolicyResolver`; its result lacks an exact current human binding/evidence basis. `AgentAggregate` still accepts Caller, `ApproverPolicy.razor` still offers it, and current domain/UI tests assert that behavior. Local retirement can preserve the wire enum while rejecting new writes and removing the choice, without calling an external seam.
- **Ingress:** `HttpAgentAdministrationContextProvider` trusts JWT roles; `AgentSetupQueryHandlerBase` accepts `query.IsGlobalAdmin`. `AgentsTrustedCommandExtensionPolicy` checks authenticated Dapr identity and allowlisted literal flags, not AD-30 signatures. `EventStoreAgentCommandDispatcher` forwards the supplied envelope with `MessageId` as idempotency key and no separate nonce admission.
- **EventStore:** `ITrustedCommandExtensionPolicy`, `IIdempotencyIntentAdapter`, `IIdempotencyAdmissionCoordinator` and `CanonicalIdempotencyIntentEncoder` are existing integration seams. The business-idempotency encoder has its own format; it is not the AD-29/30 signing format. No replay ledger or security observation/spool implementation was found in Agents, EventStore or Builds source.
- **Platform:** `../platform/docs/ext-host-1-agents-composition.md` describes the historical scaffold. `../platform/eng/verify-agents-host.sh` builds that scaffold and explicitly defers live Agents composition. It cannot establish the expanded host/secrets contracts.

## Planning disposition and verification

The full draft retains its frozen intent and remains `draft`; sprint status remains `backlog`. The unresolved choice is to retain this complete scope until owner acceptance, or explicitly narrow to local Caller-policy retirement and its regression tests. Narrowing would not select Branch B, close the four dependencies, or complete live readiness.

`eng/verify-story-5.4.ps1` does not exist yet. No source code changed, builds/tests were not run, and no external seam was executed. Implementation still needs persisted-state replay/denial recovery, restricted-capability ACL, cross-tenant non-disclosure and live dependency evidence after the applicable gates are satisfied. Prior Story 5.3 test results are continuity context only.

## Full-scope owner implementation follow-through — 2026-09-27

The user has resolved the scope choice above: preserve **all** of Story 5.4. The [owner implementation proposal](../specs/spec-story-5-4-dependency-unblock/SPEC.md) replaces the proposed narrowing option with concrete work: Parties identity-by-id, tenant isolation, divergent-retry conflicts and durable current/historical human binding first; then the complete six Conversations seams, full custody profile and complete Platform composition including v21/v23.

Branch B was recommended from the current Organization provisioning code and selected by the user as the implementation path on 2026-09-27; formal Product/Parties acceptance remains pending. The proposal's [evidence and acceptance handoff](../specs/spec-story-5-4-dependency-unblock/evidence-and-acceptance.md) records the newer inspected reference/owner revisions, existing in-memory identity-binding work and EventStore crypto core without treating them as delivered contracts or live evidence. Read-only owner issue checks still found four open issues with no comments.

The original inspection above remains historical evidence. The story is still draft/backlog; all four complete records remain Uncommitted with no accepted target/date/command. The register now records the Branch B implementation direction; its commitment fields and all original entry/execution gates are unchanged. No owner messages were posted and no external seam was executed.

## Current reference and owner recheck — 2026-10-03

Rechecked during `bmad-build 5.4` after the root reference update. Agents started clean on `main`. The full-scope and Branch B choices remain settled; this recheck does not request narrowing or another identity-branch decision.

### Inspected immutable source positions

| Checkout | Full HEAD |
| --- | --- |
| Agents | `88105d4178d3827c7a5488e9008f358cb7b8db15` |
| Tenants reference | `bfc10cb62dff6784c4b4bcabadfd537ef74fd77a` |
| Parties reference | `937cb2a343aaa74963db9bb867a2c3a01ff48677` |
| Conversations reference | `35121cab6bfc963fb2a8d411a51800f6ef5a2a70` |
| EventStore reference | `2c58ffda41759e895ace4b9625c9bd931a217672` |
| Builds reference | `c16249a6a0c88b903e14c7f6612632e2c70d4e94` |
| Sibling Platform | `c3c473a4aa8da421896e4795fcdb0f4d2771a0be` |

Each identifier was obtained directly with `git rev-parse HEAD`. They identify inspected source, not accepted dependency targets. Sibling Platform and references were read-only; no fetch, submodule update or nested initialization occurred.

### Owner acceptance and entry gates

Fresh `gh issue view --json number,title,state,url,body,comments` reads found [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54), [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4), [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) and [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) all OPEN with empty comments. None supplies owner acceptance, an immutable complete target, integration date or executable command.

| Record | Current disposition | Missing input beyond target/date/command |
| --- | --- | --- |
| EXT-PARTIES-1 | Uncommitted | Product/Parties acceptance of Branch B, final tenant-scoped provisioning and authoritative current/historical human-binding contract |
| EXT-CONV-AI-1 | Uncommitted | Final complete six-seam contract and live component verification |
| EXT-SECRETS-1 | Uncommitted | Custody repository, independent trust authorities, numeric signing/recovery/retention profile and full custody contract |
| EXT-HOST-1 | Uncommitted | Complete host/protection/migration composition, replicated spool durability, restricted replay/recorder credentials and full v21/v23 contract |

The [register](../planning-artifacts/external-dependency-register.md#authority-and-entry-gate) requires complete Committed/Available records before ready-for-dev. Available and passing evidence for the exact installed target are required before consumer seam execution; only an accepted owner's exact compatibility command can qualify a Committed seam. New source primitives and local tests cannot establish either gate.

### Updated code findings

- **Tenants:** `TenantProjectionEventHandler.ApplyAsync` still ignores only lower sequences and accepts jumps/equal conflicts. `TenantLocalState.LastEvent` is not a contiguous checkpoint/high-water proof. `AddHexalithTenants` defaults to an in-memory consumer store and does not register a global-administrator consumer. Public global-administrator set/remove events and the `system/global-administrators` identity are reusable; do not depend on Tenants Server's internal singleton projection as authority.
- **Parties:** Identity/provisioning/binding domain and public contracts remain unchanged from the prior inspected reference. `PartyDetail` still mixes PII with nullable freshness and lacks stable actor/version/interval/history. Create on any existing state returns no-op before divergent intent validation. UI identity binding still defaults to in-memory storage. Branch B preserves the existing Organization identity, but this is not the complete authoritative contract.
- **Conversations:** Domain, Server, Client and Contracts have no six-seam completion changes from the prior inspected reference. `IConversationClient` supplies general get/append and `ParticipantRole.Facilitator` remains reusable. Public restricted membership/removal, accepted trace/provenance, typed MessageId/existence/deletion/removal results, authoritative denominator and atomic durable approved-deletion delivery remain absent. Hidden detail still collapses access outcomes.
- **Agents authority:** `HttpAgentAdministrationContextProvider` trusts JWT roles; setup and audit query bases trust global-admin flags. Composition still binds deferred authority/Party/approver readers. `AgentInteractionGateOrchestrator` performs downstream reads after tenant denial. `IApproverPolicyResolver` has no acting actor/binding/Conversation/exclusion context; `ApproverPolicyVerdict` does not prove returned source correspondence and unknown outcomes can pass. These require fresh authority admission before protected reads/effects and exact stable-human approver evidence.
- **Caller:** Aggregate configuration and UI still accept/offer Caller, with existing tests affirming it. Retirement remains a prerequisite inside full 5.4: preserve enum deserialization and historical event folds, reject new writes and legacy runtime eligibility, remove configuration choices, update shared fixtures and focused domain/UI tests. This alone cannot complete the story.
- **EventStore:** `ITrustedCommandExtensionPolicy`, `IIdempotencyIntentAdapter`, `IIdempotencyAdmissionCoordinator` and `IAggregateActor` provide extension/admission/fenced-read primitives. New `DaprSecretIdempotencyDigestKeyProvider`, `IIdempotencyDigestKeyProvider` and `IdempotencyDigestKeyRing` support active/retained business-idempotency keys and production secret-source validation. `CanonicalIdempotencyIntentEncoder` has its own framing; these do not supply AD-29/30 trusted-envelope custody, issuer-wide nonce replay or security-denial durability. Payload-protection core/interface remain reusable, while default DI still registers `NoOpEventPayloadProtectionService`.
- **Platform/Builds:** No trusted-envelope profile, replay registrar, replicated security spool/worker or exact-stream/event capability implementation was found. `../platform/apphost.cs` still defers Agents DomainService/UI to 5.6, and its host verifier only builds the historical scaffold. New Platform planning assigns Agents enrollment to post-MVP Epic 12; that is not a delivery commitment. Builds' per-run development signing helpers do not establish production custody.

### Planning result and validation limits

Refreshed the Story 5.4 draft Code Map and epic context. Preserve the frozen intent, original acceptance criteria, Branch B implementation direction, Story 5.3 behavior, dependency commitment fields and sprint backlog status. No source implementation, owner message, migration, deployment, build/test, compatibility command or consumer seam execution occurred. `eng/verify-story-5.4.ps1` remains unimplemented.

Required next evidence is the complete owner acceptance packet, not another approval of full scope or Branch B. The existing [owner implementation plan](../specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md) remains the handoff for delivering missing contracts. Full 5.4 cannot become ready-for-dev from these static findings.

## Post-reference-update gate recheck — 2026-10-03

Rechecked after the two latest Agents reference-update commits during `bmad-build 5.4`. Agents began clean on `main`. Independent read-only investigations covered authority/identity, all six Conversations seams, and custody/host composition. Full scope and Branch B remain settled. No owner messages, Git mutations, builds/tests, compatibility commands or consumer seams were executed.

### Inspected source positions

| Checkout | Full HEAD |
| --- | --- |
| Agents | `1b101eb0adca6387b66ec4b456e204f26e38a9c8` |
| Parties reference and sibling owner | `5388884eec84b16545fdc008b2fc04547b0ed5b6` |
| Tenants reference and sibling owner | `a46c127c8be1d8d6ecccdf0d710e25befa644cc6` |
| Conversations reference | `f415298801097caa5f745e6976d67a79e4e2aab4` |
| Sibling Conversations owner | `932548e2f222dce546f8f777b8827af5b43be417` |
| EventStore reference | `b51978dd1d2a3721ad239db2623e1560377c7583` |
| Builds reference | `688eec9a4333245cc0ff7772115c769094471863` |
| Platform reference | `c3c473a4aa8da421896e4795fcdb0f4d2771a0be` |
| Sibling Platform owner | `909321c40e36074d345b707b942c74a7c8f5c075` |

These identifiers came directly from Git and locate inspected source only. Sibling Parties and Platform contain existing uncommitted implementation, preserved read-only; their working-tree contents are not covered by their HEAD identifiers or installed in Agents' clean references.

### New implementation evidence and remaining gaps

- **Parties:** The reference adds a [draft Branch B identity specification](../../references/Hexalith.Parties/_bmad-output/implementation-artifacts/spec-ext-parties-1-branch-b-authoritative-identity.md), without committed source contracts. The [sibling owner spec](../../../parties/_bmad-output/implementation-artifacts/spec-ext-parties-1-branch-b-authoritative-identity.md) is in-progress and records design approval for Platform actor authority, trusted identity-service binding writes and finite Parties attribution history. Its uncommitted `IPartiesIdentityClient`, `PartyIdentityEvidence`, `HumanActorBindingEvidence`, `PartyIdentityAuthority` and authoritative source/query folds address explicit tenant scope, immutable provisioning, source correspondence and historical intervals. This is owner implementation progress, not formal acceptance of the complete dependency target. The [owner implementation notes](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-implementation-notes.md) still identify pending production retention settings, no production `IIdentityHistoryCustody`, missing irreversible cleanup/restore guarantees and no purpose-scoped retained-history source/checkpoint after profile erasure. The [proposed verifier](../../../parties/eng/verify-ext-parties-1.ps1) explicitly exits 1 in Live mode even with every input supplied because production custody/history and authenticated persisted P-01–P-10/restart/restore/failure-injection lanes are missing. Final typed outcomes also need reconciliation with the register. No verifier was run here.
- **Tenants:** New correction UI/state/tests do not change the authority seam. `TenantProjectionEventHandler` still accepts equal and jumping sequences, has no global-administrator event consumer and retains nullable last-event metadata rather than contiguous/high-water proof. Reuse exact `TenantIdentity` source addresses; current membership and global-administrator revocation still need authoritative admission.
- **Conversations:** Neither reference nor newer sibling has any `src/` change since the previous recheck. Existing `MessageId`, idempotency and Facilitator primitives remain reusable. Restricted membership/read/removal, accepted trace/provenance, typed message/existence/deletion/removal results, authoritative denominator and durable approved-deletion delivery remain incomplete. A generic `Hidden` detail result still collapses access outcomes.
- **EventStore/Platform:** New operation-bound RSA-PSS identity admission and stable actor registry are reusable through `IdentityAdmissionProof`, `IdentityAdmissionSigner` and `IdentityActorRegistryActor`; sibling Platform adds purpose-separated identity configuration. These do not implement AD-29/30 envelope HMAC custody, the numeric signing/recovery/retention profile, issuer-wide nonce replay or independent decision-approval authority. No replicated denial spool, restricted exact-stream replay/recorder credentials or complete v21/v23 host/protection protocol was found. Both Platform host verifiers still defer live Agents composition to Story 5.6. Builds changes add no relevant custody/host contract.
- **Agents:** Source/test/verifier files are unchanged from the previous inspected root revision. JWT/global-admin flags, deferred authority/approver bindings, unchecked Party scope/freshness, Caller acceptance, post-denial downstream reads and unsigned trusted extensions remain implementation work. Preserve Story 5.3 catalog/enablement and exact command outcomes.

### Owner evidence and disposition

Fresh authenticated `gh issue view --json number,title,state,url,body,comments` reads found [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54), [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4), [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) and [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) OPEN with empty comments. No complete accepted target/date/command packet was found in those requests or inspected owner artifacts. Parties design approval is recorded as progress without filling the register's commitment fields.

All four complete records remain `Uncommitted`. The register requires accepted complete commitments before `ready-for-dev`; Available and passing exact-target compatibility evidence remain prerequisites to consumer execution. Preserve frozen intent, original acceptance criteria, full scope, Branch B, draft/backlog and every dependency commitment field. Updated only this report and the draft's questions, Code Map and planning notes. `eng/verify-story-5.4.ps1` remains absent; no live readiness or story completion is claimed.

## Owner and contract gate recheck — 2026-10-04

Rechecked during `bmad-build 5.4`. Agents began clean on `main`, one commit ahead of its tracked remote. Three independent read-only investigations covered identity/authority, Conversations, and custody/hosting. Full scope and Branch B remain settled.

### Inspected source positions

| Checkout | Full HEAD |
| --- | --- |
| Agents | `844c86173435504cad7d20ed9a4c0e53fc586aca` |
| Parties reference and sibling owner | `5388884eec84b16545fdc008b2fc04547b0ed5b6` |
| Tenants reference | `a46c127c8be1d8d6ecccdf0d710e25befa644cc6` |
| Sibling Tenants owner | `29e92cdae986ab990ca640fdeb690b46f705deab` |
| Conversations reference | `f415298801097caa5f745e6976d67a79e4e2aab4` |
| Sibling Conversations owner | `d68378fff8aefa58a6158c50d798e7e0f7d80cb4` |
| EventStore reference | `b51978dd1d2a3721ad239db2623e1560377c7583` |
| Builds reference | `688eec9a4333245cc0ff7772115c769094471863` |
| Platform reference | `c3c473a4aa8da421896e4795fcdb0f4d2771a0be` |
| Sibling Platform owner | `909321c40e36074d345b707b942c74a7c8f5c075` |

Each identifier was obtained directly with `git rev-parse HEAD` in the owning checkout. Parties and Platform siblings contain pre-existing uncommitted work; sibling Tenants reports a dirty nested EventStore reference. Those changes were preserved read-only and are not described by the owning HEAD. No submodule was initialized, updated or deinitialized.

### Current evidence and unresolved commitments

- **Parties:** Installed source still lacks the complete identity/history contract. Sibling `IPartiesIdentityClient`, `PartyIdentityEvidence`, `HumanActorBindingEvidence`, `PartyIdentityAuthority` and `PartyIdentityQueryService` remain uncommitted implementation proposals. The owner notes still require production retention settings, production custody, purpose-scoped retained-history source/checkpoint after profile erasure, and cleanup/restore guarantees. `../parties/eng/verify-ext-parties-1.ps1` still unconditionally exits 1 in Live mode because authenticated persisted P-01–P-10/restart/restore/failure-injection lanes and production custody/history are missing; it was inspected, not executed. Final typed outcomes and complete Branch B acceptance remain pending.
- **Tenants:** The newer sibling revision adds audit/command tests, with no Client authority-contract change. `TenantProjectionEventHandler` still accepts equal/jumping sequences and supplies neither contiguous/current-head proof nor a global-administrator consumer. `TenantIdentity.ForGlobalAdministrators()` supplies the reusable exact source address.
- **Conversations:** The newer sibling revision has no `src/` change since `932548e2f222dce546f8f777b8827af5b43be417`. General get/append, deterministic MessageId/idempotency and Facilitator vocabulary remain reusable. Restricted membership/state/removal, accepted posting trace/provenance and typed MessageId lookup, authoritative denominator, Agents Service Principal content/access reads, and durable approved-deletion publication/checkpoint/retry/acknowledgement/quarantine remain incomplete. `ConversationDetailResult.Hidden` still collapses deletion/removal into Forbidden.
- **Custody/host:** Reference and sibling HEADs are unchanged from the previous recheck. Identity admission, digest-key and protection primitives do not implement the complete AD-29/30 custody profile, issuer replay, replicated denial spool, private exact-target capabilities or v21/v23 composition. Both host verifiers still defer live Agents composition to Story 5.6.
- **Agents:** `git diff 1b101eb0adca6387b66ec4b456e204f26e38a9c8 HEAD -- src test eng references _bmad-output/planning-artifacts/external-dependency-register.md` is empty. Existing authority, Party, approver, Caller, unsigned-extension and downstream-after-denial findings remain. The draft Code Map now explicitly includes the approver port/verdict and setup/audit query bypasses; preserve Story 5.3 outcomes.

Fresh authenticated `gh issue view --json number,title,state,url,body,comments` reads found [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54), [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4), [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) and [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) all OPEN with empty comments. No complete owner-accepted contract, immutable target, integration date or executable compatibility command was found.

### Disposition and validation

All four complete records remain `Uncommitted`. Required next input is links to their complete owner acceptance packets, as detailed in the [handoff](../specs/spec-story-5-4-dependency-unblock/evidence-and-acceptance.md#per-owner-acceptance-packet). This is missing delivery evidence; another approval of full scope or Branch B cannot supply it. The register's ready-for-dev and Available/exact-target execution gates remain unchanged.

Updated only this recheck and the draft's open question, Code Map and planning notes. Preserve frozen intent, original acceptance criteria, full scope, Branch B and draft/backlog. Verification is limited to preservation, document references and whitespace; no source implementation, build/test, owner message, Git mutation, deployment, compatibility command or consumer seam execution occurred. `eng/verify-story-5.4.ps1` remains absent and no live readiness or story completion is claimed.

**Subsequent specification approval — 2026-10-04:** The user replied "I accept". Recorded approval in the spec frontmatter and notes; no further specification approval is required. The response supplies no immutable owner target, integration date or executable verification command and does not waive the register's entry/execution gates. All four records remain Uncommitted; draft/backlog and the approved frozen intent/acceptance criteria are preserved. This approval update changes documentation only.
