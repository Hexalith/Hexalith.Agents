# Story 5.4 owner delivery reconciliation — 2026-10-22

**Disposition: blocked before replacement acceptance and installation.** New owner work completes source/component scopes, not the complete installed deliveries required by the [external dependency register](../planning-artifacts/external-dependency-register.md). No complete replacement commitment can be submitted from these packets: required deployment, trust, implementation and date fields are unresolved. Accepting a canonical source commit alone would not meet the requested commitment standard.

The [2026-10-18 reconciliation](story-5-4-dependency-reconciliation-2026-10-18.md) and its acceptance remain authoritative. This document changes no commitment, status, integration date, checkout, installed configuration or attempt. No owner Live/Full command, qualification receiver, OQ-5.4-1 phase or consuming Agents path ran. No cohort attempt was reserved. Existing journals and attempts were not opened for writing. Story 5.4 remains in progress; the condition for resuming its build is unmet.

## Evidence read and preserved

The investigation read all ten new Platform acceptance packets (completion, continuation preview/final, F1, and three shared host/secrets generations), the new Conversations blocked acceptance packet and readiness/review/publish records, the Parties direct runtime handoff and Platform prerequisite decision packet, and EventStore's full updated protection acceptance packet including the October 21 proposal and October 22 checkpoint. It also reconciled the subsequent Dapr-only custody architecture approval. Exact paths, file hashes, commands, complete missing-input lists and scope distinctions are retained in:

- [Platform owner packets](tests/story-5-4-reconciliation-2026-10-22/platform-owner-packets.md)
- [Parties and Conversations owner packets](tests/story-5-4-reconciliation-2026-10-22/identity-owner-packets.md)
- [Protection owner packets](tests/story-5-4-reconciliation-2026-10-22/protection-owner-packets.md)
- [Machine-readable reconciliation](tests/story-5-4-reconciliation-2026-10-22/reconciliation.json)
- [Static verification receipt](tests/story-5-4-reconciliation-2026-10-22/verification.json)

Hashes identify exact evidence bytes, not owner acceptance or installed trust. Canonical ancestry was checked directly against each repository's locally recorded `origin/main`; no fetch was performed. Sibling working trees have unrelated ongoing work, which was preserved. Root `references/` checkouts differ from the sibling checkouts addressed by the owner commands. No submodule was initialized or updated.

## Accepted records and new candidate disposition

All targets below are full identifiers obtained directly from Git, not reconstructed abbreviations.

| Record | Existing accepted target/status | New canonical source observation or handoff | Replacement disposition |
| --- | --- | --- | --- |
| EXT-SECRETS-1 | `abbec4093715c1db854433119b32d4e16df20d08`, Committed | Shared handoff `3b4151df682448557d58a09027a05c5b41b6ce31` | IncompleteSourceCandidate; 83 overlapping required/derived/implementation/acceptance blockers; three absent anchors; no joint Live runner |
| EXT-HOST-1 | `abbec4093715c1db854433119b32d4e16df20d08`, Committed | Same shared handoff `3b4151df682448557d58a09027a05c5b41b6ce31` | Fourteen empty profile slots; Full has a component pass path but supplies no joint Live Level 4 qualification |
| EXT-CONV-AI-1 | `87e8b4a6ec3224b20a560b0e83de857805925572`, Committed | Current source `60f18214d7a4565d28096628af95d867ddd5e874` | All 31 reviewed snapshot files match published source; blocked packet remains NotSubmitted with no installed manifest, complete runtime target/command or hashes |
| EXT-PARTIES-1 | `56cb4401c4179d431385ad3d931fefa402e4fc5a`, Committed | Handoff `ee4a4b8736131c0e7ce480b6dc419dcc0c266c3b`; current source `9a46682ad170ba70e395aa00012f86f09ef6b315` | Live still unconditionally exits 1; full P-01–P-10 implementation and actor-history backend/authority are absent |
| EXT-PROTECTION-1 | Target unresolved, Uncommitted | Current source `995117f9b899a2994e99257091b0ea35cfbe5090` | Owner expressly says incomplete delivery, do not accept as Committed; G1–G7 remain open |

These source observations are reviewable but are not proposed complete integration targets. The unchanged nine-field accepted records remain available in the machine-readable attachment so the full artifacts and consumer lists are retained. No contract scope was reduced to make a candidate acceptable.

The current Platform pair supersedes `60472462e4b43f4d328260199b22655639d36dd3`. It includes `c0edad7607eaa5b96f8d601097c76db244a38a2f`, which rechecks persisted evidence and required Passed cells when replaying an attempt. The intermediate `c0edad76` pair and the final `3b4151d` pair remain retained. Reusing the older pair would omit a meaningful verifier correction.

## Concrete command and hash review

Exact owner command strings are copied into the reports and `reconciliation.json`, including their absolute detached paths. They are review data; no script executing them was created. Their present conditions are:

| Record | Existing candidate command | Why it cannot qualify the complete record |
| --- | --- | --- |
| EXT-SECRETS-1 | Shared packet's exact-HEAD/clean guard, two pinned environment references, `verify-ext-secrets-1.ps1 -Mode Live` | Missing implementations, independently installed authorities/anchors and JointLivePhaseRunner; unaccepted replacement configuration |
| EXT-HOST-1 | Same exact-HEAD/clean guard, `verify-agents-host.sh --mode Full --profile /tmp/ext-story-54-shared-3b4151d/host/agents-deployment-profile.json` | Default profile incomplete; a component exit 0 deliberately retains dependencyAvailable=false, productionEngineRegistered=false and liveEffects=0 |
| EXT-CONV-AI-1 | Existing accepted Live command still points to absent `/home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json`; replacement exact command is null | No installed source/worker/Party/approval/receiver/lookup/transport bindings; no accepted configuration/plan/profile/anchor pins |
| EXT-PARTIES-1 | Handoff pins Parties `ee4a4b8736131c0e7ce480b6dc419dcc0c266c3b`, EventStore `ab8fa115c486875be3a22860a273d5d68a26c749`, Platform `2ac8e73c09fe631c5620723b54784d2e28e8941f`, then Live | Refusal-only Live mode; Platform pin also differs from the new shared handoff, requiring coordinated requalification |
| EXT-PROTECTION-1 | `python3 ../eventstore/tools/verify-ext-protection-1.py --mode Live --profile /home/administrator/hexalith-protection-evidence/ext-protection-1/accepted-profile.json --evidence-root /home/administrator/hexalith-protection-evidence/ext-protection-1/attempts` | Profile does not exist; no complete accepted target, installed engine/roles/trust or physical provider proof |

Current shared packet pins:

| Binding | SHA-256 / actual state |
| --- | --- |
| Host acceptance packet | `ab0dd589f27ac8841ea9b1a441b452909bae15efce9175edfc0bb63a22e10b8a` |
| Secrets acceptance packet | `4e6e30f608a86906f15bb4ca46e7a8b4e64cac0518146ecee09616afc3459ed1` |
| Host profile | `f93680dfb9ae76f963005f0058622672c567612532591555e501dd3d13db7083` — hashes an incomplete profile |
| Host plan | `8d9716c0348f40fc9caab75aa3ebc1d8df9718aa7f6a4234c2a91be0de225679` |
| Secrets configuration | `b1a99a90583489424fcc236541d15ccf7bd0abe8ca8c8e33b9dccc966d01b8c8` — hashes an incomplete configuration |
| Secrets plan | `16dd493ff0414fea13be95f137d372e54088881550665aa3239a5814a9e862ed` |
| Executor / observer / enrollment anchors | Each null; no actual anchor hash exists |
| Null-anchor map | `53f138498b53f560ce6957f054d6feb7d27cdee2190c23bbf4331a3fcb89e160` — not an installed trust anchor |
| Protection 41-case plan | `c1e7759a2687c122dd182aa48f1828e531166c0157a04b2ae0eba35e9ac4d10c` — no installed profile or role/engine hashes |

Secrets canonical JSON has no trailing newline; host canonical JSON has one. Current raw configuration/profile bytes match their respective owner canonicalizers. The reports distinguish packet-file hashes from plan-object hashes. No empty trust field was replaced with a synthetic key or digest.

## Integration dates

The four accepted records still name **2026-10-08**. That date was missed; repeated historical dates in new packets are not new delivery promises. Both current Platform verifiers still enforce it literally, and the host plan/profile hash binds it. A new date requires owner verifier corrections and fresh coordinated profile/plan/configuration/packet hashes before replacement acceptance.

Protection explicitly proposes **2026-11-02**, unaccepted. It supplies no complete delivery target or installed profile to accompany that date. No owner packet supplies an accepted new date for Parties or Conversations. Dates cannot be truthfully completed by copying the protection proposal across other owners or relabeling the missed date. This is an outstanding commitment field for the owners to return, not a request to redecide the accepted policy/timing bounds.

## Remaining blockers and accountable deliveries

1. **Platform custody and EventStore protection:** Implement/install the adopted Dapr-only `custody-lifecycle-v1` semantic component/provider, actual key delivery and complete batch/lifecycle composition, independent current/nonrollback authority, complete copy/holder inventory, all-or-none erasure and exact original receipts. The architecture is already approved; direct native application SDKs and the earlier AWS exception were superseded. Existing adapters with null underlying installations fail closed and do not supply the provider. G1–G7 details and required independent roles/engine/trust/recovery chains are in the protection report.

   The retained native OpenBao negative control has SHA-256 `6efa381bc70b3de57a24142e5e679f53781c7f89d5ab6740fb7664966f30888e`, result `ObservedRevival`, `g1Qualified:false`. Ordinary Raft restore revived deleted key material and plaintext. HMAC custody, restored tombstones or a Dapr wrapper do not establish irreversible erasure. No new destructive probe was authorized or run by this reconciliation.
2. **Platform qualification composition:** Supply all 83 named secrets prerequisites and fourteen host references; independently install executor/observer/enrollment programs, public P-256 trust and scoped opaque credentials; deliver the real JointLivePhaseRunner. Host references are currentTenant, globalAdministrator, humanIdentity, approverEvidence, replay, recorder, worker, spoolBackend, failureModel, protectionEngine, protectionObserver, partiesBinding, conversationsBinding and replicaCount. These are actual installations/proofs, not strings to fill into defaults. Approval private keys remain outside Platform/Agents custody.
3. **Parties and Platform retained history:** Implement the full authenticated persisted P-01–P-10 verifier and production history custody/all-copy destruction/nonrollback restore/successor-continuity sources. The Platform prerequisite form has fifteen unresolved command bindings and thirteen unresolved decisions (D01–D04, Q1–Q7, F1/F2). `PLAT-ACTOR-HISTORY-1` remains a symbolic, unregistered owner work item; adding a new commitment would need a complete owner contract. Preserve Branch B and the already accepted 365-day exclusive-expiry policy.
4. **Conversations and installation authorities:** Install and enroll the dedicated service Party, worker/machine binding, approval issuer, current command/source/publication authority, delivery transport, separate conformance receiver and independent lookup. Freeze the retained attempt-journal location, complete manifest and twelve hash-bound input files. All eleven stable readiness blocker codes and every missing installed binding are in the identity report. Publishing the receiver and matching reviewed source does not install it. Its component evidence is Level 4 only; it performs no Agents Story 8.3 path.
5. **All owners:** Return a coordinated complete target set with fresh detached packets, explicit credible integration dates, executable commands, actual installed hashes and independent trust. The present Parties handoff and new Platform pair do not use one Platform target. A target or hash change requires renewed acceptance. Existing fixed `/tmp` command output paths also require retained-original handling before any later accepted execution; do not run them as convenience diagnostics now.

## Acceptance and execution boundary

There are **zero complete changed commitments ready for acceptance**. The [draft build spec](spec-5-4-reconcile-owner-deliveries.md) records the authorized full objective and is not ready for development while these owner deliveries remain missing. Neither “approve source” nor accepting null fields can establish the requested complete commitments. The existing accepted records are not relabeled Uncommitted merely because unsubmitted candidate packets exist.

When owners return complete deliveries, compare the final immutable bytes, present all changed nine-field commitments and operational hash bindings for explicit acceptance, and only then install the exact clean target set without disturbing unrelated owner work. Preserve OQ-5.4-1 exactly:

1. Before starting, EXT-PROTECTION-1 is Committed, the host verifier has the required pass path and pinned actual profile, all installed inputs/anchors exist, and secrets/host share the accepted Platform target.
2. Execute one retained cohort attempt in phase order: **0 protection owner command → 1 secrets without host references → 2 host using phase-1 credentials with every Agents consumer seam refusing DependencyNotAvailable → 3 mutual secrets S3/S4/v23 and host H3/H4/v21/v23 cases**.
3. Make both Platform records Available only if the entire required run passes. Unknown/lost outcomes retain the same identity and permit exact original recovery lookup; recovery-only exit 0 with qualificationPassed=false is insufficient. Keep every attempt directory, original recovery journal and evidence digest. No journal reset or retry under a new artifacts path bypasses the single-attempt bound.
4. Execute the other accepted owner commands only with their own complete exact-target prerequisites. The Conversations receiver remains separately installed/authenticated with no Agents package; its evidence never substitutes for Story 8.3/RQ-1 Level 5.
5. After every relevant record is Available at the installed targets, resume `bmad-build 5.4` in Agents. That story still needs its production bindings, AD-30 admission/replay/spool integration, persisted acceptance matrix and complete review. Owner availability alone does not mark the story done.

No repeated refusal run would repair the missing deliveries. This run stops at their documented delivery blockers and preserves the remaining OQ-5.4-1 attempt.
