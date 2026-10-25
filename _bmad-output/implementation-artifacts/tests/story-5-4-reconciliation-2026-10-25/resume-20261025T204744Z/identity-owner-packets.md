# Story 5.4 identity packet reconciliation — resumed 2026-10-25

**Disposition: zero complete changed commitments for acceptance. EXT-PARTIES-1 and EXT-CONV-AI-1 remain Committed and unavailable.** The new Conversations October 25 delivery remains explicitly blocked. Parties source now contains its coordination packet and evidence-directory hardening, but Live remains refusal-only and the custody stop remains effective.

This is static reconciliation. No owner command, readiness probe, qualification, build, test, installation, credential retrieval, runtime call, attempt reservation or consuming Agents path ran. Only this report and its [JSON evidence](identity-owner-packets.json) were written. Original packets, attempts, failures and recovery journals were preserved.

## Every commitment field

| Field | Parties | Conversations |
| --- | --- | --- |
| `Owner` | Accepted accountable project owner unchanged; no new installed operator/authority enrollment. | Accepted accountable project owner unchanged; no new installed operator/authority enrollment. |
| `Repository` | Hexalith.Parties matches; sibling canonical source and Agents reference are separate checkouts. | Hexalith.Conversations matches; sibling canonical source and Agents reference are separate checkouts. |
| `RequiredArtifact` | Full Branch B identity, authoritative liveness, human actor binding and 365-day original-unit history scope retained; local P-01-P-10 cannot satisfy production custody. | All six seams and C1-C4 retained, including durable approved deletion and independent contract-conformance receiver; no subset acceptance. |
| `TargetVersionOrCommit` | Packet still preserves accepted 56cb4401c4179d431385ad3d931fefa402e4fc5a; old coordination pin is not current canonical delivery. Packet is now committed but no complete replacement target is offered. | October 25 packet candidate ed53473ea4c67aa10ffc46c9b951d9864fe263d4 is canonical ancestry; replacement target and completeReviewedImmutableTarget remain null. Current sibling is newer. |
| `TargetIntegrationDate` | Null; accepted 2026-10-08 remains historical/missed. Proposed 2026-11-13 feasibility and 2026-12-14 integration are superseded by stop direction, not active commitments. | Packet repeats historical/missed 2026-10-08. New installed-delivery spec (now in-progress) directs 2026-10-25 only if a complete packet is sealed that UTC date; no such sealed packet exists, so no changed date is submitted. |
| `CompatibilityContractAndVerificationCommand` | Accepted complete contract and historical exact command preserved. Candidate command is executable refusal only; current fresh-directory guard would refuse its already-existing output path. No production P-01-P-10 pass path or installed-hash binding. | Replacement exact command null, manifest/artifacts operands null. Current verifier guards committed exact target and twelve input hashes/manifest via RuntimeQualificationBinding; no complete accepted binding or installed bytes exists. |
| `RequiredEvidenceLevel` | Levels 4 and 5 retained. Exact-target installed Level 4 required for Available; local test custody is insufficient. | Levels 4 and 5 retained. Receiver is ContractConformance Level 4 only; Story 8.3/RQ-1 Level 5 deferred. Source/build closure is not installed Level 4. |
| `AcceptedStatus` | Accepted register remains Committed unavailable; proposed packet is explicitly unaccepted/unavailable. | Accepted register remains Committed unavailable; packet NotSubmitted explicitly incomplete/unaccepted/unavailable. |
| `ConsumingStories` | Complete accepted list and conditional consumption retained verbatim in acceptedRecords; no consumer gate relaxed. | Complete accepted list retained verbatim in acceptedRecords, including RQ-1; no consumer gate relaxed. |

The JSON retains all nine accepted fields and complete record sections verbatim. Branch B, immutable tenant/Party identity, independent decision authority, P-01–P-10, and exactly 365 fixed days from original binding-effective-at with exclusive expiry remain unchanged. Conversations retains all six seams and C1–C4; its separately authenticated receiver must contain no Agents package/runtime. Receiver owner qualification is Level 4; Story 8.3 and RQ-1 retain Level 5.

## Current canonical source

Full identifiers were obtained directly from Git. Root source-inventory.json separately verifies fresh remote main heads. Neither source ancestry nor checkout cleanliness proves installation.

| Checkout | HEAD | Working tree |
| --- | --- | --- |
| `/home/administrator/projects/hexalith/parties` | `9ee857b11ebcd13d44644be7c04c4ea6da5fdc44` | Clean |
| `/home/administrator/projects/hexalith/conversations` | `8039d2abcf41bcd2c01fa3dbe618cbec2f4f6da3` | Dirty; unrelated owner work preserved |
| `/home/administrator/projects/hexalith/agents/references/Hexalith.Parties` | `9c116910f9d712b82ae27ceb1946605d76c8a451` | Clean |
| `/home/administrator/projects/hexalith/agents/references/Hexalith.Conversations` | `8444207001d6ad3cd197c2ffba519317effa8b37` | Clean |

The accepted Parties target remains `56cb4401c4179d431385ad3d931fefa402e4fc5a`; the accepted Conversations target remains `87e8b4a6ec3224b20a560b0e83de857805925572`. Parties packet coordination still names old Parties `9c116910f9d712b82ae27ceb1946605d76c8a451`, EventStore `d41a3c55a684491d02f61c5ea98e0f74332b5d4d`, and Platform `90f33a59164d0dcfc21fb688f28651afa4d3df4b`. Those observations do not select a new complete target set.

Conversations packet records canonical source candidate `ed53473ea4c67aa10ffc46c9b951d9864fe263d4`, now an ancestor of current main. Compared with its 31-file inventory, current HEAD matches 28 files. The three changes are the deferred-work ledger, owner-qualification spec, and runtime-qualification documentation; inspected runtime/receiver/runner source matches. Source completeness is still distinct from an accepted installed target.

## Parties delivery

Packet: `/home/administrator/projects/hexalith/parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-owner-delivery-2026-10-25/replacement-acceptance-packet.json`; raw SHA-256 `5e1a89ff07f7663d603c27737b9242320c305b6595f4096cc4c96f29d851e3ea`. It remains `proposed-coordination`, with acceptance/availability/Live qualification false and `TargetIntegrationDate: null`. Its content is now committed in the clean current sibling checkout; this corrects the earlier audit’s uncommitted-source observation without changing acceptance.

The retained accepted command is data, not executed:

```bash
test "$(git -C ../parties rev-parse HEAD)" = "56cb4401c4179d431385ad3d931fefa402e4fc5a" && test -z "$(git -C ../parties status --porcelain)" && pwsh -NoProfile -File ../parties/eng/verify-ext-parties-1.ps1 -Mode Live -EventStoreRoot ../eventstore -PlatformRoot ../platform -EvidenceDirectory /tmp/ext-parties-1-live
```

The packet’s refusal command is also data:

```bash
pwsh -NoProfile -File eng/verify-ext-parties-1.ps1 -Mode Live -EvidenceDirectory /tmp/ext-parties-1-owner-delivery-20261025-live
```

The current verifier at `eng/verify-ext-parties-1.ps1:27` rejects an existing evidence directory, then atomically creates a permanent `.ext-parties-1-attempt` marker. Its Live branch at line 33 still exits 1 even with all twelve inputs. The candidate output directory already exists, so replay would now fail before recording another refusal. This hardening protects evidence; it does not add a production qualification pass path. No command was replayed.

The current Parties DI resolves history custody and retained-history readers from the host and registers no production provider. The retained 11-test persisted class used test custody; the historical broad Local attempt still has four failed EventStore cases and was not replaced by that focused pass. Neither result qualifies installed P-01–P-10.

All twelve current process presence checks remain false (only booleans recorded):

```text
EXT_PARTIES_GATEWAY_URL
EXT_PARTIES_TENANT_A
EXT_PARTIES_TENANT_B
EXT_PARTIES_PROVISIONER_CREDENTIAL
EXT_PARTIES_IDENTITY_WRITER_CREDENTIAL
EXT_PARTIES_READER_CREDENTIAL
EXT_PARTIES_POLICY_ID
EXT_PARTIES_RETENTION
EXT_PARTIES_EXPIRY_TRIGGER
EXT_PARTIES_CUSTODY_TARGET
EXT_PARTIES_RESTORE_TARGET
EXT_PARTIES_FAILURE_INJECTION_TARGET
```

Actual installed profile, configuration, plan, and public-trust hashes are absent. The historical HSM proposal also has null backendProfile/applicationConfiguration/qualificationPlan/publicTrust/completeInstalledTarget hashes. Packet/document digests do not fill these bindings.

The effective `../platform/docs/implementation/ext-parties-1-custody-direction-2026-10-25.md` stops Luna/FM and further production actor-history backend implementation under the unchanged contract. No complete non-AWS backend is verified. OpenBao remains conditional on an explicit guarantee amendment and is neither selected nor qualified. November 13 feasibility and December 14 integration dates are superseded historical proposals, not active commitments. The accepted October 8 date remains missed; no replacement date exists.

The unchanged prerequisite packet still has **661 null paths** and all fifteen B01–B15 executable bindings still have **165 null paths**. Their exhaustive paths and complete forms are in the JSON. Required outcomes remain:

| ID | Missing owner binding |
| --- | --- |
| D01 | Supply both exact environment bindings and release/enrollment records; registry issuer, authoritative deployment and two explicitly approved tenants. No existing target established. |
| D02 | Supply credential references, only-writer policies, renewal owners and procedures for every role. Generic shared bootstrap and recovery credentials are ineligible substitutes. |
| Q1 | Select a qualified existing backend/key hierarchy and show per-original-unit irreversible destruction of every generation, including historical immutable key backups. Reject soft delete, TTL, tombstones and merely denied reads. |
| Q2 | Name the independent authority outside the EventStore/OpenBao/ciphertext restore cut; writers, fence/reissue procedure, authenticated time source, skew and currentness bounds. |
| Q3 | Close C01–C16 with real locations/generations or authenticated absence; durable work and original receipts, owner-approved targets and independent probes; define receipt minimization and retention. |
| Q4 | Name cleanup, escalation and renewal operators; approve bounded retries, outage and receipt-completion limits and prove cryptographic expiry during outage. The five-second library wait is not an expiry SLA. |
| Q5 | Approve collision/idempotency identity and exact original-binding lookup for establish/rebind/revoke. Revocation effectiveAt cannot create a later deadline for predecessor actor evidence; SDK AdmitAsync lacks binding/logical ID. |
| Q6 | Approve package/bootstrap path in both actual owning hosts, Party snapshot codec and payload adapter, retained-source reader and independent source admission; no no-op/local-dev fallback. |
| Q7 | Preserve immediate profile ineligibility and independently bounded actor history; decide all key/backup erasure boundaries and minimal content-free anchor/receipt/continuation retention. Reconcile ordinary backup-key retention with hard actor-purpose expiry. |
| F1 | Approve proof producer/schema/version/scope/positions/epochs/currentness and erasure classification that proves complete unique successor versions/intervals without retaining expired predecessor actor/provenance/logical evidence. Current strict fold stays closed until accepted. |
| D03 | Supply exact isolated target, immutable cut, authority reconciliation/epoch hooks, independent observer, outage approval, cleanup scope and verified disposal bound. No target selected. |
| D04 | Supply exact workload UIDs and faults for admission/copy/destruction/receipt commit and lost acknowledgement; operator/observer credentials, duration, abort, cleanup and rollback. 6.1 rollout faults alone do not cover custody failure probes. |
| F2 | After decisions, implementation and installed probes exist, accept immutable source/package/image targets, integration date, exact complete Live command and all P-01–P-10 evidence. Keep status Committed until that owner-accepted command passes from clean exact checkouts. |

B01–B15 retain missing approvals, operators/observers, scoped credential references, resource allowlists, exact argv/working directory, timeout/cancellation, expected receipts, abort/cleanup and result references. The stop direction supplies none of those values; it does not authorize reopening backend work.

## Conversations delivery

New packet: `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261025-ed53473/replacement-acceptance-packet.blocked.json`; raw SHA-256 `20530c84523119b21e609d7e4dbdb95abe08aff867b58097dfa51105cac76932`. It is `BlockedReplacementAcceptancePacket`, `complete: false`, `accepted: false`, `available: false`, `NotSubmitted`. All 32 inventory hashes match retained files. This is the newer returned packet, supplementing the preserved October 22 delivery.

The current installed-delivery spec records an owner direction to use `2026-10-25` only if a complete packet is sealed that UTC date. It also explicitly says no existing inputs were supplied and they must stay null. No complete packet was found, and the new spec is dirty. That conditional date is distinguished from the blocked packet’s repeated historical October 8 date; neither is applied as a changed commitment.

The retained accepted command is data, not executed:

```bash
test "$(git -C ../conversations rev-parse HEAD)" = "87e8b4a6ec3224b20a560b0e83de857805925572" && test -z "$(git -C ../conversations status --porcelain)" && pwsh -NoProfile -File ../conversations/eng/verify-ext-conv-ai-1.ps1 -Mode Live -LiveManifest /home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json -ArtifactsPath /tmp/ext-conv-ai-1-live
```

The replacement target, exact pinned command, manifest operand and output operand remain null. The intended shape is `pwsh -NoProfile -File ../conversations/eng/verify-ext-conv-ai-1.ps1 -Mode Live -LiveManifest <unresolved> -ArtifactsPath <unresolved>`; it is not an executable accepted replacement. The expected manifest `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json` remains absent, and the process has no `EXT_CONV_AI_1_LIVE_MANIFEST` reference.

All ten installation bindings remain missing:

```text
liveManifest
servicePartyEnrollment
workerMachineBinding
approvalIssuer
commandSourceAuthority
sourcePublicationAuthority
hostDeliveryTransport
conformanceReceiver
independentLookup
retainedOwnerAttemptJournal
```

All twelve frozen files still have null path and SHA-256:

```text
configuration
plan
hostProfile
hostVerifierEvidence
sourceAnchor
partyAnchor
approvalAnchor
workerAnchor
receiverAnchor
lookupAnchor
observerAnchor
controlAnchor
```

The JSON retains all null paths and the eleven original blocker codes. The live manifest still needs actual tenant/foreign-tenant/Party/worker records, two immutable receiver targets, current source namespace and storage identities, current UTC count-window data, independent gateway/Parties/receiver/control/observer bases and nine distinct scoped enrolled credentials. Merely naming references cannot establish authority. No installed bytes were available to hash.

Current `Get-OqQualification` requires protection Committed, a shared host/secrets target, an accepted `RuntimeQualificationBinding`, exact raw manifest plus all twelve file hashes, accepted Live host evidence with `passPath: true` at its target/profile, distinct receiver/lookup/worker principals, a canonical absolute retained journal directory and exact POST body inventory. These checks precede `Reserve-OqAttempt`. The register has no complete Conversations runtime binding, and the missing profile/manifest/trust cannot be replaced with a null-map digest.

The published conformance receiver is a separate SDK service whose source rejects Development startup and requires its own enrolled application identity. Its project has no Agents reference. The historical local restore graph’s actual bytes still match SHA-256 `01601c212646a8c3cc5d9446208a26b0c0ee4b6d2e4a1c588ce803d3229c1058`: 52 libraries, zero Hexalith.Agents libraries. This proves local dependency closure only. No installed receiver/lookup authentication, live principals or installed restore graph is supplied.

Retained native XML confirms 2,247 passing tests across five projects, including 31 receiver cases, and 37 passing repeated six-seam cases already included in the suite total. The packet also retains 45 passing tooling/synthetic-runner cases. These historical local results do not establish current installed Level 4 evidence. The current server Program does not opt into the deletion runtime; authority defaults remain unavailable until installed providers are supplied.

## Acceptance and execution boundary

**No complete changed identity commitment is submitted.** Passing C1–C4 is not required before accepting a complete replacement as Committed; the blockers here are the absent complete target, executable exact command, installed hashes/authority and supported integration date. After complete replacement acceptance and prerequisite availability, exact-target P-01–P-10 and C1–C4 qualification may establish Available only through full required persisted passing evidence. No consuming Story 5.4 build may resume on these incomplete packets.

The JSON hashes 70 inspected public files and distinguishes historical source/test evidence from actual installed bindings. Static inspection is limited to named local paths and supplied artifacts; it is not a survey of every remote installation. Original attempts and recovery journals were never opened for writing.

## Concurrent source update reconciled

At `2026-10-25T20:56:39.594476+00:00`, the Conversations installed-delivery spec changed from `draft` to `in-progress`. Its earlier observed SHA-256 was `3f44a8e3c47af9f2bebf661fb7afac3d16e7400b5bc5079543025af5adac561b`; current SHA-256 is `4d56053d9b617de6a24c7d2ea98e7faabf072f03ce4c5ffbd72e868d9a63c680`. Replacing only that status string reproduces the exact earlier hash, establishing that no other bytes changed. The JSON retains both observations and the earlier static-verification result. The root's failed verification receipt remains separate evidence.

The two retained delivery directories are still the only ones present; their blocked packets are unchanged, and the expected installed manifest remains absent. Workflow status does not accept the conditional October 25 date or establish a complete commitment. Zero identity replacements remain ready for acceptance. No owner command ran during this recheck.
