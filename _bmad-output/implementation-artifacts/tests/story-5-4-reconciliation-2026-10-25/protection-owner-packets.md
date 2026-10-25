# Story 5.4 protection owner packet reconciliation — 2026-10-25

**BlockedNotSubmitted. Zero complete changed protection commitments are ready for acceptance.** The new October 25 delivery completes source corrections and retains component evidence; its own packet leaves G1–G7 open. EXT-PROTECTION-1 remains **Uncommitted**, with no complete immutable target or installed profile/engine/roles/trust. Proposed integration date **2026-11-02** remains unaccepted.

This report records local file/hash and read-only Git inspection. No Live/Full/cohort command, build/test, installation, infrastructure probe, destructive experiment, credential read, acceptance, or register/source change was performed. The only writes are this report and its JSON companion. Original attempts and recovery journals were not opened for writing.

## Complete commitment review

The JSON companion contains all nine exact register fields and the complete v21/v23 section. The full artifact, every consumer and both evidence levels remain required.

| Field | Disposition |
| --- | --- |
| `Owner` | Unchanged accountable owner: EventStore Maintainer. |
| `Repository` | Unchanged Hexalith.EventStore; origin remote https://github.com/Hexalith/Hexalith.EventStore.git. |
| `RequiredArtifact` | Full exact register artifact preserved; all G1–G7 remain open. Source corrections do not satisfy installed production delivery. |
| `TargetVersionOrCommit` | Register TBD; latest packet completeImmutableOwnerTarget is null. Canonical observed HEAD is source metadata only. |
| `TargetIntegrationDate` | Register TBD; explicit 2026-11-02 proposal remains unaccepted and is not a completed integration promise. |
| `CompatibilityContractAndVerificationCommand` | Register contract and Command:TBD preserved. Candidate Live command exists, but accepted ProtectionProfileSha256 and installed inputs are absent. |
| `RequiredEvidenceLevel` | Levels 4 and 5 preserved. Current evidence is source/component only; installed Level 4 and consuming Level 5 remain unfulfilled. |
| `AcceptedStatus` | Uncommitted preserved; packet BlockedNotSubmitted must not be accepted or advanced. |
| `ConsumingStories` | Exact register consumers preserved; packet has a trailing Markdown delimiter loss at RQ-1 but no consumer may be omitted. |

The owner JSON copies `ConsumingStories` with the closing Markdown backtick missing from `RQ-1`, a transcription defect caused by broad delimiter trimming. The authoritative field is preserved verbatim here: 5.6, 5.8, 6.1–6.4, 6.8, 7.1–7.4, 7.7, 8.1–8.3, 8.8; `RQ-1`. The other eight fields match after removing only complete scalar code formatting. This report does not repair or supersede the owner packet.

## Canonical source and exact command

- eventstoreSibling: `d41a3c55a684491d02f61c5ea98e0f74332b5d4d` on `main`, local `origin/main` `d41a3c55a684491d02f61c5ea98e0f74332b5d4d`, ancestor check True; unrelated working-tree changes preserved: `M _bmad-output/implementation-artifacts/2-13-dapr-notification-distribution-qualification.md`, ` M _bmad-output/implementation-artifacts/spec-2-13-dapr-notification-distribution-qualification.md`.
- agentsRootReference: `d41a3c55a684491d02f61c5ea98e0f74332b5d4d` on `main`, local `origin/main` `d41a3c55a684491d02f61c5ea98e0f74332b5d4d`, ancestor check True; clean.

The latest owner packet observed an earlier source baseline; those hashes are historical observations, never replacement target commitments. Both current source copies contain the October 25 audit. No fetch or checkout occurred.

Candidate command, from the Agents repository root, retained for review only:

```bash
python3 ../eventstore/tools/verify-ext-protection-1.py --mode Live --profile /home/administrator/hexalith-protection-evidence/ext-protection-1/accepted-profile.json --evidence-root /home/administrator/hexalith-protection-evidence/ext-protection-1/attempts
```

The entrypoint and Python interpreter exist; static source inspection confirms an input-dependent success path. Preconditions require the accepted ISO date, clean exact full target reachable from local `origin/main`, and `ProtectionProfileSha256=<actual raw profile-file hash>` in the existing compatibility field. Current installed preconditions fail. Calling Live would create an attempt directory before rejecting the Uncommitted record, so it was not used as a diagnostic. Recovery-only `RECOVERED` / `EXACT_ORIGINAL_RECOVERED` may exit zero but has `qualificationPassed:false` and cannot advance availability.

## Actual hash and installed-input checks

| Item | Observed SHA-256 / state |
| --- | --- |
| Raw committed 41-case plan | `c1e7759a2687c122dd182aa48f1828e531166c0157a04b2ae0eba35e9ac4d10c`; equals packet and verifier raw-file semantics; no case executed. |
| Accepted profile | `/home/administrator/hexalith-protection-evidence/ext-protection-1/accepted-profile.json` does not exist; hash is null. |
| Accepted engine / executor / observer / enrollment / capability trust | No actual accepted installation or corresponding hashes supplied. |
| Public installed HMAC runtime reference | `31c28a9d1dd21eed8ca4d0c421088cb946b262c624ba461d0795cab92cd1feb3`; `ext-secrets-1-hmac`, 51 null fields. |
| Public installed key mappings | `f4e650995f0b35882377e01fb4f30b3944b018aec136e16496b6d349dd595ffe`; ordinary snapshot semantics, lifecycle/all-copy unqualified. |
| Semantic provider installation gaps | `b1084315e00c7da47ad31bb6c8a4f8db73e8098702afff913b6158dff6a95292`; 20 null installation fields. |

Packet/file hashes below identify evidence bytes, not installed trust. Null maps, HMAC credentials, source fixtures, tenant strings or public endpoints cannot substitute for enrolled protection roles or capability trust.

The missing closed-profile members are:

- `schema`
- `targetCommit`
- `planSha256`
- `tenantReferences.primary`
- `tenantReferences.other`
- `engineBinary.path`
- `engineBinary.sha256`
- `roles.executor.executable`
- `roles.executor.executableSha256`
- `roles.executor.publicAnchorFile`
- `roles.executor.publicAnchorSha256`
- `roles.executor.credentialReference`
- `roles.executor.identity`
- `roles.executor.installationManifest`
- `roles.executor.installationManifestSha256`
- `roles.observer.executable`
- `roles.observer.executableSha256`
- `roles.observer.publicAnchorFile`
- `roles.observer.publicAnchorSha256`
- `roles.observer.credentialReference`
- `roles.observer.identity`
- `roles.observer.installationManifest`
- `roles.observer.installationManifestSha256`
- `roles.enrollment.executable`
- `roles.enrollment.executableSha256`
- `roles.enrollment.publicAnchorFile`
- `roles.enrollment.publicAnchorSha256`
- `roles.enrollment.credentialReference`
- `roles.enrollment.identity`
- `roles.enrollment.installationManifest`
- `roles.enrollment.installationManifestSha256`
- `roles.executor.installationManifest.files[].path`
- `roles.executor.installationManifest.files[].sha256`
- `roles.observer.installationManifest.files[].path`
- `roles.observer.installationManifest.files[].sha256`
- `roles.enrollment.installationManifest.files[].path`
- `roles.enrollment.installationManifest.files[].sha256`
- `capabilityTrust[].audience`
- `capabilityTrust[].issuer`
- `capabilityTrust[].keyFamily`
- `capabilityTrust[].keyVersion`
- `capabilityTrust[].publicAnchorFile`
- `capabilityTrust[].publicAnchorId`
- `capabilityTrust[].publicAnchorSha256`
- `capabilityTrust[].publicAnchorVersion`
- `capabilityTrust[].tenantId`
- `capabilityTrust[].trustProfileRevision`

Schema must be `ext-protection-1-profile/v2`. Role manifests require complete absolute file/hash inventories containing their executable. Role identities, credential references and actual P-256 fingerprints must be distinct; capability keys must differ from all transport keys and across tenants. Public trust must retain old verifiers and bind exact issuer/audience/tenant/family/version/anchor/revision. No such installation can be derived from this absent profile.

All public runtime-reference null fields were freshly checked:

- `qualification.daprApiCredentialReference`
- `qualification.enrollment.appChannelCredentialReference`
- `qualification.enrollment.appId`
- `qualification.enrollment.callerAuthenticatorReference`
- `qualification.enrollment.callerScopeAuthorityReference`
- `qualification.enrollment.identity`
- `qualification.enrollment.publicAnchorFile`
- `qualification.enrollment.version`
- `qualification.enrollmentCredentialReference`
- `qualification.executor.appChannelCredentialReference`
- `qualification.executor.appId`
- `qualification.executor.callerAuthenticatorReference`
- `qualification.executor.callerScopeAuthorityReference`
- `qualification.executor.identity`
- `qualification.executor.publicAnchorFile`
- `qualification.executor.version`
- `qualification.isolatedTargetId`
- `qualification.observer.appChannelCredentialReference`
- `qualification.observer.appId`
- `qualification.observer.callerAuthenticatorReference`
- `qualification.observer.callerScopeAuthorityReference`
- `qualification.observer.identity`
- `qualification.observer.publicAnchorFile`
- `qualification.observer.version`
- `runtime.acceptedReplacementCommit`
- `runtime.actorRoleAuthorityReference`
- `runtime.allCopyInventoryReference`
- `runtime.allCopyLifecycleAuthorityReference`
- `runtime.audience`
- `runtime.authenticatedRequesterTransport`
- `runtime.compromiseGuardMirrorReference`
- `runtime.compromisePublisherReference`
- `runtime.compromiseSubscriberCredentialReference`
- `runtime.compromiseSubscriptionReference`
- `runtime.currentInventoryAuthorityReference`
- `runtime.currentRevocationReference`
- `runtime.independentSignerAccount`
- `runtime.independentSignerCredentialReference`
- `runtime.issuer`
- `runtime.migrationAuthorityReference`
- `runtime.nonrollbackJournalReference`
- `runtime.profileNotBefore`
- `runtime.profileReference`
- `runtime.profileValidUntil`
- `runtime.profileVersion`
- `runtime.protectionOwnerReference`
- `runtime.publicAnchorRetentionReference`
- `runtime.publicTrustReference`
- `runtime.replayExactScope`
- `runtime.replayRegistrarCredentialReference`
- `runtime.restoreTarget`

All semantic installation fields remain null:

- `awsAccountId`
- `region`
- `authorityTableArn`
- `authorityTableId`
- `checkpointBucketOwnerAndEnrollmentVersion`
- `originalBucketOwnerAndEnrollmentVersion`
- `epochKeyArnsAndIds`
- `kekProtectorArnsAndIds`
- `independentPlacementAndRestoreCut`
- `executorIdentityAndCredentialReference`
- `observerIdentityAndCredentialReference`
- `enrollmentIdentityAndCredentialReference`
- `publishedPublicTrustAndCurrentRevocation`
- `installedComponentArtifactDigest`
- `installedDaprTransportProfileDigest`
- `installedClosureAndTerminationVerifierDigests`
- `measuredCompletionReserveBytes`
- `approvedRetentionSchedule`
- `itemizedCurrentProviderQuote`
- `explicitNativeApproval`

## New delivery and remaining G1–G7 requirements

New EventStore source corrects independent owner currentness/genesis and actual actor transitions through the Platform Dapr bridge; P39 now verifies native signer/no-issue/successor/intent originals. These fixes supersede older statements that those exact source seams are absent or permanently refuse. They do not supply an independently installed issuer, actual custody or restore proof. The adopted Dapr-only application architecture is already decided. The newer semantic provider proposal uses a component-internal AWS reference implementation while applications stay on existing Dapr SDKs. Its F2/F3 preparation is explicitly unapproved/unexecuted; no native deployment or expenditure is authorized by architecture adoption.

| Gap | Remaining delivery |
| --- | --- |
| G1 — Open | Installed six-operation custody/key delivery and all-or-none manifest composition, effective hold pins and authenticated original vectors remain unqualified. Adopted semantic source and concurrent public Dapr adapters are not installations. |
| G2 — Open | Owner-currentness/genesis/actual-transition source seam is corrected. Independent private/recovery and lifecycle-authorizer backends, durable hold/dispatch-reservation issuer, published trust, registrar, enrollment and separate admission/revocation/activation evidence remain absent or unqualified. |
| G3 — Open | Actual complete copy/holder closure, independent authority placement/native restore fencing, alternate/tainted retirement, terminal native deletion, survivor recovery and original signed vectors/restore barriers remain unqualified. |
| G4 — Open | Mixed-root directory snapshot representation/policy and bindings remain unresolved. Current ProtectSnapshotAsync serializes the entire snapshot under one binding resolved with requestedTarget:null. Installed writer/anchor/scheduler and source-plus-target restore/replay qualification remain missing. |
| G5 — Open | Independent executor, read-only persisted-state observer and enrollment programs/ACLs, same enrolled store/deployment, engine/source/inventory pins, actual driver fixtures and one-attempt authority are not installed. |
| G6 — Open | Native signing/no-issue/recovery/intent correlation is now checked offline. Installed authenticated append/current-online-authority, complete inventory and physical/restore provenance remain unproved; role signatures or summaries cannot supply them. |
| G7 — Open | Complete canonical coordinated source/package target, package-lane locked restore, actual installed profile/engine/roles/trust and accepted integration date are unresolved. Retained package restore exits 1 with NU1102; concurrent lock changes were preserved. |

The package-lane NU1102 failure is retained owner evidence; this reconciliation did not rerun restore or claim that current package availability was checked. Mixed-root directory snapshot policy and actual installed writer/anchor/scheduler/source-plus-target replay remain open. The full v21/v23 obligations include exact online authority, atomic cancel/reserve ordering, original receipts, replacement-key compare, private registrar, loss/recovery and restore negatives; component successes do not waive them.

## Packet and retained evidence integrity

- Repository audit, detached blocked packet and Agents root-reference audit are byte-identical: `839d22de1a6ecde5e7bb302689f404d22819ab014d175977aeb3ae61dfbfde99`.
- Verification manifest `d7f339eea2d1044435fe0fad1d6c06e55531c5a99e854a7a393ce18fe93cea55` matches its source-inventory and blocked-packet pins.
- All 40 retained command log hashes match. All 28 captured source-file hashes match current files.
- 2 of 3 component binary hashes differ at the current mutable Debug output paths. Historical component results are retained; current binaries are not exact captured or installed qualification evidence.
- Retained final owner results report 217 Server component tests and 66 verifier/native interoperability tests passing; source mode reports 41 planned cases with `liveExecuted:false`. Those tests were not rerun here.
- Earlier failures, package restore failure, declined alternatives and original results remain retained under the owner evidence directory.

| Changed mutable artifact | Captured SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Server/bin/Debug/net10.0/Hexalith.EventStore.Server.dll` | `f08ef36f30c9b2bf338dd8a6f6319cb085d3c7dfd93e588668ff6248ce3b6cfb` | `ab3b350237867d32233ea4c644cf1f20c7de55def351826a516c6624e660c08d` |
| `/home/administrator/projects/hexalith/eventstore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll` | `efb608afbd7c8ab84a4391f7f256cdee4b9dab1b718c4898ef5e1753afdf16eb` | `986089c57d77e399a5ea6054b4d33ce44634829f98d818651b8edd556eb58f71` |

The retained OpenBao negative-control result and repository copy still hash to `6efa381bc70b3de57a24142e5e679f53781c7f89d5ab6740fb7664966f30888e`: `ObservedRevival`, `g1Qualified:false`. It records ordinary Raft restoration reviving deleted material/plaintext. No probe was rerun. This is a preserved counterexample, not a fresh experiment against the proposed shared-cut provider.

## Inspected evidence inventory

All hashes below are SHA-256 of actual file bytes, including existing line endings.

| Path | SHA-256 |
| --- | --- |
| `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md` | `5bf35ac2857664c0589966a10944cd66278af85242c7c1f08e31ae29aa40e8a2` |
| `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-22.md` | `aeb11856b83cdede109f5f24408928f468f24b6bd50e056c1e32abf2f75628e6` |
| `/home/administrator/projects/hexalith/eventstore/docs/implementation/ext-protection-1-delivery-2026-10-25.json` | `839d22de1a6ecde5e7bb302689f404d22819ab014d175977aeb3ae61dfbfde99` |
| `/home/administrator/projects/hexalith/eventstore/docs/implementation/ext-protection-1-delivery-2026-10-25.md` | `dd14a953584af5354c66844d70a2598077d863482aa647bf29ed6c402ad8f55f` |
| `/home/administrator/projects/hexalith/eventstore/docs/implementation/ext-protection-1-owner-acceptance.md` | `95b9e515afee4c2cd49d11b97ccdd08ec4f4efbb665e0fbf90eabda97013ab06` |
| `/home/administrator/projects/hexalith/eventstore/docs/implementation/ext-protection-1-verifier-protocol.md` | `2c3c0c18e475af60d27fe47d6282e51e535ba5d592b05238f43ffacd8cbcafab` |
| `/home/administrator/projects/hexalith/eventstore/tools/verify-ext-protection-1.py` | `6810d3ede10d42c2e5fdd63b031d1de6bc2f3eb0e6f0ab2d5d24f762f052624d` |
| `/home/administrator/projects/hexalith/eventstore/tools/ext_protection_1.py` | `0db4502871ad63fbcab4c8095d8d5b753707520d803d08763b87c5ae0d06d540` |
| `/home/administrator/projects/hexalith/eventstore/tools/ext-protection-1/plan.json` | `c1e7759a2687c122dd182aa48f1828e531166c0157a04b2ae0eba35e9ac4d10c` |
| `/home/administrator/projects/hexalith/eventstore/_bmad-output/implementation-artifacts/spec-ext-protection-1-owner-delivery.md` | `259fc40ee5c87bab4ea26f14364f3f9e593ae69985db7f1ed2781244efb7525e` |
| `/home/administrator/projects/hexalith/eventstore/_bmad-output/implementation-artifacts/spec-ext-protection-1-story-5-4-owner-qualification.md` | `be3bd8d370e12a250de62c3909dbec69ed5a49b9dddefcaf453ef64381627c05` |
| `/home/administrator/projects/hexalith/agents/references/Hexalith.EventStore/docs/implementation/ext-protection-1-delivery-2026-10-25.json` | `839d22de1a6ecde5e7bb302689f404d22819ab014d175977aeb3ae61dfbfde99` |
| `/home/administrator/projects/hexalith/agents/references/Hexalith.EventStore/docs/implementation/ext-protection-1-owner-acceptance.md` | `95b9e515afee4c2cd49d11b97ccdd08ec4f4efbb665e0fbf90eabda97013ab06` |
| `/home/administrator/hexalith-protection-evidence/ext-protection-1/delivery-20261025T065854Z-9c732cee/commitment-packet.blocked.json` | `839d22de1a6ecde5e7bb302689f404d22819ab014d175977aeb3ae61dfbfde99` |
| `/home/administrator/hexalith-protection-evidence/ext-protection-1/delivery-20261025T065854Z-9c732cee/verification-manifest-final.json` | `d7f339eea2d1044435fe0fad1d6c06e55531c5a99e854a7a393ce18fe93cea55` |
| `/home/administrator/hexalith-protection-evidence/ext-protection-1/delivery-20261025T065854Z-9c732cee/source-inventory-final.json` | `474bed220d1cee1bc83ebf92eb4feaecd7de0a5fb2451e9e766fea74ecf604b9` |
| `/home/administrator/hexalith-protection-evidence/ext-protection-1/delivery-20261025T065854Z-9c732cee/installation-observation-final.json` | `9e17c88d5c2094666d3d60ba0c053c41450d4a877085aef50bcd3659efea76e1` |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/provider-key-installation-20261017T100804405564Z/runtime-reference.json` | `31c28a9d1dd21eed8ca4d0c421088cb946b262c624ba461d0795cab92cd1feb3` |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/provider-key-installation-20261017T100804405564Z/provider-key-mappings.json` | `f4e650995f0b35882377e01fb4f30b3944b018aec136e16496b6d349dd595ffe` |
| `/home/administrator/projects/hexalith/platform/docs/implementation/ext-secrets-1-semantic-provider-2026-10-22/final-installation-gaps.json` | `b1084315e00c7da47ad31bb6c8a4f8db73e8098702afff913b6158dff6a95292` |
| `/home/administrator/projects/hexalith/platform/docs/implementation/ext-secrets-1-semantic-provider-2026-10-22/OWNER-APPROVAL.md` | `00e12dadad4de7be5e4bb020017fafe54ca5d14389bac9355c0a9e5894a8218c` |
| `/home/administrator/projects/hexalith/platform/docs/implementation/ext-secrets-1-erasure-authority-2026-10-20/OWNER-APPROVAL.md` | `e7a5387aff07074090f529c341484c5ffdf6f1989cf001b04d0e1920e484e692` |
| `/home/administrator/hexalith-protection-evidence/ext-protection-1/option-a-feasibility-20261018T113800Z-1dbe62f55af0434684a9602a3824bfe9/raft-probe-attempt-6.result.json` | `6efa381bc70b3de57a24142e5e679f53781c7f89d5ab6740fb7664966f30888e` |
| `/home/administrator/projects/hexalith/platform/docs/implementation/ext-secrets-1-option-a-feasibility-2026-10-18/native-result.json` | `6efa381bc70b3de57a24142e5e679f53781c7f89d5ab6740fb7664966f30888e` |

## Cohort disposition

OQ-5.4-1 remains blocked before phase 0 because EXT-PROTECTION-1 is Uncommitted and its accepted complete target/profile/roles/trust do not exist. Do not submit this incomplete packet for acceptance, consume a cohort attempt, or invoke the consuming Agents build. After complete acceptance and installation, preserve the exact order: protection → secrets without host references → host composition with all Agents consumers refusing → mutual cases. Both Platform records advance only on the whole required passing exact-target run. Maintain one attempt per accepted target set and retain every original attempt/recovery journal. Level 5 remains with consuming qualification and RQ-1.
