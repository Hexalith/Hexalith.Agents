---
title: 'Story 5.4 installed-image and native-loader construction proposal'
status: proposed-not-accepted
created: 2026-11-02
affects: [EXT-HOST-1, EXT-SECRETS-1, OQ-5.4-1]
---

# Proposed construction for owner validation

Recommend **SelfContainedDotnetVerityImageV1**, with measurement contract **Platform.InstalledDotnetWholeImageMeasurement.v1**. This is a concrete technical proposal. No architecture decision, installation, accepted target, profile, date, command or availability is established by this document.

Current Platform source accepts only `SelfContainedStaticElfV1` and independently accepted static-ELF kernel closure. The same requirement applies to every selected host resource. The actual services are ordinary .NET hosts, and no supported whole-host static/AOT construction has been implemented or qualified. Separately, `eng/ext-secrets-1/execution_profile.py` unconditionally returns `External.IndependentlyVerifiedInstalledImageAndNativeLoaderMeasurementProtocol`. The carried R8E-3 restriction intentionally prevents an owner Boolean or file census from clearing that gap.

The proposed construction packages ordinary self-contained, non-single-file .NET hosts through the existing SDK image-publish path. Each workload executes from a complete read-only dm-verity root filesystem on an independently administered Linux execution host with an accepted measured/verified-boot construction. App code, CLR/JIT, native libraries, OS loader, system configuration and launch supervisor belong to one measured immutable image. The kernel documentation describes dm-verity's read-only block-integrity guarantees and the need to authenticate its root hash. [Linux dm-verity documentation](https://www.kernel.org/doc/html/latest/admin-guide/device-mapper/verity.html).

All permitted loader inputs must be inside that image. Pin public configuration bytes; keep private credentials in separate purpose-bound mounts outside public artifact inventories. Writable output/store mounts must not supply executable or managed assemblies. Close environment and search-path overrides, including startup hooks, additional dependency stores, runtime roll-forward and host-dependent runtime discovery. The .NET environment controls make these part of the loader construction rather than ordinary application settings. [Microsoft .NET environment-variable documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables).

## Proposed proof fields

| Group | Exact information to bind |
| --- | --- |
| Contract and scope | Schema/version, accepted policy hash, Platform and owner targets, cohort/profile/configuration/plan hashes, resource, phase and command identity |
| Image closure | OCI manifest/config digests, complete image-file inventory digest, dm-verity root hash/parameters, apphost and entry assembly, dependency/runtime-configuration files, bundled CLR/native-runtime identities |
| Execution substrate | Actual host identity and boot incarnation, accepted boot/kernel/initramfs/module measurements, architecture/RID, authenticated verity-device/mount identities, supervisor artifact/version |
| Launch policy | Exact argv/cwd/environment digest, UID/GID, capabilities, namespaces, mount table, writable-volume classifications, seccomp/LSM policy hashes, accepted CLR/JIT transformation and assembly-loading rules |
| Independent witness | Installed witness/role/key version and accepted anchor, fresh challenge/request digest, observed substrate/image/policy facts and signed evidence references |
| Currentness | Independently surviving origin/genesis, deployment generation, host/boot epoch, authority and trust/revocation revisions, observation instant and exclusive validity |
| Launch original | Deterministic launch identity, prepare/reservation original, exact intent digest, dispatch/result or authenticated unknown, boot-bound process identity/pidfd, cgroup/namespace identities |
| Readiness | Fresh independent confirmation that the same measured process/image/mounts remain current, plus current role/trust/revocation and restore-fence evidence |

An accepted profile pins the policy, expected images and authority anchors. Fresh witness responses bind that profile hash, a challenge and the launch-intent digest. Retain those responses in the existing attempt/recovery journal. Do not insert them into frozen profile bytes or use renewed responses to reset the attempt fingerprint.

Initial operational values proposed for validation are a 30-second acquisition budget, measurement/currentness age at launch at most one second, and trusted-time uncertainty at most 100 ms. These need measured support and explicit acceptance. Existing trusted-envelope timing and destruction-capability semantics remain unchanged.

## Ordering and refusal

1. Acquire accepted inputs, exact immutable image/mount handles and independent measurements before registering or dispatching the workload.
2. The trusted launch owner conditionally reserves one exact launch at current deployment/boot/trust/revocation revisions. Cancellation versus reservation serializes at that same owner; a remote check followed by an unrelated spawn is insufficient.
3. Reservation is the irreversible boundary for that cancellation reason. Dispatch creates at most one process. Lost results recover the same original/process identity and cannot authorize a second launch.
4. Every readiness evaluation and new phase requires fresh current evidence. Existing private HMAC, replay, spool, FR-34 and domain checks continue independently; a launch attestation grants no later business or destruction authority.
5. Missing, stale, unavailable, foreign-purpose, changed-image or unverifiable evidence refuses before launch. Ambiguous dispatch remains unknown until exact lookup resolves it.

Restore qualification must cover the prescribed application, EventStore, OpenBao, execution-host and authority-state cuts. The measurement/currentness origin must survive them or consult a separately surviving monotonic witness. Restored processes and VM-memory snapshots must be fenced from network/effect access pending fresh incarnation admission, or that restore construction is rejected. Test rollback, witness loss, revocation races, lost acknowledgements and continued denial. Image measurement proves no all-copy erasure or backup non-revival property; those protection obligations retain their complete independent evidence requirements.

## Ownership and implementation

- The publisher produces the exact SDK image and provenance.
- The enrollment owner registers actual execution hosts, verity resources, role identities, public anchors and revocation.
- The independent measurement/currentness authority observes real substrate/image facts and owns nonrollback originals.
- The launch supervisor performs exact reserved launches and cannot mint measurements or business approvals.
- The business approver issues policy/acceptance decisions. The nominated `jpiquot` identity does not supply measurement or protection evidence.

Eventual implementation belongs in strict Host/Secrets profile and packet validation, `execution_profile.py`, `launch_inventory.py`, a separate measured .NET launch lease/component and an explicitly selected branch of `AgentsHostComposition`. Preserve the existing static-ELF implementation. The new branch needs negative, race, lost-result and restore tests before installed qualification.

A source-only Platform owning-process host can be implemented first: expose a narrow Agents module-registration facade, reuse SDK hosting and compose the existing HMAC/Secrets/owning-process services in the real Platform entrypoint. Tests must prove actual entrypoint registration and refusal with controlled authorities. That source host supplies no production launch pass by itself.

## Validation boundary

The owner decision to validate is whether this complete managed .NET construction is the chosen path for implementing the missing measurement contract. It is not a request to manufacture proof or accept an incomplete dependency record. Actual independently governed witness/substrate resources, permissions, trust and measured completion bounds must still be delivered. No supported resource/cost allocation or installed witness is inferred from the proposal; any provisioning remains within its separately applicable scope and authorization.

Architecture selection alone must not clear R8E-3. Keep the unconditional missing-contract refusal and current static-ELF boundary enforced until the replacement construction is implemented, independently evidenced and explicitly accepted with complete coordinated target/date/command/profile/plan/anchor bindings. The original six Conversations seams, full Host/Secrets/protection contracts and one-attempt OQ order remain unchanged.
