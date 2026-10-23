# Platform owner packet reconciliation — 2026-10-22

Investigation reads the full current `EXT-SECRETS-1` and `EXT-HOST-1` register records, the accepted 2026-10-18 reconciliation and all ten post-2026-10-18 acceptance packets listed below. No qualification command, packet preparation, deployment, credential read, target installation, Git mutation or recovery-journal operation was performed. This report is the only file written by this investigation. No OQ-5.4-1 attempt was consumed.

The source deliveries are complete within their frozen source-only scopes. The newest shared owner handoff is canonical commit `3b4151df682448557d58a09027a05c5b41b6ce31`. It supplies neither a complete deployment target nor a complete executable joint-cohort packet. Both replacement acceptances remain null; both dependencies remain unavailable.

## Canonical source and installation observations

- Sibling `../platform` HEAD and its locally recorded `origin/main` are `3beff28eede537eb36e84cdb93900af2556202d7`. The tree has a pre-existing modified `references/Hexalith.Builds` pointer. No fetch or mutation was performed.
- Every candidate in the inventory was checked directly by `git merge-base --is-ancestor <candidate> origin/main` and `git branch -r --contains <candidate>` in the Platform repository; all are ancestors of its recorded `origin/main` and contained by that ref.
- `60472462e4b43f4d328260199b22655639d36dd3` adds supplied custody credentials and evidence digests. `c0edad7607eaa5b96f8d601097c76db244a38a2f` fixes replay of a rewritten Full attempt exit by rechecking the required Passed cells and all evidence digests. `3b4151df682448557d58a09027a05c5b41b6ce31` records the shared spec done and retains that correction; c0→3b changes only the shared spec.
- `git diff --stat 3b4151d..HEAD -- eng src docs/implementation/ext-story-5-4-shared-prerequisites-2026-10-22.md` is empty. Later HEAD changes include AppHost, tracking/evidence and submodule changes outside those paths. Those later commits are not the target named in the existing pair.
- Agents root reference `references/Hexalith.Platform` HEAD is separately `c65d87a6f8f4a05fa3bea4e24ac778bf23f12cd6`, clean on its recorded `main...origin/main`. It is not the `../platform` target addressed by the packets. No checkout was changed.

## Every new acceptance packet read

All entries below have `acceptedReplacementCommit=null` and `dependencyAvailable=false`. Secrets entries also explicitly have `readiness=IncompleteSourceCandidate`, `qualificationCompleted=false` and `liveCompatibilityAuthorized=false`. Full absolute paths are preserved because packet commands bind them.

| Packet path | Candidate commit | Raw file SHA-256 |
| --- | --- | --- |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/completion-20261019/acceptance/replacement-acceptance-packet.json` | `ad1c770d680f70af3a9638306635ba514426ef36` | `d8c09616e2b801e86ca4b38e20d4e6a9ac6227b76c4341337e720f0d49138f6d` |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/continuation-20261020/acceptance-preview/replacement-acceptance-packet.json` | `b1e03da74b714683b2b281d076985a8e938f0769` | `cc44901bc6fc8f46ad86664ba4b6da75989d25e3796da903f879b913f74b33fe` |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/continuation-20261020/acceptance/replacement-acceptance-packet.json` | `69689ab00b2056c6f311af9cb4120fb38c8c766d` | `ff0a0a8a2c450b03e6feeecd83ab3a986b970b86f8d35d256540ff86db412246` |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/f1-20261020/acceptance/replacement-acceptance-packet.json` | `9954f2b841a6d454d9cf9c6bfa0e5830ff0c146c` | `de48993ad279088a3cd1e87bf80ac54ae60f596e5c0e2216a2a25b12394f38c3` |
| `/tmp/ext-story-54-shared-60472462/host/replacement-acceptance-packet.json` | `60472462e4b43f4d328260199b22655639d36dd3` | `89ab2813fb0eb122f62f03d21fed71b79d66f654a7bf0e40c0c590b876fe7faf` |
| `/tmp/ext-story-54-shared-60472462/secrets/replacement-acceptance-packet.json` | `60472462e4b43f4d328260199b22655639d36dd3` | `824a9a53ede3bd426cad953be2de3d8f4490f7511515dc2682d40feae2b4266f` |
| `/tmp/ext-story-54-shared-c0edad76/host/replacement-acceptance-packet.json` | `c0edad7607eaa5b96f8d601097c76db244a38a2f` | `f58ce9ea7c89ef9e126af4441511e83388184a523aa93cfae9070af9b693fd72` |
| `/tmp/ext-story-54-shared-c0edad76/secrets/replacement-acceptance-packet.json` | `c0edad7607eaa5b96f8d601097c76db244a38a2f` | `e79d67b309c340df5440ac2d8ff1b7f0acb31c5e43607239d3b2a145ee1fdd43` |
| `/tmp/ext-story-54-shared-3b4151d/host/replacement-acceptance-packet.json` | `3b4151df682448557d58a09027a05c5b41b6ce31` | `ab0dd589f27ac8841ea9b1a441b452909bae15efce9175edfc0bb63a22e10b8a` |
| `/tmp/ext-story-54-shared-3b4151d/secrets/replacement-acceptance-packet.json` | `3b4151df682448557d58a09027a05c5b41b6ce31` | `4e6e30f608a86906f15bb4ca46e7a8b4e64cac0518146ecee09616afc3459ed1` |

Hash semantics: “raw” means SHA-256 of exact retained file bytes. The owner secrets canonicalizer is sorted compact ASCII JSON **without** a trailing newline; its packet/configuration/binding raw bytes equal that owner canonical representation. The host canonicalizer uses sorted compact ASCII JSON **with one trailing newline**; its packet/profile raw bytes equal that owner canonical representation. Adding/removing a newline changes a raw hash. `planSha256` values bind owner-generated plan objects rather than a packet file; `publicAnchorSha256` binds the canonical anchor-hash map, not any concatenated anchor bytes.

Chronology: 2026-10-19 completion (`ad1c770`) is superseded by the 2026-10-20 continuation preview (`b1e03da`), final continuation (`69689ab`), F1 source contract (`9954f2b`), and then shared source/host delivery (`6047246`), its evidence correction (`c0edad7`) and final shared handoff (`3b4151d`). Prior four secrets preparation reports have 75 overlapping missing-input identifiers each; the newest shared default configuration has 83, because it supplies none of the earlier five historical provider/HMAC/inventory/tenant references and no accepted replacement pin. These are overlapping runtime/derived/implementation/acceptance blockers, not 83 distinct secret values. The 604 secrets packet has an empty `cohortBlockingInputs` because it omitted register evidence; the c0 and 3b packets correctly include the four cohort blockers.

Historical baseline also read: `/home/administrator/hexalith-host-evidence/ext-host-1/runtime-composition-abbec40/replacement-acceptance-packet.json`, raw SHA-256 `d5427ae5ad6fc9c706a25d3ab557c6b28a06eac0a2b85bcf733bc188b336e354`, target `abbec4093715c1db854433119b32d4e16df20d08`. It has the same empty-profile hash and no pinned `--profile` in its command. It is not a new completed deployment packet.

## Current shared pair and exact command bytes

Both current packets pin `3b4151df682448557d58a09027a05c5b41b6ce31`. The following commands are existing packet text, reproduced for review only. Neither command was executed. They are standalone owner entry points and do not implement the accepted joint phase order.

Host command:

```bash
test "$(git -C ../platform rev-parse HEAD)" = "3b4151df682448557d58a09027a05c5b41b6ce31" && test -z "$(git -C ../platform status --porcelain)" && bash ../platform/eng/verify-agents-host.sh --mode Full --artifacts-path /tmp/ext-host-1-full --profile /tmp/ext-story-54-shared-3b4151d/host/agents-deployment-profile.json
```

Secrets command:

```bash
test "$(git -C ../platform rev-parse HEAD)" = "3b4151df682448557d58a09027a05c5b41b6ce31" && test -z "$(git -C ../platform status --porcelain)" && env EXT_SECRETS_1_RUNTIME_CONFIGURATION=/tmp/ext-story-54-shared-3b4151d/secrets/runtime-reference.json EXT_SECRETS_1_ACCEPTANCE_PACKET=/tmp/ext-story-54-shared-3b4151d/secrets/replacement-acceptance-packet.json pwsh -NoProfile -File ../platform/eng/verify-ext-secrets-1.ps1 -Mode Live -EventStoreSourceRoot ../eventstore -ArtifactsDirectory /tmp/ext-secrets-artifacts -EvidenceDirectory /tmp/ext-secrets-evidence
```

| Current pin | Value |
| --- | --- |
| Host profile file | `/tmp/ext-story-54-shared-3b4151d/host/agents-deployment-profile.json` |
| Host profile SHA-256 (owner canonical and raw) | `f93680dfb9ae76f963005f0058622672c567612532591555e501dd3d13db7083` |
| Host plan SHA-256 (owner canonical object) | `8d9716c0348f40fc9caab75aa3ebc1d8df9718aa7f6a4234c2a91be0de225679` |
| Secrets configuration file | `/tmp/ext-story-54-shared-3b4151d/secrets/runtime-reference.json` |
| Secrets configuration SHA-256 (owner canonical and raw) | `b1a99a90583489424fcc236541d15ccf7bd0abe8ca8c8e33b9dccc966d01b8c8` |
| Secrets complete 234-case plan SHA-256 | `16dd493ff0414fea13be95f137d372e54088881550665aa3239a5814a9e862ed` |
| Runtime qualification binding file raw SHA-256 | `993d095a411989574b98da3dc79f8c9ab32e6b01d3b4216e028827a1d54323b8` |
| Executor anchor SHA-256 | `None` (JSON null) |
| Observer anchor SHA-256 | `None` (JSON null) |
| Enrollment anchor SHA-256 | `None` (JSON null) |
| Public anchor-map SHA-256 | `53f138498b53f560ce6957f054d6feb7d27cdee2190c23bbf4331a3fcb89e160` |
| Secrets blocked-inputs raw SHA-256 | `3fbf3190f23f9cef9c53a3b9d256c04ff8ff3d05a23f754603a7ca8142567e39` |
| Host exact-command.sh raw SHA-256 | `8583ec95f9e07557a986c5c475c106f734ed26998001f19df42b529ab0aa6539` |
| Secrets exact-command.sh raw SHA-256 | `b20f28cb08092ec9463652d46f746ed7b131ae8e52990ec8509d1e285fee60a1` |

The three JSON null anchor values are absent trust, not literal anchor digests. The non-null aggregate public-anchor-map hash is only the hash of those three nulls and establishes no installed P-256 authority. The current secrets configuration leaves `runtime.acceptedReplacementCommit=null`; changing it after acceptance would change the configuration hash and require a fresh exact packet.

Integration date remains the historical `2026-10-08` in both owner source contracts and the host profile/plan. It is overdue and is not a new promised date. `ext-secrets-1/qualify.py:acceptance()` rejects any register date other than literal `2026-10-08`; host `missing_references()` likewise enforces that date and the host plan hashes it. Neither owner supplies a new integration date. A new future integration date is therefore a proposal requiring an owner source correction and fresh profile/plan/packet hashes before acceptance; assigning an arbitrary date in Agents would produce a command that rejects. No new date is inferred here. Host remains `192.168.1.30`, Parties branch B, L/S/O/H/R = 300/30/600/86400/604800, and the export-overlap disposition remains Open.

## Static command outcome and exact blockers

Against the current sibling checkout both copied commands would first fail the exact-HEAD guard (current HEAD differs; its Builds pointer is also dirty). Even from a clean exact 3b4151d checkout, the current host profile is incomplete and Full returns refusal before starting suites. Full now has a component pass path for a complete profile and thirteen exact Passed cells with a persisted evidence digest. That pass deliberately leaves `dependencyAvailable=false`, `productionEngineRegistered=false`, `liveEffects=0` and `missingRuntimeInputs=[EXT-PROTECTION-1,trust-anchors,OQ-5.4-1]`; it cannot qualify the joint Live cohort.

The host profile has these 14 missing references:

```text
currentTenant
globalAdministrator
humanIdentity
approverEvidence
replay
recorder
worker
spoolBackend
failureModel
protectionEngine
protectionObserver
partiesBinding
conversationsBinding
replicaCount
```

Its four binding gaps remain `partiesBinding`, `conversationsBinding`, `protectionEngine`, `protectionObserver`. The five `services.*` entries are relative checkout paths, not independently proven deployed bindings.

The current shared secrets preparation report is `DependencyNotAvailable`, `packetPrepared=true`, `qualificationEffects=0` and contains these exact 83 unique missing-input identifiers:

```text
CurrentProfileValidity
DistinctEnrolledTenants
IndependentCurrentInventoryAuthority
IndependentDecisionSignerAndActorRoleAuthority
IndependentInstalledQualificationAuthorities
IndependentPersistedStateObserver
IndependentRoleAppChannelAndAuthenticatedCallerScopes
InstalledOwnerQualificationExecutor
IsolatedTargetEnrollmentAuthority
MountedQualificationCallerCredential
MountedQualificationDaprCredential
OQ-5.4-1.AcceptedPinnedHostProfile
OQ-5.4-1.CommittedProtectionOwner
OQ-5.4-1.JointLivePhaseRunner
OQ-5.4-1.SharedPlatformTarget
OwnerAcceptedTargetRuntimeQualificationBindingAndExactCommand
PrivateAuthenticatedCompromiseSubscriberBinding
PrivateExactReplayRegistrarBinding
QualifiedAllCopyLifecycleAdapter
QualifiedNonrollbackRestoreAdapter
qualification.daprApiCredentialReference
qualification.enrollment.appChannelCredentialReference
qualification.enrollment.appId
qualification.enrollment.callerAuthenticatorReference
qualification.enrollment.callerScopeAuthorityReference
qualification.enrollment.identity
qualification.enrollment.installedP256PublicAnchor
qualification.enrollment.publicAnchorFile
qualification.enrollment.version
qualification.enrollmentCredentialReference
qualification.executor.appChannelCredentialReference
qualification.executor.appId
qualification.executor.callerAuthenticatorReference
qualification.executor.callerScopeAuthorityReference
qualification.executor.identity
qualification.executor.installedP256PublicAnchor
qualification.executor.publicAnchorFile
qualification.executor.version
qualification.isolatedTargetId
qualification.observer.appChannelCredentialReference
qualification.observer.appId
qualification.observer.callerAuthenticatorReference
qualification.observer.callerScopeAuthorityReference
qualification.observer.identity
qualification.observer.installedP256PublicAnchor
qualification.observer.publicAnchorFile
qualification.observer.version
runtime.acceptedReplacementCommit
runtime.actorRoleAuthorityReference
runtime.allCopyInventoryReference
runtime.allCopyLifecycleAuthorityReference
runtime.audience
runtime.authenticatedRequesterTransport
runtime.callTimeout
runtime.capabilitySignerCredentialReference
runtime.capabilitySignerReference
runtime.compromiseGuardMirrorReference
runtime.compromisePublisherReference
runtime.compromiseSubscriberCredentialReference
runtime.compromiseSubscriptionReference
runtime.currentInventoryAuthorityReference
runtime.currentRevocationReference
runtime.hmacCredentialReference
runtime.independentSignerAccount
runtime.independentSignerCredentialReference
runtime.issuer
runtime.migrationAuthorityReference
runtime.nonrollbackJournalReference
runtime.profileNotBefore
runtime.profileReference
runtime.profileValidUntil
runtime.profileVersion
runtime.protectionOwnerReference
runtime.providerBinding
runtime.publicAnchorRetentionReference
runtime.publicTrustReference
runtime.replayExactScope
runtime.replayRegistrarCredentialReference
runtime.replayRegistrarReference
runtime.restoreTarget
runtime.tenantA
runtime.tenantB
runtime.tenantInventoryReference
```

The ten `IMPLEMENTATIONS` names remain mandatory missing implementation/installation work: independent role authentication/scopes; executor; persisted-state observer; enrollment authority; all-copy lifecycle adapter; nonrollback restore adapter; current inventory authority; independent decision signer/actor-role authority; exact private replay registrar; authenticated compromise subscriber. Supplying opaque strings cannot establish those implementations. `cohort_prerequisite_inputs()` always appends `OQ-5.4-1.JointLivePhaseRunner`: the standalone secrets client does not execute protection phase 0, interleave host phase 2, or prove all consumers refuse. A local configuration flag cannot supply this runner.

Earlier 2026-10-19/20 retained input ledgers distinguish 53 missing required public fields from five supplied historical references and 25 supplemental component/proof references. The last inspected token observation in those handoffs expired 2026-10-18; later renewal was not inspected or inferred absent. The current default shared config does not carry the five supplied historical strings. No credential file contents were inspected by this investigation.

## Owner delivery scope and required next actions

The approved Dapr-only shared-cut architecture is already selected. Ordinary OpenBao Raft restore was previously observed to revive deleted keys; OpenBao HMAC, a generic secret store, the bounded shared-cut source model, and synthetic/local transport tests do not prove all-copy erasure. F1 local transport progressed beyond the older negative default-limit source measurement, but a semantic production lifecycle component, independently surviving erasure/fencing authority, complete holder/copy closure and physical restore/non-revival evidence remain prerequisites. No authority/provider artifact may be fabricated from the passing source tests.

1. Platform/custody/protection owners must deliver the actual semantic lifecycle/restore provider and independently governed current/anchored authority; exact installed private signing/replay/compromise/export paths; all-copy/holder/native-intent inventory and non-revival/original-lookup proofs. Preserve exact identities and all existing journals.
2. Independent profile/approval, workload-identity and qualification owners must supply current issuer/audience/role/revocation profiles, restricted opaque credential references and scope evidence, genuine executor/observer/enrollment implementations, three distinct independently governed installed public P-256 anchors, and all 234 disjoint target scopes. Approval private keys stay outside Platform/Agents.
3. Host owner must fill the fourteen actual profile references and prove the service/protection/observer/Parties/Conversations bindings. Retained-history work remains assigned only symbolically to `PLAT-ACTOR-HISTORY-1`; the owner handoff does not add a register row or install that backend.
4. Protection must have a complete accepted commitment and permitted owner phase 0. Platform must implement a real joint phase runner and one shared final target with secrets. Pin a complete host profile, final runtime configuration, both plans, real anchors and exact commands. Correct the hardcoded historical date before proposing a new date, then regenerate fresh detached packets.
5. Present complete changed commitments for explicit owner acceptance. The present source pair can be reviewed as incomplete source delivery, but accepting its empty deployment/trust slots cannot establish the required complete integration commitment. No register acceptance or availability change is inferred.
6. Only after exact acceptance and complete prerequisites, install clean exact targets and run one retained OQ-5.4-1 attempt: phase 0 protection; phase 1 secrets without host references; phase 2 host using phase-1 credentials with every Agents consumer seam returning `DependencyNotAvailable`; phase 3 mutual S3/S4/v23 and host H3/H4/v21/v23. Both Platform records become Available only if the entire required evidence passes. No Agents runtime path is enabled. Unknown outcomes permit exact original lookup only; never reset a journal or reissue a destructive identity.

## Supporting sources read

- Agents `_bmad-output/planning-artifacts/external-dependency-register.md`, full EXT-SECRETS-1 and EXT-HOST-1 records and OQ-5.4-1; `_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-18.md`.
- `../platform/_bmad-output/implementation-artifacts/spec-5-4-ext-secrets-1-ext-host-1-shared-delivery.md` (done), `spec-5-4-ext-host-1-full-qualification-verifier.md` (done), and `spec-5-4-ext-host-1-runtime-composition.md`.
- `../platform/docs/implementation/ext-story-5-4-shared-prerequisites-2026-10-22.md`; `ext-secrets-1-completion-2026-10-19/README.md`; `ext-secrets-1-continuation-2026-10-20/README.md`; `ext-secrets-1-f1-2026-10-20/README.md`.
- `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/completion-20261019/HANDOFF.md`, `engineering-prerequisite-refusal.json`; `continuation-20261020/HANDOFF.md`, `final-bindings.json`; `f1-20261020/HANDOFF.md`, `final-bindings.json`; all current shared profile/configuration/binding/blocked-input/command files.
- `../platform/eng/ext-host-1/{prepare_acceptance.py,verify_full.py}` and `../platform/eng/ext-secrets-1/{prepare_acceptance.py,qualify.py,runtime-reference.example.json}`; Git immutable-object/ref/diff observations stated above.
