---
title: 'Approved MVP scope: operational security first, advanced custody deferred'
status: approved-applied
created: 2026-10-28
decision_id: DEC-5.4-MVP-CUSTODY-2026-10-28
approval_source: 'Owner instruction apply recommended after the pragmatic custody assessment'
scope: 'Planning, acceptance boundaries and Platform owner instructions; no runtime enablement'
---

# Approved MVP custody scope amendment

The owner approved the recommendation to defer the proposed Luna HSM, independent current-authority cluster and immutable-archive stack from MVP work. Reuse the installed operational OpenBao, inventory and HMAC services and their retained evidence. Keep tenant isolation, current authorization, revocation, replay protection, durable content-free security audit and existing retention periods.

This is an applied work-priority and acceptance-scope decision. It introduces a bounded identity/operational-security milestone within Story 5.4, not an immediately executable production MVP or a new dependency contract. The parent story remains in progress. No dependency becomes Available, no launch gate passes and no consuming runtime or qualification command gains permission from this decision.

## Why the scope changes

The earlier next action was to obtain independent custody, authority and archive infrastructure for full physical non-revival qualification. Platform's [D2 assessment](../../references/Hexalith.Platform/docs/implementation/story-5-4-coupled-delivery-2026-10-25/D2-provisioning-request-2026-10-28.md) found none installed in the referenced handoff and assessed one conditional vendor option. Actual supported survivor migration, irreversible partition-scoped destruction and current-authority rollback enforcement remained unproven. Provisioning that stack now would create substantial operational work before establishing that the construction qualifies.

The retained OpenBao `ObservedRevival` result remains authoritative: deleting a software-managed key does not prove that restoring an older backup cannot revive it. Keep that limitation explicit. Do not relabel ordinary access denial, active-data deletion, rotation or a local tombstone as irreversible all-copy erasure.

## Before and after

| Before | Approved current direction |
| --- | --- |
| Obtain Luna/independent authority/WORM resources as the immediate continuation for full coupled delivery | Defer that provisioning/evaluation request from MVP work; no procurement or custom custody-platform fallback is selected |
| Treat complete custody, protection and host delivery as the single ongoing work package | Prioritize ordinary identity/operational-security contract and installation work as a bounded milestone; retain dependent protected-content work as blocked/deferred |
| Full owner records and OQ-5.4-1 govern live acceptance | The same complete records and ordered qualification still govern every executable seam; no partial pass or synthetic replacement record is created |

Existing full acceptance contracts remain the requirements for deferred features. Their required artifacts, target/date/command pins and AcceptedStatus values are unchanged. This amendment changes work priority and adds milestone acceptance; it supplies no reduced live contract. A future proposal to execute a reduced seam needs the owning maintainers' complete exact-scope replacement packet and explicit acceptance under the [dependency register](external-dependency-register.md). A subset must never advance the old broader record.

## Retained work and exact blocked paths

“Retained work” means contract/package work and installation within already authorized scope. It is not permission to consume an unavailable seam or replay a completed installer.

| Path | MVP work disposition | Live boundary |
| --- | --- | --- |
| Tenant isolation, current tenant/actor authority, revocation and role correctness | Retain | Fresh authoritative adapters and complete accepted dependencies remain required; unavailable defaults continue to refuse |
| Branch B Organization Party identity by tenant/id | Retain | Current classification/liveness and full Parties availability remain required; Organization identity grants no human actor or approver authority |
| Current Human binding and approver basis | Retain contract/correctness work | Still blocked without current policy, unique current binding and custody authorization; deferring historical reads does not remove these checks |
| Canonical HMAC, timing, replay admission before command construction | Retain existing operational services and passing evidence | Current accepted profile, credentials, independently required authority and installed host adapters remain required |
| Durable denial spool, restricted recovery worker and safe audit | Retain | The actual accepted durability/replica failure model, ACLs and persisted recovery evidence remain required; no single-node durability claim is upgraded |
| Retained human attribution after profile erasure | Defer dependent live delivery | Remains disabled without approved custody/history/restore evidence; keep 365 fixed days from binding-effective-at with exclusive expiry |
| Sensitive-content capture, context, generation, proposals and posting | Defer while protection is unavailable | FR-34 and the actual protection/host contracts continue to block; no plaintext or no-op protection fallback |
| Protected export, requester delivery, key unwrapping, retention destruction and manifest erasure | Defer affected custody-dependent operations | No destructive completion, `Erased` guarantee, all-copy receipt or restore non-revival claim without complete applicable owner evidence; existing holds and restrictions remain enforced |
| Full Story 5.4 acceptance, coupled host/protection qualification and RQ-1 | Not completed by this milestone | Complete exact-target accepted owner deliveries and the existing qualification order remain mandatory |

The public API [availability filter](../../src/Hexalith.Agents.Server/Api/AgentDependencyAvailabilityFilter.cs) checks all four complete records. [Default availability](../../src/Hexalith.Agents.Server/Ports/CommittedAgentDependencyAvailability.cs) and [trusted command admission](../../src/Hexalith.Agents.Server/Ports/UnavailableAgentTrustedCommandAdmission.cs) refuse by default. The Parties [current identity service](../../references/Hexalith.Parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs) also requires custody for eligible Humans; the [approver resolver](../../src/Hexalith.Agents.Server/Ports/ProjectedApproverPolicyResolver.cs) cannot bypass that dependency. [Interaction intake](../../src/Hexalith.Agents.Server/Application/AgentInteractions/AutomaticInteractionIntakeOrchestrator.cs) enforces protection availability. These source gates remain intact. There is currently no reduced live verifier pass path.

## Backup, retention and recovery limits

Keep existing retention triggers/durations, legal holds, exclusive expiry and Open export decisions. No data is purged, retention period extended, evidence lock created or backup policy changed by this amendment. Required live expiry, historical attribution and erasure outcomes remain blocked where their enforcement is unavailable.

Operational readiness still needs an owner-returned backup lifetime, restore privileges/procedure, current credentials/profile and meaningful recovery tests. No numerical backup lifetime or verified restore policy is supplied by this decision. Ordinary encrypted backups can retain recoverable key material; the MVP milestone promises no protection against revival through old backups. Restoration must not be used to claim current authority or completed irreversible deletion. This limitation is not a waiver of FR-34 or deferred full custody acceptance.

Preserve host `192.168.1.30`, Parties Branch B, envelope L/S/O/H/R `300/30/600/86400/604800`, the 365-fixed-day binding-history policy, accepted target/date history and every original journal. AWS remains excluded. Original installation/maintenance evidence remains timestamped evidence, not fresh health; expired profiles or unresolved preservation baselines cannot be extended by editing a document.

## Which authorities remain required

| Authority | Current disposition |
| --- | --- |
| Operational HMAC profile, exact-key inventory and revocation authority | Reuse installed `ext-secrets-1-hmac-authority` and inventory within their observed operational scope. Freshness, scope and credential checks still apply; same-cluster administration is not independent lifecycle authority. |
| Independent business-decision issuer and current actor/role authority | Retain as ordinary-security requirements. The retained custody health reports `decisionSignerInstalled:false`; current human/approver adapters and accepted public trust remain missing or blocked. The HMAC custodian cannot mint business approvals. D4/D8 owners must return actual evidence. |
| Replay admission and security-audit durability/currentness | Retain the existing exact-stream authority, restricted credentials and accepted persistence/failure model. These are ordinary host deliveries, not the proposed nonrollback lifecycle cluster; actual complete host evidence remains required. |
| Nonrollback lifecycle authority | Defer provisioning the proposed independent cluster/witness for epoch cuts, allocation exclusions and restore-resistant custody currentness. Its guarantees remain prerequisites for dependent Human/history/content/erasure paths wherever the full contracts require them. |

## Acceptance of the scope-amendment delivery

- Given the owner's approval, when MVP work is selected, then ordinary identity/operational-security work is retained and Luna/authority/WORM procurement and custom-platform construction are deferred.
- Given the amended planning and Platform owner records, when their instructions are followed, then no deferred provisioning command, completed installer, native experiment or qualification is executed as part of this change.
- Given current source and dependency records, when readiness or availability is evaluated, then existing blockers stand; current Human/approver, history and content paths gain no exception.
- Given a software-managed key deletion or restored backup, when reporting the outcome, then no irreversible all-copy erasure or non-revival claim is made from that result.
- Given the original complete contracts, when the bounded milestone is recorded, then parent Story 5.4 and RQ-1 remain incomplete and all existing target/date/command/status pins remain exact.

## Planned operational-security delivery — incomplete

The completed deliverable here is the **scope amendment**. The planned **operational-security delivery** is separate and remains pending; these names are planning labels, not new dependency IDs or runtime statuses.

| Required deliverable | Existing owner and completion evidence |
| --- | --- |
| Current operational profile, exact credential/key scopes, inventory, rotation and revocation | Platform operational custody maintainer: current accepted profile and fresh authorized observations; retained installation evidence is reused, not treated as perpetually current |
| Tenant/Party/Human/approver authority and independent decision issuance | Tenants/Parties/Conversations and D4/D8 identity owners: exact current actor/role/binding/issuer evidence and accepted integration; current Human custody coupling stays blocked until legitimately satisfied or explicitly separated by a complete accepted contract |
| Authenticated ingress, replay admission, denial spool and restricted recovery | Platform host owner: installed adapters/ACLs, accepted replica placement/failure model and persisted exact-identity replay/denial recovery evidence |
| Backup lifetime, privileged restore policy and recovery verification | Existing application-host/storage/backup owner: accepted policy and authorized recovery evidence returned through `owner-decisions.json` → `mvpOperationalInputs.backupAndRestore` |
| Executable acceptance | Respective dependency owners and register owner: complete accepted exact-scope packets and their supported commands. No reduced command or availability pass path is supplied here. |

Given those actual deliveries and legitimate accepted contracts, when their exact owner verification runs, then only its complete accepted scope may be evaluated. With any missing input or inseparable deferred dependency, the planned operational delivery remains incomplete and consuming paths remain blocked.

## Implementation handoff and reopening

The project owner/Product scope decision is applied. Platform prioritizes the already installed operational services, current profile/access maintenance through the existing procedure, and the existing replay/spool/host/authority deliveries. Parties and Conversations owners retain their complete contracts and blockers. No owner message, runtime deployment, purchase or appointment is performed here.

The immediate maintenance prerequisite belongs to the **cluster credential/Kubernetes administration owner**: reconcile the historical metadata drift identified by the [retained next-baseline blocker](/home/administrator/hexalith-secrets-evidence/ext-secrets-1/next-baseline-reconciliation-20261028T070042156838Z/blocker.json), return sanitized historical proof and explicit acceptance, and establish the accepted public preservation baseline. Current managed fields alone are insufficient; editing the Secret, enabling new audit retroactively or replaying maintenance supplies no reconciliation. Only after this prerequisite is satisfied may the existing operational custody maintainer follow the [accepted maintenance procedure](../../references/Hexalith.Platform/eng/ext-secrets-1/README.md#separate-isolated-custody-maintenance-2026-10-28). No maintenance command runs for this amendment.

The **existing Platform application-host and storage/backup owner** returns backup lifetime, privileged restore subjects/scoped credential references, the accepted restore procedure and authorized recovery evidence through [the owner form](../../references/Hexalith.Platform/docs/implementation/story-5-4-coupled-delivery-2026-10-25/owner-decisions.json), `mvpOperationalInputs.backupAndRestore`. Its state is `PendingOwnerDelivery`; actual policy/evidence references and the backup lifetime remain null. `requestedEvidenceDestination` names the future artifact location, not an existing delivery. The same form records the maintenance baseline and current-profile returns with their accountable owners and pending states. Its D3–D9 disposition overlay distinguishes retained ordinary work, deferred advanced work and inseparable blocked work without changing original contract fields.

Future executable separation must first identify a genuinely independent operation-specific contract and its effect on current Human identity, approvers, host composition and protection. Owners then supply complete artifacts, immutable targets, dates, installed profiles/trust, exact verification commands and evidence for explicit replacement acceptance. Until that exists, use only the already allowed contract/package work and accepted owner commands. Do not add a scope flag, bypass the shared filter, fabricate partial dependency IDs or call the existing full Live verifier as a reduced test.

Reopen advanced custody only through a later explicit scope decision with an accountable operator, supported provider construction and funded resource scope. Resolve supported survivor migration, destruction/original-outcome recovery and current-authority enforcement before provisioning. Preserve G1–G7 and OQ-5.4-1's `protection → secrets → host-refusing-consumers → mutual` order, original attempt/recovery identity and acceptance prerequisites. No new date, attempt or qualification permission is created.

Implementation cost for this amendment is document/evidence reconciliation only. Deferred provider, installation and qualification effort has no verified estimate or completion date; none is invented. [Verification and preservation evidence](../implementation-artifacts/tests/mvp-custody-scope-2026-10-28/verification.json) records the resulting changes separately from runtime delivery.
