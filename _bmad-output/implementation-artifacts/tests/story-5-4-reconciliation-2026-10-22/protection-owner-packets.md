# Story 5.4 protection owner packet reconciliation — 2026-10-22

EXT-PROTECTION-1 remains **Uncommitted**. Its current owner packet says **“incomplete delivery; do not accept as Committed.”** Canonical published source and completed adapter tasks do not supply a complete owner delivery target, installed profile, independent trust, physical provider or passing qualification evidence. No complete replacement commitment can currently be presented for acceptance without inventing these values.

This investigation read the repository instructions, the complete EXT-PROTECTION-1 artifact/verification contract and v21/v23 extensions, OQ-5.4-1, the 2026-10-18 reconciliation and the packets below. It performed only local file/hash inspection and read-only Git inspection. No qualification, Live/Full/cohort run, deployment, provisioning, destructive probe, credential-value read, source/configuration edit or Git mutation was performed. This report is its only write.

## Packet and authority inventory

Paths beginning `../` are relative to the Agents repository root. SHA-256 values bind the exact observed bytes, including existing line endings.

| Packet or authority | SHA-256 | Interpretation |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/external-dependency-register.md` | `5bf35ac2857664c0589966a10944cd66278af85242c7c1f08e31ae29aa40e8a2` | Authoritative record, v21/v23 extensions and bounded cohort. |
| `_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-18.md` | `7ea22dad7792770f8377cef64ab6f3c7b610ad06e776b71dd3b9aa12739ca5ae` | Accepted historical replacements/cohort and unresolved dependencies. |
| `../eventstore/docs/implementation/ext-protection-1-owner-acceptance.md` | `658a8bf28027ef322fe73c0d21862fba2ae2a4a38e9fc563c006bf5772054421` | Current sibling packet, including 2026-10-21 commitment proposal and 2026-10-22 owner-bindings checkpoint; explicitly incomplete. |
| `references/Hexalith.EventStore/docs/implementation/ext-protection-1-owner-acceptance.md` | `3ab9651ee326378f2c33df734f748717d11655f7c0bb1ad493a7d5e2ac4b824d` | Older reference-checkout copy, also incomplete. |
| `../eventstore/_bmad-output/implementation-artifacts/spec-5-4-ext-protection-1-owner-delivery.md` | `6ca41bc09b8de5d613db59ce5aa35e6630b3bbfe90dbcd2a431c2c03ae1bc259` | Completed bounded adapter/source work; frozen scope forbids qualification and invented installed references. |
| `../eventstore/_bmad-output/implementation-artifacts/spec-ext-protection-1-oq-5-4-1.md` | `150d2f7aac3e2ee12d0e5e4ca22f03d1fe43c156554a07bb20f0ff3897478f11` | Completed source/component authority and verifier corrections; does not supply an accepted full delivery. |
| `../eventstore/docs/implementation/ext-protection-1-verifier-protocol.md` | `2614ab0a79d6b7064f68902aa21202a13f8f2dccb136d604b6faaa649a76e559` | Exact profile, independent-role, native-original and recovery requirements. |
| `../platform/docs/implementation/ext-secrets-1-dek-provider-decision-2026-10-18.md` | `0536906b5d68f76b2e194fbe646c56e6132dd9a1f6bfd3fbbeb525abb2301b50` | Historical provider proposals, erasure-first investigation and retained negative result; later architecture adoption below controls current direction. |
| `../platform/docs/implementation/ext-secrets-1-option-a-feasibility-2026-10-18/README.md` | `f90632721ccd0d1e4e2fd988baa016cb55a1b0fdccc9dd806803aa55f6d22a1c` | Repository-retained native observation and exact byte bindings. |
| `../platform/docs/implementation/ext-secrets-1-erasure-authority-decision-2026-10-20.md` | `c87423bf1af0757a6fb4be60b634d5a7d527458a8576573b92bfbdf6e51ca044` | Adopted Dapr-only semantic custody architecture; physical feasibility, installation and qualification remain incomplete. |
| `../platform/docs/implementation/ext-secrets-1-erasure-authority-2026-10-20/OWNER-APPROVAL.md` | `e7a5387aff07074090f529c341484c5ffdf6f1989cf001b04d0e1920e484e692` | Records approval and the subsequent controlling Dapr-only constraint. |
| `../platform/docs/implementation/actor-history-lifecycle-2026-10-07.md` | `78cf9ffe55eeea1eef02d861af2f6cac282f3c513207aa9064fe8dd4ad6bdd74` | Accepted 365-day history policy and bounded library cleanup; actual backend/independent lifecycle qualification remain absent. |

## Canonical source observations and proposed commitment

At inspection, sibling EventStore `main` was `995117f9b899a2994e99257091b0ea35cfbe5090`, subject `feat(protection): bind fail-closed EXT-PROTECTION-1 owner ports`. `git merge-base --is-ancestor HEAD origin/main` exited 0. The checkout contains unrelated modified/untracked files, which were preserved. The reference checkout was clean on `main` at `5ce5f1d84b88c808cc8a526b9decf149de977e55`, also reachable from its local `origin/main`. No fetch or checkout was performed by this investigation.

The sibling packet's prior observations `d01c6756c769dcbd8d4d5fff472d07339ebeb22a` and `6b2988d0ecae276440cb2bd29a93af4c12d7c07c` are historical baselines. The latest source commit contains additional bindings but still supplies no accepted complete `TargetVersionOrCommit`. None of these observations may be substituted for that missing full delivery target.

The accountable owner remains EventStore Maintainer; repository `Hexalith.EventStore`, `https://github.com/Hexalith/Hexalith.EventStore.git`. The owner proposes `TargetIntegrationDate` **2026-11-02** only. That date is not accepted. Required evidence remains Levels 4 and 5: owner component qualification supplies Level 4, and consuming gates/`RQ-1` later supply Level 5.

The unchanged 41-case plan at `../eventstore/tools/ext-protection-1/plan.json` hashes to `c1e7759a2687c122dd182aa48f1828e531166c0157a04b2ae0eba35e9ac4d10c`.

## Exact candidate command and missing bindings

The implemented candidate command, run from Agents, is:

```bash
python3 ../eventstore/tools/verify-ext-protection-1.py --mode Live --profile /home/administrator/hexalith-protection-evidence/ext-protection-1/accepted-profile.json --evidence-root /home/administrator/hexalith-protection-evidence/ext-protection-1/attempts
```

It was **not run**. It has an input-dependent success branch rather than an unconditional refusal. However, it is not currently an acceptance-safe Committed command: the record is Uncommitted, no complete target exists and `/home/administrator/hexalith-protection-evidence/ext-protection-1/accepted-profile.json` does not exist. No installed profile/configuration/trust/engine-binary hashes can truthfully be supplied.

Live requires the accepted register row, an ISO integration date, exact 40-character target equal to clean EventStore HEAD and reachable from local `origin/main`, plus `ProtectionProfileSha256=<actual lowercase SHA-256>` in the existing compatibility field. It does not fetch. The profile schema is `ext-protection-1-profile/v2`, with exactly `schema`, `targetCommit`, `planSha256`, `tenantReferences`, `roles`, `engineBinary` and `capabilityTrust`.

Missing operational inputs include:

- Independent `executor`, `observer` and `enrollment` programs; their complete hash-bound installation manifests, distinct public P-256 anchors, identities and `env:` credential references.
- Accepted `capabilityTrust` issuer/audience/tenant/key-family/key-version records, public anchors and trust-profile revisions; accepted installed engine binary path/hash.
- Actual key delivery, protection binding source, current guard/occurrence/transaction installations, independent anchor store and compromise registrar.
- `runtime.allCopyLifecycleAuthorityReference`, `runtime.allCopyInventoryReference`, `runtime.nonrollbackJournalReference`, `runtime.restoreTarget`, `runtime.protectionOwnerReference`, `runtime.currentRevocationReference`, `runtime.publicTrustReference` and `runtime.publicAnchorRetentionReference`.

The old packet's “no implementation class” statements are stale for current source. EventStore now contains `InteractionOccurrenceKeyProvider`, `InteractionProtectionBindingProvider`, guard/occurrence/transaction authorities and an anchor-journal adapter. `AddInteractionProtectionBindings` registers EventStore adapters with null underlying delivery/source/installation/store; it installs no physical custody and denies the first effect. Platform now has internal unregistered `CustodyKeyLifecycleBindingProvider` and `AtomicDeletionManifestBindingProvider`; neither supplies a qualified provider without an independent witness. Server does not acquire a Platform project reference.

## Adopted architecture and remaining delivery gaps

The 2026-10-20 owner decision **already adopts the architecture**. It requires provider-neutral application contracts and existing Dapr SDKs, accessed through the semantic output-binding contract `custody-lifecycle-v1`; the proposed component is `bindings.hexalithcustody`, API `v1`. Direct AWS/native application adapters, mandatory AWS deployment and the proposed Platform AD-9 exception were superseded by the owner's Dapr-only constraint. No architecture redecision is requested here.

OA-1 accepts a recoverable shared physical all-or-none cut followed by publication of exact original outcomes; it does not assert a native transaction across independent key/storage services. Actual target erasure, survivor recovery, holds, copy closure, independent current authority and original-receipt proofs remain mandatory. The qualifying component/provider artifact, installation, independent failure domain, operational bounds and physical feasibility are absent. Architecture approval did not authorize infrastructure deployment or destructive qualification.

| Gap | Remaining concrete delivery/proof |
| --- | --- |
| G1 — Custody/physical provider | Deliver and install the qualifying Dapr semantic custody component/provider, actual key delivery and six-operation/batch composition, complete key hierarchy, independently surviving authority, hold pins, all-copy erasure and immutable original receipts. Existing HMAC custody is insufficient. |
| G2 — Authority/trust | Install accepted public trust, current guard/occurrence/transaction authorities, private authenticated compromise registrar and independent nonrollback transition journal. New adapters remain closed without these actual sources. No fixture anchor or always-true callback qualifies. |
| G3 — Physical durability/restore | Prove provider partial-failure behavior, all-or-none target erasure, every current/historical copy and backup/restore path, receipt/revocation retention and old-configuration/root non-reissue against the actual provider. |
| G4 — Recovery/replay | Supply mixed-root directory snapshot policy/bindings and installed writer/anchor/scheduler qualification; prove source outbox and target replay share the real original root, typed Erased and independently proven NeverWritten where required. Source-only recovery tests are insufficient. |
| G5 — Installed owner fixture | Deliver actual independent executor/observer/enrollment installations, low-level fixtures and authenticated persisted-state observations. No mock, Agents runtime or receiver substitutes. |
| G6 — Verifier completeness | Complete signing-attempt native chains and installed current-online-authority/append/receipt chains. Existing JWS/native-original correlation checks prove offline consistency, not physical destruction, linearizability or nonrollback. |
| G7 — Complete canonical target | Commit the complete implementation and pass-capable verifier together at an immutable target reachable from `origin/main`, then bind actual installed profile/configuration/plan/anchors and accepted integration date. Partial publication does not close G1–G6. |

## Retained negative control and installed HMAC references

The actual native observation is retained at:

`/home/administrator/hexalith-protection-evidence/ext-protection-1/option-a-feasibility-20261018T113800Z-1dbe62f55af0434684a9602a3824bfe9/raft-probe-attempt-6.result.json`

Its byte-identical repository copy is `../platform/docs/implementation/ext-secrets-1-option-a-feasibility-2026-10-18/native-result.json`. Both hash to **`6efa381bc70b3de57a24142e5e679f53781c7f89d5ab6740fb7664966f30888e`**. The result records `ObservedRevival` and `g1Qualified:false`: ordinary OpenBao 2.6.2 non-force Raft snapshot restoration revived deleted native key material and restored decryption to the original plaintext. This is a retained negative control, not a new run or a production component proof.

The same detached evidence directory retains `validation-manifest.json` (`4fb5a27b30af2ece18c8bcbf880d39bb34b32f3afd9c3e1ba5a7fc15b5d426ee`) and `source-files.json` (`e842d81301759722421553e82ffd249e5913ff2234abe212f122e1d151e826dc`). Native command bytes are retained as `raft-probe-attempt-6.command.json`, also bundled as `native-command.json`, with the owner-recorded SHA-256 `b1a0cde4ce8a4b9fd17938519261df15b245b3056466d111c97f9897877ae01f`.

Installed public HMAC metadata was hash-checked without printing credential contents:

| Reference | SHA-256 / limitation |
| --- | --- |
| `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/provider-key-installation-20261017T100804405564Z/runtime-reference.json` | `31c28a9d1dd21eed8ca4d0c421088cb946b262c624ba461d0795cab92cd1feb3`; HMAC runtime reference, no DEK/manifest provider. |
| Same directory, `provider-key-mappings.json` | `f4e650995f0b35882377e01fb4f30b3944b018aec136e16496b6d349dd595ffe`; `ordinarySnapshotSemantics:true`, `lifecycleOrAllCopyQualified:false`. |

These references must not be copied into protection tenant/trust/role fields as substitutes. A restored actor snapshot, restored deny flag or successful Dapr call is not current independent authority or cryptographic-erasure evidence.

## Actor-history dependency and cohort boundary

The accepted `party-actor-retention-v1` policy is 365 fixed days from binding-effective-at. Platform supplies bounded library cleanup for one owner-inventoried unit; it registers neither `IIdentityHistoryCustody` nor a production backend. Actual all-copy lifecycle durability, authenticated receipts, restored-backup quarantine, outage/lost-ack recovery and successor continuity after predecessor expiry remain unqualified. `PLAT-ACTOR-HISTORY-1` remains an unregistered dependency in the Agents register. This policy approval is not custody availability.

OQ-5.4-1 remains blocked before phase 0. It requires EXT-PROTECTION-1 Committed, a host verifier with a pass path and pinned profile, shared exact Platform target for secrets/host and all installed inputs. Preserve fixed phase order 0 protection → 1 secrets-only → 2 host with every Agents consumer refusing → 3 mutual cases. Both Platform records become Available only from the entire passing run. Preserve exact accepted target/profile/configuration/plan/anchor hashes, one attempt per accepted target set and every original recovery journal. A changed set needs acceptance. Recovery-only `RECOVERED` / `EXACT_ORIGINAL_RECOVERED` can exit 0 with `qualificationPassed:false`; it cannot establish Available. No consuming Agents path is permitted by this investigation or by source/component successes.
