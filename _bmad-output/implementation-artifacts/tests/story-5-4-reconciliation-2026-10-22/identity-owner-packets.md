# Story 5.4 identity owner packet investigation — 2026-10-22

Read-only owner investigation; only this report is written. No owner qualification,
Live/LiveReadiness command, deployment, credential retrieval, Git mutation,
submodule update or consuming Agents path was executed. Existing acceptance,
dependency status, recovery journals and owner attempt evidence are unchanged.

## Result

Neither EXT-PARTIES-1 nor EXT-CONV-AI-1 has a complete replacement runtime
commitment ready for acceptance. Canonical source commits now exist, but installed
targets, configuration/trust hashes and new integration dates remain absent.
Parties Live has no pass path. Conversations explicitly delivered a blocked,
NotSubmitted replacement packet. Its separate conformance receiver is source and
publish evidence; it is not installed or enrolled. No record may become Available
from these packets.

## Current source versus runtime targets

Read-only Git observations found each sibling HEAD equal to its local
`origin/main`; both commits are contained by `main` and `origin/main`:

| Repository | Current canonical source | Qualification distinction |
| --- | --- | --- |
| `../conversations` | `60f18214d7a4565d28096628af95d867ddd5e874` | All 31 files in the detached reviewed-source inventory exactly match this commit's blobs. This resolves the packet's historical uncommitted-source observation for component code only. It supplies no installed manifest, enrollment, runtime target or acceptance. Unrelated new Epic 16 files currently make the sibling worktree dirty. |
| `../parties` | `9a46682ad170ba70e395aa00012f86f09ef6b315` | Current source; no installed full Live target. The Live verifier blob SHA-256 is `63e8d7974b267f8baa8a8d14ae1e50503836de5aee21a589240dc55e36277bb1`. The sibling contains external modified/untracked query/release work. |
| `references/Hexalith.Conversations` | `ef3947078cb5ccf3d2b81ca684490e565e8cd04f` | Older reference copy; not the new owner-delivery checkout. |
| `references/Hexalith.Parties` | `4fc1124eb995a40553cb23b042d4a3c1add3dfb2` | Older reference copy; its Live verifier has the identical refusal-only hash above. |

The Parties source candidate in the latest direct owner handoff is
`ee4a4b8736131c0e7ce480b6dc419dcc0c266c3b`; it remains reachable from both
current Parties main refs. Reachability does not establish qualification.

The authoritative register and 2026-10-18 reconciliation retain these accepted
records: Conversations `87e8b4a6ec3224b20a560b0e83de857805925572`, Parties
`56cb4401c4179d431385ad3d931fefa402e4fc5a`; both Committed, date `2026-10-08`.
That date was missed. No new integration date is supplied by the new packets.
Conversations' blocked packet repeats the old date without new acceptance;
Parties' Platform decision form leaves the replacement date null.

## Conversations: delivered packet and missing inputs

Detached owner directory:
`/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261022-71fab44/`.
Its README says delivery is blocked and incomplete. The replacement packet has
`status: NotSubmitted`, null complete reviewed immutable target and null exact
verification command. Its `runtimeQualificationBinding` has null manifest hash,
and null paths/hashes for all twelve files: configuration, plan, hostProfile,
hostVerifierEvidence, sourceAnchor, partyAnchor, approvalAnchor, workerAnchor,
receiverAnchor, lookupAnchor, observerAnchor and controlAnchor.

Null installed bindings are liveManifest, servicePartyEnrollment,
workerMachineBinding, approvalIssuer, commandSourceAuthority,
sourcePublicationAuthority, hostDeliveryTransport, conformanceReceiver,
independentLookup and retainedOwnerAttemptJournal. The proposed live manifest
`/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json`
is absent. No credential values were read.

All stable blocker codes reported by final readiness are:

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

The runtime qualification document additionally requires a manifest with exact
tenant/foreign tenant, service Party, worker, message and zero-Agent Conversation,
UTC count window, namespace installation, two distinct receiver commit targets,
source/receiver storage and receiver/lookup principals. Independent bases are
gateway, parties, receiver, control and observer. Nine distinct installed
credential references are service, human, approver, worker, party, receiver,
control, observer and receiverChannel. Their names are input contracts, not
evidence of enrollment.

The separate receiver requires non-Development startup, exact tenant/target,
distinct receiver/lookup/worker/gateway principals, verified storage and gateway
origin, SDK workload trust/application-channel enrollment and current native
source authority. It contains no Agents package and performs no protected
deletion. Its identity is fixed to ContractConformance/OQ-5.4-1/Level 4 with
Agents runtime disabled. Story 8.3 and RQ-1 Level 5 remain separately gated.
`ext-conv-ai-1-deletion-conformance-receiver.md:146` explicitly says no receiver
is installed by the source change.

The changed verifier requires a newly accepted RuntimeQualificationBinding row,
matching accepted host Live pass-path/profile evidence and public anchors before
HTTP effects. Configuration must pin the absolute retained attempt-journal
directory. An exclusive target-set reservation enforces one attempt independent
of artifacts destination; interrupted and failed evidence remains retained. No
reservation or C1–C4 Live attempt was made by the owner delivery. Required
installation cannot be inferred from the component receiver's successful Debug
publish.

Retained verification is LocalComponentAndSyntheticControlFlow: 2,032 C# tests,
34 helper cases plus five subtests, and 11 synthetic runner cases. The 37-case
six-seam matrix is already included in the C# total. The accepted command's
historical refusal exited 1 at the target comparison, with zero cohort effects;
the `pwsh` Live operand did not execute. No new exact command with actual manifest
or binding files exists to present for acceptance.

## Parties: handoff, exact command and no pass path

Latest direct handoff read:
`../parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-runtime-2026-10-18/README.md`.
It retains 1,991 local passing tests across twelve lanes and explicitly calls
them Local source evidence. It proposes Parties `ee4a4b8736131c0e7ce480b6dc419dcc0c266c3b`,
EventStore `ab8fa115c486875be3a22860a273d5d68a26c749` and Platform
`2ac8e73c09fe631c5620723b54784d2e28e8941f`, without accepting a new integration
date or complete runtime target. Its proposed command, copied as data only, is:

```bash
test "$(git -C ../parties rev-parse HEAD)" = "ee4a4b8736131c0e7ce480b6dc419dcc0c266c3b" && test -z "$(git -C ../parties status --porcelain)" && test "$(git -C ../eventstore rev-parse HEAD)" = "ab8fa115c486875be3a22860a273d5d68a26c749" && test -z "$(git -C ../eventstore status --porcelain)" && test "$(git -C ../platform rev-parse HEAD)" = "2ac8e73c09fe631c5620723b54784d2e28e8941f" && test -z "$(git -C ../platform status --porcelain)" && pwsh -NoProfile -File ../parties/eng/verify-ext-parties-1.ps1 -Mode Live -EventStoreRoot ../eventstore -PlatformRoot ../platform -EvidenceDirectory /tmp/ext-parties-1-live
```

It cannot establish Available. Current `../parties/eng/verify-ext-parties-1.ps1:28–39`
unconditionally writes refusal evidence and exits 1 in Live mode, even when all
twelve required inputs are populated. It explicitly lacks complete authenticated
P-01–P-10 persisted-state/restart/restore/failure-injection lanes. LiveReadiness
performs read probes only and does not satisfy qualification.

The required Live input IDs, all false in the prerequisite packet's historical
presence-only observation, are:

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

No current secret values were inspected. Missing installation goes beyond input
presence: approved staging/production release/gateway/registry/tenant bindings,
eleven scoped credential roles and renewers, purpose custody and independent
lifecycle/source authority, all-copy inventory/destruction receipts, fenced
nonrollback restore, owner-approved fault targets, and an executable complete
P-01–P-10 verifier remain required.

## Platform actor-history prerequisite

Read the complete decision and command requirements in
`../platform/docs/implementation/ext-parties-1-platform-prerequisites-2026-10-18/`.
Its README and decision form say owner acceptance, installation authority, Live
execution and Available are all false. `command-bindings.json` is nonexecutable;
all fifteen B01–B15 operator/target/command/result slots remain unresolved. The
replacement Parties/EventStore/Platform commits, package/image digests,
root-submodule/runtime manifests, integration date, full command, working
directory, acceptance and passing evidence references are null.

All thirteen decision requests remain unresolved, with no assigned principal,
decision record or acceptance date:

| ID | Required decision |
| --- | --- |
| D01 | Exact deployment and authoritative registry/two-tenant bindings |
| D02 | Scoped credentials, renewal/revocation ownership |
| Q1 | Real custody backend and irreversible all-generation destruction |
| Q2 | Independent surviving lifecycle authority, fencing and time bounds |
| Q3 | Complete C01–C16 physical copy inventory, operators and original receipts |
| Q4 | Hard cryptographic expiry during backend/worker failures and operation bounds |
| Q5 | Original binding/revocation/idempotency mapping without extending predecessor deadlines |
| Q6 | Actual owning-host composition, snapshot codec and independent source admission |
| Q7 | Erasure/key-backup boundaries and minimal retained metadata |
| F1 | Approved actor-free successor continuity after predecessor expiry |
| D03 | Exact quarantined restore target/cut/authority/cleanup |
| D04 | Approved exact fault/lost-ack targets, bounds, observers and rollback |
| F2 | Complete immutable replacement/date/command and P-01–P-10 acceptance |

The dedicated plan is
`../platform/_bmad-output/specs/spec-production-actor-history-custody/implementation-plan.md`.
It assigns T2–T6 to symbolic `PLAT-ACTOR-HISTORY-1`: backend/authority,
production provider/codecs, actual-host registration, durable destruction/restore
and installed qualification. The Agents register has no PLAT-ACTOR-HISTORY-1
record, and the 2026-10-18 reconciliation expressly did not decide registration.
F1 and F2 remain explicit additional gates. A symbolic owner-work name cannot
substitute for an accepted immutable commitment.

Preserve Branch B Organization identity by immutable tenant/Party ID and
`party-actor-retention-v1` / `party-actor-history-v1`: 31,536,000 seconds from
original binding effective-at, exclusive expiry (`now < ExpiresAt`). Retry,
revocation, erasure, rotation, copying and restore do not renew that deadline.

## Read packet inventory and integrity

SHA-256 below identifies the exact read bytes, not an installed trust anchor,
owner approval or qualification result. The detached Conversations prefix is
`/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/delivery-20261022-71fab44/`:

| Relative file | SHA-256 |
| --- | --- |
| `replacement-acceptance-packet.blocked.json` | `579b59180e13df164757204113bae20f3c15a07d0867432631b5a1cb2ad023df` |
| `README.md` | `7f1696e577b0cf370ace4de3f8ea2a571ef6a451102b0d4419761d6c3454a348` |
| `final-readiness.json` | `22ca22085b6bb687940c2bd954f9945b6d53fffaf6fdd4e9facec124591d9ec1` |
| `final-verification.json` | `41e017befe97ea52c9777cefb0300845eb6ca210665773247c083d44977b7386` |
| `review-summary.json` | `2b79b032cfb580a247f48c60e525d07f81d5ff4174f5e0bfd971683946dd4f2e` |
| `reviewed-source-inventory.json` | `e8a06faad669ff58b01b883d60d87baddec5c2e5f094d3fc11e48789870e12d1` |
| `reviewed-working-tree.diff` | `c98da10ba0a9f270838910284cb6e717535ec49774c16aea7c369ca91e49f36a` |
| `reviewed-receiver-publish.json` | `2703df04dc36501aa30c2ce3215d83a9e2ee9766ad96f11f78572c5782e591af` |
| `final-accepted-command-refusal.json` | `fd9794d9321e4cdfd2c908fa5451a07352d10c80015210c6f8d19e52fbd12ef7` |
| `final-source-observations.json` | `e537a06edbc40c483f27a7d72f06256da28538e949d74af0f260e2ab2c0e72c1` |
| `evidence-inventory.json` | `044d6bc5d7c2af2486140236d37422d871b8de90691e226bd59b93454011372b` |

Owner source documents read:

| Path relative to Agents | SHA-256 |
| --- | --- |
| `../conversations/docs/implementation/ext-conv-ai-1-runtime-qualification.md` | `0087665ac0d04817ebd5005287f41cb760c390505f788fcf5a9ef96aa12f3130` |
| `../conversations/docs/implementation/ext-conv-ai-1-deletion-conformance-receiver.md` | `b19f449f7aeb59da5916e83076a77938fe5ea2dabdf02b170d6a87b9cb3fa98e` |
| `../conversations/_bmad-output/implementation-artifacts/spec-5-4-ext-conv-ai-1-owner-qualification.md` | `549aba1d58ff4e58d39ece11ca65b4aac6a25d09c93170635cf055f4796e4b4e` |
| `../parties/_bmad-output/implementation-artifacts/tests/ext-parties-1-runtime-2026-10-18/README.md` | `d299af695cdd9600eb680a463645256aa22cd4c4c9225e4cc66d137190a43db2` |

Platform prerequisite packet prefix is
`../platform/docs/implementation/ext-parties-1-platform-prerequisites-2026-10-18/`.
Decision/command forms, README, installation/qualification plans and validation
were read; supporting metadata/source/preservation/local-receipt records were
inspected for their stated evidence boundaries. All packet file hashes are:

| Relative file | SHA-256 |
| --- | --- |
| `README.md` | `d9130b9171fec5f6ea71f49a6e65b9a8f9507bd01e498d910d9a82d297edd5e1` |
| `decision-packet.json` | `dfe23b4aa297a8aadc0edecf44f21e62811e98e935715eaf510f445bfe6eab2c` |
| `command-bindings.json` | `657723d4692a99e57ec6cd3c2df9ad5a032c4db4e354716858125550e1312c69` |
| `installation-plan.md` | `d4b71da0b8c0db0c2d6b1528ba410cd36ea070b125b54d2e40c828ad474697f5` |
| `qualification-plan.md` | `2ec91e0c2d45f510e27312f985ca71f9967630c50e67b873af34ae286d1dc221` |
| `validation.json` | `b597ad50f1018cb5f42f6f47253a94b089a40e92e7bcb8a5d857b86b474aa348` |
| `sources.json` | `d7aef552a181df28b29adcf6c311596f99ab35652cbb0b7bda134825b220ba21` |
| `preservation.json` | `ca1258e08eccabd22e6a1a00ee3cf158f5c6964fee22737f06d942dc3f59e7c4` |
| `local-evidence-audit.json` | `f26b8d7302552f8ebf6db9b3d0f4ad70e2a0ac99c8fe09b92e18c5d788fea118` |
| `native-metadata.json` | `9cc814eb5f5bbbaa4d70162422ba63f2cda26508dd69b876761bf4fbb46e7fbe` |
| `github-metadata.json` | `9ecfa2b854bae2bdd2c71e9568859fdca41e2b13290253f5c613054c74506f7d` |
| `SHA256SUMS` | `218c5aa1baed00a7e2666f4f27cdde442b77b00c05240d78d7d38ff5580df9cf` |

No detached Parties evidence root exists under
`/home/administrator/hexalith-parties-evidence`. No newer complete Parties or
Conversations acceptance packet was found in the scoped sibling owner documents,
detached Conversations directory or older root reference copies. This is a
scoped discovery result; it does not claim undisclosed external deployments or
packets cannot exist.
