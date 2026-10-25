# Story 5.4 identity owner packet reconciliation — 2026-10-25

**Disposition: no complete changed identity commitment is ready for acceptance.** EXT-PARTIES-1 and EXT-CONV-AI-1 remain Committed and unavailable. The new Parties packet is proposed coordination with an unresolved date and no installed qualification. Conversations still supplies its October 22 blocked NotSubmitted packet.

This is static reconciliation. No Live, LiveReadiness, Full, Local, build, test, installation, network request, credential retrieval, attempt reservation, Git mutation, or consuming Agents path was executed. Existing owner evidence was read and hashed only. Owner working trees have unrelated/concurrent changes; this report does not claim they stayed globally unchanged.

[Machine-readable evidence](identity-owner-packets.json) retains complete accepted sections, all nine fields, source observations, exact command strings, copied public packet contents, input-presence booleans, file hashes, and exhaustive null-field paths. Hashes in this report identify bytes; they are not approval or installed public trust.

## All commitment fields

| Field | Parties | Conversations |
| --- | --- | --- |
| `Owner` | Copied accepted accountable owner; no new installation authority. | Copied accepted accountable owner; no new installation authority. |
| `Repository` | Hexalith.Parties; matches. | Hexalith.Conversations; matches. |
| `RequiredArtifact` | Complete Branch B identity/liveness/human actor/history scope restated; local P-01–P-10 does not deliver installed custody. | All six core seams retained verbatim, including durable approved deletion and the OQ receiver composition. |
| `TargetVersionOrCommit` | Packet leaves accepted 56cb4401c4179d431385ad3d931fefa402e4fc5a; coordination HEAD excludes uncommitted new implementation. No complete replacement target. | Replacement target and completeReviewedImmutableTarget null; canonical component source exists but no complete installed target. |
| `TargetIntegrationDate` | Null. Owner explicitly declines to invent a date. | Repeats missed 2026-10-08; no explicit new promise or acceptance. |
| `CompatibilityContractAndVerificationCommand` | Full contract restated. Existing command retained as display text; standalone executable command runs unconditional refusal and lacks exact checkout/installed-hash guards. No qualifying command. | No replacement exact command; manifest and artifact operands null. Current runtime verifier requires additional accepted RuntimeQualificationBinding absent from register. |
| `RequiredEvidenceLevel` | Levels 4 and 5 retained; local test custody is not installed Level 4 qualification. | Levels 4 and 5 retained; receiver component scope Level 4 only, consuming Story 8.3/RQ-1 Level 5 still gated. |
| `AcceptedStatus` | Packet proposed (not a valid register state); accepted=false and available=false. Register Committed unchanged. | Packet NotSubmitted; accepted=false and available=false. Register Committed unchanged. |
| `ConsumingStories` | Full consumer list and conditional scope restated; no subset availability. | Full consumer list retained; no consumer may execute before Available. |

The full record text and extensions are retained in the JSON. Branch B, immutable tenant/Party identity, independent actor authority, 365 fixed days (31,536,000 seconds) from original binding effective-at and exclusive expiry remain unchanged. The six Conversations seams, separately authenticated receiver, no Agents package/runtime, fixed OQ Level 4 scope and deferred consuming Level 5 remain mandatory.

## Canonical source observations

Full IDs below came directly from Git; canonical reachability uses locally recorded `origin/main`, without fetch. A reachable HEAD is not an installed or accepted target.

| Checkout | HEAD | On local origin/main |
| --- | --- | --- |
| `/home/administrator/projects/hexalith/parties` | `9c116910f9d712b82ae27ceb1946605d76c8a451` | true |
| `/home/administrator/projects/hexalith/conversations` | `8444207001d6ad3cd197c2ffba519317effa8b37` | true |
| `/home/administrator/projects/hexalith/agents/references/Hexalith.Parties` | `9c116910f9d712b82ae27ceb1946605d76c8a451` | true |
| `/home/administrator/projects/hexalith/agents/references/Hexalith.Conversations` | `8444207001d6ad3cd197c2ffba519317effa8b37` | true |

Both sibling working trees are dirty. The root reference copies have the same HEADs, but are separate checkouts. The Parties packet/spec/new persisted test were untracked, and its verifier and DI source were modified at observation. No complete clean commit contains those new local changes. Conversations has 29/31 exact reviewed-snapshot matches at current HEAD; only `.github/workflows/ci.yml` and `_bmad-output/implementation-artifacts/deferred-work.md` differ. The runtime/receiver files still match the reviewed component snapshot.

Parties proposes the following coordination only:

| Repository | Packet canonical target | HEAD at this reconciliation |
| --- | --- | --- |
| parties | `9c116910f9d712b82ae27ceb1946605d76c8a451` | `9c116910f9d712b82ae27ceb1946605d76c8a451` |
| eventStore | `d41a3c55a684491d02f61c5ea98e0f74332b5d4d` | `d41a3c55a684491d02f61c5ea98e0f74332b5d4d` |
| platform | `90f33a59164d0dcfc21fb688f28651afa4d3df4b` | `7c56ad6352a74edec62450668e106ec8d6011e40` |

All three proposed IDs are ancestors of their locally recorded origin/main. Platform has moved beyond the packet pin. The packet separately preserves Platform `f045ac70d107bad32a1c1d2463f8e0f6fe7353cd` at the aborted Local script start, then `90f33a59164d0dcfc21fb688f28651afa4d3df4b` for the focused persisted class; that class does not establish a full joint Platform qualification.

## Parties packet, commands and evidence

Packet: `/home/administrator/projects/hexalith/parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-owner-delivery-2026-10-25/replacement-acceptance-packet.json`; raw SHA-256 `5e1a89ff07f7663d603c27737b9242320c305b6595f4096cc4c96f29d851e3ea`. It says `proposed-coordination`, `accepted=false`, `available=false`, `liveQualified=false`, `localPersistedEvidenceIsAvailable=false`. `TargetIntegrationDate` is null and the target field deliberately preserves the old accepted target.

Its standalone executable command is data here, not executed:

```bash
pwsh -NoProfile -File eng/verify-ext-parties-1.ps1 -Mode Live -EvidenceDirectory /tmp/ext-parties-1-owner-delivery-20261025-live
```

Working directory is the Parties repository. This command has no exact-target/clean-checkout or installed input hash guard and always exits 1 at `eng/verify-ext-parties-1.ps1:28–39`. Even all twelve populated environment variables cannot make it qualify. Its fixed evidence directory already contains the owner refusal and must not be overwritten. The packet also embeds the historical accepted command as escaped display text, not a newly executable exact-target replacement.

The newly added Local lane exercises real processor/query/fold logic using a disposable durable directory and `TestOnlyIdentityHistoryCustody`; production DI still requires host-supplied custody/history readers. Reading retained XML verified 11/11 passing focused tests, zero skipped/not-run, including P-01–P-10 and missing-provider refusal. Reading the original broader XML verified 446 passed and 4 failed out of 450 EventStore tests. The original Local command stopped there before Parties; the focused class was a separate run and does not erase that failure. The four failures are `StreamsControllerTests.NativeHeadGatewayCrossesRealWorkloadMiddlewareWithJwtBindingsOrExactSignedProof` variants (null observation).

All current process presence checks were false (values never printed):

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

No actual installed Parties profile/configuration/qualification plan/public trust hash is supplied. The packet file digest above is not one of those bindings. `/home/administrator/hexalith-parties-evidence` was absent. Runtime environment proof cannot be inferred from release packages or test results.

Remaining prerequisite decisions are supplied by the owner packet and remain delivery blockers, not a request to reconsider accepted policy:

| ID | Missing delivery |
| --- | --- | --- |
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

The Platform prerequisite form still has 661 null paths, retained exhaustively in `parties.missingPrerequisiteFields`. These span actual staging/production deployment and tenant/registry bindings; eleven scoped credential roles with separate renewal/revocation authority; key/copy/backup inventory C01–C16; original-unit lifecycle, independent current/nonrollback authority, receipt, restore, fault and actor-free successor bindings. Counts are null-field inventories, not counts of independent decisions. `PLAT-ACTOR-HISTORY-1` remains a symbolic owner work item and supplies no accepted register record.

All fifteen executable prerequisite command bindings remain unresolved (165 null paths, including their command/authority/target/evidence details):

| ID | Binding |
| --- | --- | --- |
| B01 | Release allowlist/profile/configuration validator |
| B02 | Environment-layer install executor |
| B03 | Application release executor |
| B04 | Tenant registry enrollment and two-tenant verification |
| B05 | Credential resolve/renew/revoke and missing-generation readiness probes |
| B06 | Runtime isolation and trust enforcement probes |
| B07 | Backend purpose/key capability and all-generation destruction probes |
| B08 | Current lifecycle/time/epoch and fence-reissue probes |
| B09 | Independent persisted source/snapshot/copy/work/receipt observer |
| B10 | Original-unit cleanup and original outcome lookup |
| B11 | Pre-expiry backup capture and immutable version verification |
| B12 | Isolated restore, reconciliation and cleanup executor |
| B13 | Fault and lost-ack injection/rollback executor |
| B14 | Actor-free successor certificate producer/verifier |
| B15 | Full exact-checkout compatibility command |

## Conversations packet, commands and installation

Packet: `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261022-71fab44/replacement-acceptance-packet.blocked.json`; raw SHA-256 `579b59180e13df164757204113bae20f3c15a07d0867432631b5a1cb2ad023df`. The detached owner directory contains only `delivery-20261022-71fab44`; its README and final readiness explicitly retain incomplete/blocked status. No newer complete detached packet or installed manifest was found.

The accepted command remains (data only):

```bash
test "$(git -C ../conversations rev-parse HEAD)" = "87e8b4a6ec3224b20a560b0e83de857805925572" && test -z "$(git -C ../conversations status --porcelain)" && pwsh -NoProfile -File ../conversations/eng/verify-ext-conv-ai-1.ps1 -Mode Live -LiveManifest /home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json -ArtifactsPath /tmp/ext-conv-ai-1-live
```

It cannot run at current HEAD and its proposed manifest path is absent. The replacement exact shell command, immutable target, manifest hash and every frozen-input path/hash remain null. `EXT_CONV_AI_1_LIVE_MANIFEST` is absent in this process. No credential value was read.

All twelve missing frozen files:

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

All ten missing installed bindings:

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

All eleven retained readiness blocker codes:

```text
APPROVAL_ISSUER_MISSING
COMMAND_SOURCE_ATTESTATION_UNAVAILABLE
HOST_DELIVERY_TRANSPORT_MISSING
HOST_LIVE_PASS_PATH_NOT_QUALIFIED
LIVE_MANIFEST_MISSING
OWNER_TARGET_NOT_ACCEPTED
PARTY_ENROLLMENT_MISSING
PROTECTION_NOT_COMMITTED
PUBLICATION_AUTHORITY_UNAVAILABLE
RECEIVER_NOT_INSTALLED
WORKER_BINDING_MISSING
```

The complete installed manifest must supply schema/target, exact tenant and foreign tenant, service Party, current worker, deterministic message/zero-Agent Conversation, UTC count window, namespace installation, two distinct immutable receiver targets, source/receiver storage identities, and receiver/lookup principals. It requires independent gateway, parties, receiver, control and observer bases, plus nine distinct enrolled credential references: service, human, approver, worker, party, receiver, control, observer and receiverChannel. These roles must be enrolled, scoped and current; a string reference does not prove any of them.

The configuration must pin a canonical absolute retained owner attempt-journal directory. The complete plan must hash every POST body and no extra body. The manifest and all twelve frozen input files require accepted raw file SHA-256 bindings; they are not replaced by a canonical null-map digest. The host evidence must bind the accepted Platform target and profile with `verificationMode: Live` and `passPath: true`; component evidence is insufficient. There are no installed bytes to hash for any of these fields.

The receiver source is a separate SDK service with no Agents dependency. Its reviewed component requires non-Development startup; distinct receiver, lookup, worker and gateway principals; exact tenant/target/storage; signed workload/application-channel trust and native current source authority. It performs no protected deletion, fixes ContractConformance/OQ-5.4-1/Level 4, and keeps Agents runtime disabled. Its successful historical Debug publish proves only an artifact; no separately enrolled/authenticated installation or independent lookup is evidenced. Story 8.3 and RQ-1 Level 5 remain gated.

The current C1–C4 runner checks committed target acceptance and the additional RuntimeQualificationBinding before HTTP effects, then reserves one retained target-set journal. The accepted register has no complete new binding. No reservation was made by this reconciliation.

## Preservation and verification

Only this report and its JSON attachment were written. No owner packet, accepted record, original attempt, recovery journal or command output path was opened for writing. The JSON records raw hashes of inspected packets, commands, docs, retained XML/refusal evidence, and the register. All nine accepted fields and full record sections are retained. File-hash verification is static evidence only and does not advance availability.

Required next delivery: clean complete coordinated source targets, a new explicit integration date per owner, accepted executable full qualification commands, independently installed authoritative inputs and exact frozen hashes. Until those exist, there is no complete identity commitment to submit for acceptance and no permitted qualification or Agents continuation.
