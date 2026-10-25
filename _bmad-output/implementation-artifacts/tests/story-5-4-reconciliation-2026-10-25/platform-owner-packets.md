# Platform owner packet reconciliation — 2026-10-25

**Disposition: no complete EXT-SECRETS-1 or EXT-HOST-1 replacement is ready for acceptance.** The October 25 Dapr custody foundation is complete within its source/deployment-preparation scope; its owner explicitly records an uninstalled, physically unqualified provider. The existing shared detached pair remains an IncompleteSourceCandidate. No owner command, build/test, packet preparation, deployment, native operation or cohort attempt ran in this reconciliation. Accepted records and original journals were not changed.

## Full commitment-field reconciliation

The machine report retains both entire accepted records, including full artifact, command contract, consumer list and signing/v21/v23 extensions. No field or extension is weakened.

| Field | EXT-SECRETS-1 | EXT-HOST-1 |
| --- | --- | --- |
| Owner | Existing project owner acting as Platform Maintainer; unchanged | Same; unchanged |
| Repository | Hexalith.Platform; unchanged | Hexalith.Platform; unchanged |
| RequiredArtifact | Entire resolution/custody, independent decision authority, trusted-envelope/replay, key delivery and signing/compromise scope retained; foundations cover only a portion | Entire host, spool/private credentials, migration/fences/protection/guard scope retained; component verification is insufficient |
| TargetVersionOrCommit | Accepted abbec4093715c1db854433119b32d4e16df20d08; shared source candidate 3b4151df682448557d58a09027a05c5b41b6ce31 remains unaccepted | Same accepted/candidate targets, required shared target preserved |
| TargetIntegrationDate | Accepted 2026-10-08 missed; no complete new integration date | Same |
| CompatibilityContractAndVerificationCommand | Full S1–S4/v23 contract retained; exact candidate command below cannot qualify without installed authorities and joint runner | Full H1–H4/v21/v23 contract retained; command has only a component pass path |
| RequiredEvidenceLevel | Levels 4 and 5 retained; no complete exact-target Level 4 delivery | Same; Level 5 remains consuming/RQ-1 work |
| AcceptedStatus | Committed, not Available; no change | Committed, not Available; no change |
| ConsumingStories | 5.4, 5.5, 5.6, 5.8, 6.1, 6.3, 6.4, 7.3, 8.1, 8.2, 8.3, 8.4; RQ-1; unchanged | 5.4, 5.6, 6.1, 8.1, 8.3; RQ-1; unchanged |

The extra accepted RuntimeQualificationBinding is retained exactly in the JSON. The current destruction-capability profile and v23 extension remain binding for Secrets; host v21/v23 and open Product gates remain binding for Host. Branch B, host 192.168.1.30, 365-day history and timing 300/30/600/86400/604800 are unchanged.

## Canonical sources and current installation

- Sibling Platform HEAD/recorded origin/main: `7c56ad6352a74edec62450668e106ec8d6011e40`; unrelated modified Story 2.5 command evidence remains. No fetch performed.
- Root reference Platform HEAD: `90f33a59164d0dcfc21fb688f28651afa4d3df4b`; clean. This is not the sibling checkout named by owner commands.
- New source foundation commit: `3775b5668fb9eac408e351893f133663045b87da`, directly resolved from Git and ancestor of recorded origin/main. Later scoped source diff is empty; it is source provenance, not an installed/accepted integration target.
- Reviewed inventory comparison: 292/293 source files match. The drift is `references/Hexalith.Builds/Props/Directory.Packages.props`; expected `c8efba903924130cdcb286066bb3a9e97dd3dcb9155ccc3f852c65329ea2a0bf`, current `c43f62b33c765e28519b6c72058c6317649ec53963d7cbe7f0d0ddabb3898375`. This build dependency difference prevents relabeling prior local source results as current exact-graph evidence.
- All ten October 19–22 detached acceptance packets, plus the historical abbec40 Host packet, were re-read and their current hashes/canonical ancestry are retained below and in JSON. They continue to have null acceptedReplacementCommit and false dependencyAvailable. No newer complete detached replacement was found in the owner evidence roots or shared packet locations.

## Exact candidate commands (not executed)

### EXT-SECRETS-1

```bash
test "$(git -C ../platform rev-parse HEAD)" = "3b4151df682448557d58a09027a05c5b41b6ce31" && test -z "$(git -C ../platform status --porcelain)" && env EXT_SECRETS_1_RUNTIME_CONFIGURATION=/tmp/ext-story-54-shared-3b4151d/secrets/runtime-reference.json EXT_SECRETS_1_ACCEPTANCE_PACKET=/tmp/ext-story-54-shared-3b4151d/secrets/replacement-acceptance-packet.json pwsh -NoProfile -File ../platform/eng/verify-ext-secrets-1.ps1 -Mode Live -EventStoreSourceRoot ../eventstore -ArtifactsDirectory /tmp/ext-secrets-artifacts -EvidenceDirectory /tmp/ext-secrets-evidence
```

### EXT-HOST-1

```bash
test "$(git -C ../platform rev-parse HEAD)" = "3b4151df682448557d58a09027a05c5b41b6ce31" && test -z "$(git -C ../platform status --porcelain)" && bash ../platform/eng/verify-agents-host.sh --mode Full --artifacts-path /tmp/ext-host-1-full --profile /tmp/ext-story-54-shared-3b4151d/host/agents-deployment-profile.json
```

Both commands pin the shared 3b4151d target and require a clean sibling checkout. Neither guard matches current sibling state. Script/interpreter entrypoints are inspectable, but executable syntax does not supply required implementation, installation or qualification. The Host Full implementation can return zero only as componentQualification; it explicitly retains dependencyAvailable=false, productionEngineRegistered=false and liveEffects=0. Secrets cohort_prerequisite_inputs still unconditionally appends OQ-5.4-1.JointLivePhaseRunner. The accepted owner commands remain unchanged and are retained verbatim in the JSON.

## Hash reconciliation

| Binding | Actual hash / result |
| --- | --- |
| hostProfileRawSha256 | `f93680dfb9ae76f963005f0058622672c567612532591555e501dd3d13db7083` |
| hostProfileCanonicalSha256 | `f93680dfb9ae76f963005f0058622672c567612532591555e501dd3d13db7083` |
| hostProfilePacketMatch | `True` |
| hostPlanRecomputedSha256 | `8d9716c0348f40fc9caab75aa3ebc1d8df9718aa7f6a4234c2a91be0de225679` |
| hostPlanPacketMatch | `True` |
| secretsConfigurationRawSha256 | `b1a99a90583489424fcc236541d15ccf7bd0abe8ca8c8e33b9dccc966d01b8c8` |
| secretsConfigurationCanonicalSha256 | `b1a99a90583489424fcc236541d15ccf7bd0abe8ca8c8e33b9dccc966d01b8c8` |
| secretsConfigurationPacketMatch | `True` |
| secretsPlanRecomputedSha256 | `16dd493ff0414fea13be95f137d372e54088881550665aa3239a5814a9e862ed` |
| secretsPlanCaseCount | `234` |
| secretsPlanPacketMatch | `True` |
| nullAnchorMapSha256 | `53f138498b53f560ce6957f054d6feb7d27cdee2190c23bbf4331a3fcb89e160` |
| trustAnchorHashes | `{"executor": null, "observer": null, "enrollment": null}` |
| installedTrustProven | `False` |

Host canonical JSON includes one final newline; Secrets canonical JSON includes none. Raw profile/configuration bytes match their owner canonical forms. Plan hashes were independently recomputed using the inspected pure plan generator/host plan object; no qualification or packet generator was invoked. The null-anchor-map hash authenticates no trust. No actual executor, observer or enrollment anchor exists in these bindings.

The existing public HMAC runtime reference is `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/provider-key-installation-20261017T100804405564Z/runtime-reference.json`; current raw SHA-256 `31c28a9d1dd21eed8ca4d0c421088cb946b262c624ba461d0795cab92cd1feb3`. It has 51 null fields, enumerated in JSON, and does not install the DEK provider or the full qualification authority.

The accepted October 18 Secrets detached configuration was separately rehashed: raw/canonical SHA-256 `ca019f1325d973919a46b046fcb21be39a4b318800cd59e9378904c365228344` matches the register, but its executor/observer/enrollment public-anchor paths remain null. Its exact packet and file inventory are retained in JSON. This consistency does not establish installed qualification.

### Retained packet files

| Path | Candidate | Raw SHA-256 |
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
| `/home/administrator/hexalith-host-evidence/ext-host-1/runtime-composition-abbec40/replacement-acceptance-packet.json` | `abbec4093715c1db854433119b32d4e16df20d08` | `d5427ae5ad6fc9c706a25d3ab557c6b28a06eac0a2b85bcf733bc188b336e354` |

## Complete blockers

### Secrets shared packet — 83 overlapping required/derived/implementation/acceptance inputs

- `CurrentProfileValidity`
- `DistinctEnrolledTenants`
- `IndependentCurrentInventoryAuthority`
- `IndependentDecisionSignerAndActorRoleAuthority`
- `IndependentInstalledQualificationAuthorities`
- `IndependentPersistedStateObserver`
- `IndependentRoleAppChannelAndAuthenticatedCallerScopes`
- `InstalledOwnerQualificationExecutor`
- `IsolatedTargetEnrollmentAuthority`
- `MountedQualificationCallerCredential`
- `MountedQualificationDaprCredential`
- `OQ-5.4-1.AcceptedPinnedHostProfile`
- `OQ-5.4-1.CommittedProtectionOwner`
- `OQ-5.4-1.JointLivePhaseRunner`
- `OQ-5.4-1.SharedPlatformTarget`
- `OwnerAcceptedTargetRuntimeQualificationBindingAndExactCommand`
- `PrivateAuthenticatedCompromiseSubscriberBinding`
- `PrivateExactReplayRegistrarBinding`
- `QualifiedAllCopyLifecycleAdapter`
- `QualifiedNonrollbackRestoreAdapter`
- `qualification.daprApiCredentialReference`
- `qualification.enrollment.appChannelCredentialReference`
- `qualification.enrollment.appId`
- `qualification.enrollment.callerAuthenticatorReference`
- `qualification.enrollment.callerScopeAuthorityReference`
- `qualification.enrollment.identity`
- `qualification.enrollment.installedP256PublicAnchor`
- `qualification.enrollment.publicAnchorFile`
- `qualification.enrollment.version`
- `qualification.enrollmentCredentialReference`
- `qualification.executor.appChannelCredentialReference`
- `qualification.executor.appId`
- `qualification.executor.callerAuthenticatorReference`
- `qualification.executor.callerScopeAuthorityReference`
- `qualification.executor.identity`
- `qualification.executor.installedP256PublicAnchor`
- `qualification.executor.publicAnchorFile`
- `qualification.executor.version`
- `qualification.isolatedTargetId`
- `qualification.observer.appChannelCredentialReference`
- `qualification.observer.appId`
- `qualification.observer.callerAuthenticatorReference`
- `qualification.observer.callerScopeAuthorityReference`
- `qualification.observer.identity`
- `qualification.observer.installedP256PublicAnchor`
- `qualification.observer.publicAnchorFile`
- `qualification.observer.version`
- `runtime.acceptedReplacementCommit`
- `runtime.actorRoleAuthorityReference`
- `runtime.allCopyInventoryReference`
- `runtime.allCopyLifecycleAuthorityReference`
- `runtime.audience`
- `runtime.authenticatedRequesterTransport`
- `runtime.callTimeout`
- `runtime.capabilitySignerCredentialReference`
- `runtime.capabilitySignerReference`
- `runtime.compromiseGuardMirrorReference`
- `runtime.compromisePublisherReference`
- `runtime.compromiseSubscriberCredentialReference`
- `runtime.compromiseSubscriptionReference`
- `runtime.currentInventoryAuthorityReference`
- `runtime.currentRevocationReference`
- `runtime.hmacCredentialReference`
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
- `runtime.providerBinding`
- `runtime.publicAnchorRetentionReference`
- `runtime.publicTrustReference`
- `runtime.replayExactScope`
- `runtime.replayRegistrarCredentialReference`
- `runtime.replayRegistrarReference`
- `runtime.restoreTarget`
- `runtime.tenantA`
- `runtime.tenantB`
- `runtime.tenantInventoryReference`

### Host — fourteen empty profile references

- `currentTenant`
- `globalAdministrator`
- `humanIdentity`
- `approverEvidence`
- `replay`
- `recorder`
- `worker`
- `spoolBackend`
- `failureModel`
- `protectionEngine`
- `protectionObserver`
- `partiesBinding`
- `conversationsBinding`
- `replicaCount`

The five services entries are sibling source paths, not installed service bindings. The shared pair still has no installed production/profile composition.

### October 25 custody foundation — 68 unique top-level inputs

Each item retains the accountable owner and next action; expanded member-level inputs are preserved verbatim in the structured report.

| Input | Owner | Next action |
| --- | --- | --- |
| `images.component` | Platform release owner | Build and pin the reviewed artifact by immutable image digest (not installed or supplied) |
| `images.custodyApplication` | Platform release owner | Build and pin the reviewed artifact by immutable image digest (not installed or supplied) |
| `images.dapr` | Platform release owner | Build and pin the reviewed artifact by immutable image digest (not installed or supplied) |
| `images.dotnetSdk` | Platform release owner | Build and pin the reviewed artifact by immutable image digest (not installed or supplied) |
| `images.dotnetAspnet` | Platform release owner | Build and pin the reviewed artifact by immutable image digest (not installed or supplied) |
| `artifacts.custodyApplicationComposition` | Platform host composition owner | Integrate the public adapters into the owning authenticated custody application; no production DI is activated here (not installed or supplied) |
| `artifacts.componentProfile` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.enrollment` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.componentVerifier` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.transportProfile` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.holderClosureProfile` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.capacityMeasurement` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.independentPlacement` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.authorityEnrollment` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.originalArchiveEnrollment` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.privateAuthorizerImplementation` | Independent private-authority service maintainer | Implement authenticated exact-intent/current-permission authorization; install separately after source review (not installed or supplied) |
| `artifacts.controlIssuerImplementation` | Governance/guard and protection-owner maintainers | Implement durable hold/dispatch/reservation handoff and original lookup; install separately after source review (not installed or supplied) |
| `artifacts.independentObserverImplementation` | Independent infrastructure/security observer | Implement native evidence, copy/buffer/termination and survivor-recovery verification; install separately after source review (not installed or supplied) |
| `artifacts.eventStoreIntegration` | EventStore Maintainer | Complete owning-source adapter/owner-state integration and return an exact published source/package target (not installed or supplied) |
| `artifacts.physicalQualification` | Platform infrastructure owner | Supply retained exact artifact bytes, owner implementation and accepted profile pin (not installed or supplied) |
| `artifacts.g1Evidence` | Protection owner and independent erasure observer | Return retained accepted physical non-revival/shared-cut proof; the OpenBao negative control remains unqualified (not installed or supplied) |
| `artifacts.registrationOriginalLocatorImplementation` | Platform custody application maintainer | Implement and install ICustodyKeyRegistrationSource under its exact scoped original lookup binding (not installed or supplied) |
| `artifacts.reservedManifestOriginalLocatorImplementation` | EventStore Maintainer with Platform custody host integration owner | Implement and install ICustodyReservedManifestSource with restricted authenticated protection-original lookup (not installed or supplied) |
| `artifacts.registrationOriginalMapping` | Platform custody application maintainer | Retain full key identity to exact original registration/actor revision/KEK/opaque root/wrap receipt mappings, verified independently before authority use (not installed or supplied) |
| `artifacts.reservedManifestOriginalMapping` | EventStore Maintainer | Retain exact ConsumptionReserved receipt to complete immutable signed request, issued/dispatch guard facts and sorted manifest vector (not installed or supplied) |
| `credentialReferences.component` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `credentialReferences.worker` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `credentialReferences.privateAuthorizer` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `credentialReferences.controlIssuer` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `credentialReferences.independentObserver` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `credentialReferences.recoveryAdministrator` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `credentialReferences.recoveryDeputy` | Security installation owner | Install the least-privilege opaque credential reference; never put credential values in this ledger (not installed or supplied) |
| `placement.applicationRecoveryClass` | Recovery administrator and deputy | Enroll independent failure/recovery placement outside application backups (not installed or supplied) |
| `placement.authorityRecoveryClass` | Recovery administrator and deputy | Enroll independent failure/recovery placement outside application backups (not installed or supplied) |
| `placement.originalRecoveryClass` | Recovery administrator and deputy | Enroll independent failure/recovery placement outside application backups (not installed or supplied) |
| `placement.authorityFailureDomain` | Recovery administrator and deputy | Enroll independent failure/recovery placement outside application backups (not installed or supplied; authority must survive the entire application restore/failure cut) |
| `placement.applicationFailureDomain` | Recovery administrator and deputy | Enroll independent failure/recovery placement outside application backups (not installed or supplied) |
| `logicalRoles.interaction-custody` | Platform composition owner | Bind the logical role to its authenticated installed Dapr service/component (not installed or supplied) |
| `logicalRoles.custody-authorizer` | Platform composition owner | Bind the logical role to its authenticated installed Dapr service/component (not installed or supplied) |
| `kubernetes.appPort` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.appProtocol` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.controlPlaneAddress` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.controlPlaneNamespace` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.controlPlaneTrustDomain` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.sentryAddress` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.sentryTokenAudience` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.sentryTokenExpirationSeconds` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.trustAnchorSecret` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.trustAnchorKey` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.apiTokenSecret` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.apiTokenKey` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.appApiTokenSecret` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.appApiTokenKey` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.workerGroupId` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.workerOutputOwnershipEvidence` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `kubernetes.internalGrpcPort` | Platform Kubernetes identity/host owner | Install exact Dapr workload token/trust/control-plane/API-channel and receiving-host inputs; retain owner/volume proof (not installed or supplied) |
| `referenceCredentials.componentSecret` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `referenceCredentials.componentKey` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `referenceCredentials.componentProfile` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `referenceCredentials.workerSecret` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `referenceCredentials.workerKey` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `referenceCredentials.workerProfile` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `referenceCredentials.mountedFile` | Infrastructure reference-backend credential owner | Install the exact secret/key and shared-credential profile at the declared protected file; no metadata/default-chain fallback (not installed or supplied) |
| `profiles.kubernetesNativeSidecars` | Independent transport/closure observer | Measure and attest the complete installed transport, buffer and host profile (not installed or supplied; exact supported installed profile required) |
| `profiles.swapDisabled` | Independent transport/closure observer | Measure and attest the complete installed transport, buffer and host profile (not installed or supplied; exact supported installed profile required) |
| `profiles.coreDumpsDisabled` | Independent transport/closure observer | Measure and attest the complete installed transport, buffer and host profile (not installed or supplied; exact supported installed profile required) |
| `profiles.memoryCaptureDisabled` | Independent transport/closure observer | Measure and attest the complete installed transport, buffer and host profile (not installed or supplied; exact supported installed profile required) |
| `g1Qualified` | Platform and protection owners | Return independently accepted installation/proof (physical G1 proof absent; ObservedRevival remains unqualified) |

The expanded reference-profile list contains 133 actual members; every supplied value remains null. It includes complete source/successor/tainted epoch and KEK mapping, immutable account/store/archive identities, enrollment/trust/current revocation, separate authority/original recovery placement, every copy/holder/producer and empty copy class, actual transport/termination/sanitation, numerical retention and measured capacity. The JSON retains every member and owner action; this count is not interchangeable with the top-level installation ledger.

### Owning-source dispositions

- **independent-private-authorizer — MissingOwningServiceImplementationAndInstallation.** Owner: Independent private-authority service maintainer. Implement authenticated prepare/authorize and owner-call permission issuance from fresh independent current state, exact body hashes, original controls and retained revocation. DaprCustodyAuthorizerClient is transport only; caller hashes grant no permission.
- **durable-control-issuer — MissingOwningServiceImplementationAndInstallation.** Owner: Governance/guard and protection-owner maintainers. Implement durable RegisteredDispatchBlocked hold reservations, release ordering, guard dispatch versus protection ConsumptionReserved original handoff and exact loss lookup. No snapshot/read-then-effect issuer is provided.
- **independent-closure-observer — MissingOwningServiceImplementationAndInstallation.** Owner: Independent infrastructure/security observer. Implement actual native final-evidence verification, complete inventory, I/O/sanitation/termination, restore non-revival and independent survivor recovery. Signed disposable test statements are not implementation or trust.
- **eventstore-owning-source-integration — MissingAcceptedOwningSourcePackageAndProductionIntegration.** Owner: EventStore Maintainer. Review/commit/publish the owner-state port and actual transition bridge, integrate exact original state/genesis/custody adapters into owning source, and return accepted source/package/profile pins. The local build explicitly used current sibling source, including work not in its installed package; no sibling was edited or activated.
- **custody-application-composition — MissingOwningHostCompositionAndInstallation.** Owner: Platform host composition owner. Compose the public clients behind the authenticated custody application API with its local daprd/component, public enrollment/trust, mounted scoped credentials and actual holder/copy integration. No receiving API host or production DI is activated by these libraries/templates.
- **history-custody — OutsideThisInteractionFoundation.** Owner: Parties/EventStore history owners and Platform history custodian. Preserve the existing accepted exclusive expiry/history policy and open source/owner/qualification obligations. This interaction epoch component does not implement exact history expiry or resolve Q1-Q7.
- **export-lifecycle — OpenPolicyAndUnqualifiedClosureUnchanged.** Owner: Product/Governance/Security and export store owner. Retain Open export disposition and separately accepted all-copy obligations; an export-affected B5 remains blocked. No policy or date was changed.
- **complete-key-registration-original-locator — MissingOwningSourceImplementationAndInstallation.** Owner: Platform custody application maintainer. Implement ICustodyKeyRegistrationSource.FindAsync for the complete tenant/object/purpose/alias/key/lifecycle/store/contract identity. Retain the exact original CustodyKeyRegistration (wrap operation, actor ExpectedRevision, original KEK version, opaque wrapped-key reference and wrap receipt). The locator is a durable hint only: every use must still authenticate the independent custody original. Install its scoped store/Dapr lookup binding and preserve mappings across restore without root reallocation.
- **reserved-complete-manifest-original-locator — MissingOwningSourceImplementationAndInstallation.** Owner: EventStore Maintainer with Platform custody host integration owner. Implement ICustodyReservedManifestSource.FindAsync from the exact protection ConsumptionReserved receipt to its retained immutable complete DeletionBatchConsumptionRequest: capability/JWS/canonical identity, issued guard revision, dispatch receipt/revision and full sorted target vector. Exact duplicate/loss/restore uses that original; no singleton derivation, batch-only alias, newly signed request or reconstructed manifest. Install authenticated restricted original lookup and wire it into the custody lifecycle provider.

The current owner delivery explicitly distinguishes 23 component scenarios, 128 Runtime tests, 5 Protection tests and 10 offline tests from installed evidence. Release publish emitted 41 local files; reviewed runtime image pin remains absent, so the final image-context attempt is Blocked and no image was built. Earlier image-context success is superseded, retained source evidence. No test result upgrades physical G1.

## Separate isolated continuation and dates

The other two custody worktrees are dirty and were read without modification. The integration continuation HEAD is `a5b0468ba002d645195ef019c03f94c30263d7f8`; source work exceeds that commit. Its DELIVERY.md requires the detached receipt `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/continuation-20261023-hmr4aw3v/final-delivery.json`, which is absent. Its source reproduction command therefore has no resolved final exact source target, and is not an owner Live command.

That continuation records eight missing delivery groups: PHYS-1 (eligible existing primitive), AUTH-1 (independent placement/enrollment), TRUST-1 (actual issuer/public trust), CONTROL-1 (signed guard/protection/owner control), COPY-1 (complete physical inventory/original proof), DAPR-1 (installed transport/closure), PORTS-1 (qualified production integration), OQ-1 (shared owners/profile/runner). Full requiredBindings and owner actions are in JSON. Its installed component/configuration/physical-plan/enrollment/trust/executor/observer/enrollment hashes are all null.

Its unaccepted milestones are source integration review 2026-10-26, existing-asset/placement decision 2026-10-27 and a conditional exact physical approval packet 2026-10-29. Native installation/physical qualification is explicitly NotSchedulable with a null date. These are not new complete dependency integration commitments. The isolated owner amendments exclude AWS/new hosts/HSM acquisition from that MVP and retain EUR 0 additional spend; the main foundation separately labels its AWS backend an uninstalled reference, not mandatory. Neither source branch authorizes infrastructure spending or a physical experiment here.

Both current verifiers still literally enforce 2026-10-08; Host profile/plan hashes bind it. Any credible new date requires corrected owner verifier/packet/profile/plan bindings and explicit acceptance of the entire coordinated replacement. Do not copy a proposed milestone into TargetIntegrationDate.

## Qualification disposition

OQ-5.4-1 is not eligible: protection remains uncommitted, complete installed authorities/inputs/trust are absent, the shared pinned profile is incomplete, and a real joint phase runner is missing. Preserve phase order protection → secrets without host references → host with Agents seams refusing → mutual cases. One accepted target set receives one original attempt only; retain every prior attempt and recovery journal. A later fixed output path must not overwrite an original. Both Platform records must remain non-Available on any failure.

The OpenBao negative result remains ObservedRevival, g1Qualified=false, SHA-256 6efa381bc70b3de57a24142e5e679f53781c7f89d5ab6740fb7664966f30888e. Source wrappers, test doubles, scheduled deletion or restored flags do not supply irreversible erasure. There are no complete changed Platform commitments to submit for acceptance.

## Evidence inventory

All inspected paths, raw SHA-256/byte sizes, accepted full records, canonical Git outputs, exact commands, complete prerequisite ledgers, dates and raw/canonical comparisons are retained in [platform-owner-packets.json](platform-owner-packets.json). This report establishes static reconciliation only.
