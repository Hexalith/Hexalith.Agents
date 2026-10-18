# Story 5.4 dependency packet reconciliation — 2026-10-18

Reconciles the four delivered owner packets against the [dependency register](../planning-artifacts/external-dependency-register.md). It proposes replacement records and a bounded owner-qualification cohort.

**Acceptance (2026-10-18):** the project owner replied "apply recommended".

- **Accepted:** R-CONV; R-SECRETS and R-HOST on the shared target `abbec40`; cohort `OQ-5.4-1`.
- **Not changed:** R-PARTIES.
- **Date:** no new integration date was proposed, so `2026-10-08` is retained.
- **Not decided:** `PLAT-ACTOR-HISTORY-1`, which had no recommendation, is not registered.

The register records this acceptance. The EXT-SECRETS-1 detached packet was regenerated at `abbec40` under `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/acceptance-abbec40-20261018/`, with `runtime.acceptedReplacementCommit` set to the accepted commit. The qualification run that follows is recorded under `tests/story-5-4-owner-qualification-2026-10-18/`.

When this document was written, no owner command, Live/Full verifier, deployment, Git mutation or consuming Agents path had run. All four records remain `Committed`, none `Available`.

## Packets reconciled

| Record | Delivered packet | Proposed candidate |
| --- | --- | --- |
| EXT-CONV-AI-1 | `../conversations/docs/implementation/ext-conv-ai-1-runtime-qualification.md` + `spec-ext-conv-ai-1-runtime-delivery.md` (status `in-progress`, review log empty) | `<reviewed-full-commit>` placeholder; Live runner landed in `a9b16e7d8c7ebfb617f1ad23038dcdaf983eaa51` |
| EXT-PARTIES-1 | `../platform/docs/implementation/ext-parties-1-platform-prerequisites-2026-10-18/` (untracked in Platform; unsigned decision form, `executable: false`) | none |
| EXT-SECRETS-1 | `../platform/_bmad-output/implementation-artifacts/spec-5-4-ext-secrets-1-runtime-delivery.md` + detached packet `/home/administrator/hexalith-secrets-evidence/ext-secrets-1/runtime-delivery-20261017-9b73d77/detached-packet/` | `9b73d777590a58f847e90d3658b09efd4185be63` |
| EXT-HOST-1 | `../platform/_bmad-output/implementation-artifacts/spec-5-4-ext-host-1-runtime-composition.md` + detached packet `/home/administrator/hexalith-host-evidence/ext-host-1/runtime-composition-abbec40/` | `abbec4093715c1db854433119b32d4e16df20d08` |

Owner HEADs observed after fetch: Conversations `87e8b4a6ec3224b20a560b0e83de857805925572` (three unrelated modified files), Parties `d2ec85c9e2b85e2b0c7a50d92ffaf197590ea053` (clean), Platform `d1eb16166ee1a62b791a3201cdcd689f33f4fa8f` (the Parties packet directory is untracked), EventStore `ab8fa115c486875be3a22860a273d5d68a26c749` (two modified docs).

## Findings

### F1 — Platform target is not canonical

`f448db29493c1b30b0f18380188fe99b8b7606fe` is pinned by both EXT-SECRETS-1 and EXT-HOST-1. It is not reachable from `main` or `origin/main`, and no ref contains it. It is a pre-rewrite copy of `bb6a8c011640844b65b8603f8a5f9256709c6239`, which has the same subject and author date. The two commits have different trees (`cf3b987…` vs `b2d2336…`) and different parents, and 11 files differ between them. The pinned object survives only in the local object store and can be garbage-collected. Neither record has a durable canonical target.

The other two pins are canonical: Conversations `8469c5c…` and Parties `56cb440…` are on `origin/main`.

### F2 — No pinned command can pass

Each pinned target hardcodes a closed Live/Full gate:

- **Conversations** `8469c5c:eng/verify-ext-conv-ai-1.ps1:45`: `throw 'Live gate closed before any calls…'`.
- **Parties** `56cb440:eng/verify-ext-parties-1.ps1:28-39`: Live mode always ends `exit 1`. HEAD `d2ec85c9` is unchanged.
- **Platform** `f448db2:eng/verify-ext-secrets-1.ps1:12-13`: Live mode always throws `DependencyNotAvailable: EXT-SECRETS-1`.
- **Platform** `f448db2:eng/verify-agents-host.sh`: Full mode refuses.

The [2026-10-10 exact-target run](tests/owner-delivery-2026-10-08/root-revision12-exact-20261010T1212Z/README.md) already recorded all four exiting 1 at these gates. Re-running them would reproduce that result and could not establish `Available`.

### F3 — Candidate commands

| Record | Candidate pass path | Blocking defect |
| --- | --- | --- |
| EXT-CONV-AI-1 | Real C1–C4 Live runner (`eng/verify-ext-conv-ai-1-live.ps1`); it reads the committed Agents register and requires the target commit to equal the register's `TargetVersionOrCommit` and HEAD. | The packet's command shape omits `-LiveManifest`/`EXT_CONV_AI_1_LIVE_MANIFEST`, so a run returns `BLOCKED` until a manifest is pinned. No candidate commit is named or reviewed. |
| EXT-PARTIES-1 | None. | Live mode is unconditional `exit 1` at every commit. |
| EXT-SECRETS-1 | Real path through `eng/ext-secrets-1/qualify.py` (234-case plan, `planSha256` `16dd493f…`). | `qualify.py:acceptance()` requires the register date to be literally `2026-10-08` and reads an extra `RuntimeQualificationBinding` row. That row is not part of the nine-field schema. |
| EXT-HOST-1 | None. | `eng/ext-host-1/verify_full.py:verify()` returns 1 on every path, even with all bindings present and passing suites. `verify-agents-host.sh` never forwards `--profile`. Full runs only unit suites (custody and `GovernanceScopeGuardTests`), so it could not supply live Level 4 evidence. |

### F4 — Installed configuration and trust bindings are absent

| Record | Missing installed inputs (references only) | Trust bindings |
| --- | --- | --- |
| EXT-CONV-AI-1 | All 13 manifest fields, including service Party ID, worker enrollment, machine binding, approval issuer, receiver and receipt lookup, and storage identity. | None of the eight `env:` credential references is installed. No independent receiver authentication exists. |
| EXT-PARTIES-1 | Twelve Live inputs: gateway, tenants A/B, three credentials, policy, custody, restore, failure-injection, issuer and subject. Plus decision-form B01–B12, Q1–Q7, F1/F2 and D01–D04 (all `null`). | No issuer or role authority. Custody depends on `PLAT-ACTOR-HISTORY-1`, which is not a register record. |
| EXT-SECRETS-1 | 69 missing inputs in `blocked-inputs.json`, including profile validity, signer, replay registrar, compromise subscriber, all-copy lifecycle and nonrollback restore. | The executor, observer and enrollment P-256 anchors are all `null`. |
| EXT-HOST-1 | 14 missing profile references: current tenant, global admin, human identity, approver evidence, replay, recorder, worker, spool backend, replica count, failure model, protection engine and observer, Parties binding, Conversations binding. | Replay, recorder and worker credentials are not minted. No protection observer exists. |

### F5 — Contract coverage gaps outside the four packets

- `EXT-PROTECTION-1` is `Uncommitted` (target and command `TBD`). EXT-HOST-1 needs it (`protectionEngine`, `protectionObserver`), EXT-SECRETS-1 needs it (`runtime.protectionOwnerReference`), and so does the Conversations C2 `ProtectedDeletionRevision`. No qualification of those three can pass until it is committed and passing.
- Parties custody T2–T6 is assigned to the symbolic `PLAT-ACTOR-HISTORY-1`, which has no register record. Required human exact-Conversation and class/time-range tests remain blocked on the Open Product decisions named in EXT-HOST-1.
- Story 8.3, the Agents deletion receiver, is `backlog` in sprint status, so no receiver implementation exists.

## Qualification dependency cycles

Each edge below is "the record's owner command needs this installed and qualified seam".

```text
Cycle A (Platform):  EXT-SECRETS-1 ──protectionOwner/migrationAuthority/compromiseGuardMirror──▶ EXT-HOST-1
                     EXT-HOST-1    ──replay/recorder/worker private credentials, signer compose──▶ EXT-SECRETS-1

Cycle B (cross-owner): EXT-CONV-AI-1 ──C2–C4 receiver + independent receipt lookup──▶ Agents 8.3 receiver
                       Agents 8.3 receiver ──hosted by──▶ EXT-HOST-1 ──conversationsBinding──▶ EXT-CONV-AI-1
                       Agents 8.3 is a listed EXT-CONV-AI-1 consumer ⇒ must fail closed until EXT-CONV-AI-1 is Available

External (acyclic) blockers: EXT-PROTECTION-1 (Uncommitted) ◀── EXT-HOST-1, EXT-SECRETS-1, Agents 8.3
                              PLAT-ACTOR-HISTORY-1 (unregistered) ◀── EXT-PARTIES-1
                              EXT-PARTIES-1 ◀── EXT-HOST-1 (partiesBinding), EXT-CONV-AI-1 (preflight `party`)
```

Sources for each edge:

- **Cycle A:** `../platform/eng/ext-secrets-1/qualify.py` `REQUIRED`; the `AgentsDeploymentProfile` slots and `prepare_acceptance.py` `REFERENCE_KEYS`; register EXT-HOST-1 RequiredArtifact and its v21/v23 extensions.
- **Cycle B:** the runtime-qualification packet's last paragraph; register EXT-CONV-AI-1 `ConsumingStories`; register Accepted Status Semantics.

Under the register, Cycle B cannot close without executing a consuming Agents path. That path is forbidden.

## Proposed bounded owner-qualification cohort `OQ-5.4-1` (for explicit acceptance)

This proposal adds no exception to the register's semantics and enables no consuming Agents path. It defines one owner-command sequence for each cycle and the conditions under which those runs may set `Available`.

1. **Break Cycle B with an owner-qualification receiver.** During EXT-CONV-AI-1 owner qualification, the receiver role is a contract-conformance receiver. It implements `DeletionSignalAccepted(SignalId, SourceRevision, ProtectedDeletionRevision)` plus independent receipt lookup.
   - It is installed and authenticated separately from Conversations.
   - It contains no Agents package or runtime path.
   - Its evidence counts as Level 4 component evidence only.

   Agents 8.3's real receiver is proven later, under Story 8.3 and `RQ-1` Level 5. This matches the register's rule that Level 5 is completed by the consuming gate. Adopting this changes EXT-CONV-AI-1's verification composition: the manifest's `bases.receiver` and `credentialReferences.receiver` point to this receiver.
2. **Close Cycle A with a joint Platform cohort.** EXT-SECRETS-1 and EXT-HOST-1 must pin the **same** Platform commit, because both run from `../platform`. One cohort run executes, in fixed order:
   - **Phase 0:** the EXT-PROTECTION-1 owner command, once that record is committed.
   - **Phase 1:** secrets cases with no host references.
   - **Phase 2:** host composition using the phase-1 credentials, with every Agents consumer seam asserted to refuse with `DependencyNotAvailable`.
   - **Phase 3:** the mutual cases (secrets S3/S4/v23 compromise and protection-block; host H3/H4/v21/v23).

   Both records become `Available` only if the entire run passes. Any failure leaves both non-Available.
3. **Bounds.**
   - Exactly the accepted commits, profile, configuration, plan and anchor hashes. Any change voids acceptance.
   - One attempt per accepted target set. Original recovery journals and every attempt directory are retained.
   - Agents runtime paths are never enabled.
   - Prerequisites before any cohort run: EXT-PROTECTION-1 `Committed`, a host verifier with a pass path and a pinned `--profile`, and all F4 inputs installed.

## Proposed replacement records (pending acceptance)

Only changed fields are shown. Accepting a changed target or command returns that record to `Uncommitted` until the acceptance is recorded, then to `Committed`. None becomes `Available` by acceptance.

### R-CONV — EXT-CONV-AI-1

| Field | Proposed value |
| --- | --- |
| `TargetVersionOrCommit` | `87e8b4a6ec3224b20a560b0e83de857805925572` (`origin/main` tip; contains `a9b16e7` Live runner and `676c0d8`; only `.github/workflows` changed after `676c0d8`) |
| `TargetIntegrationDate` | Owner to set (2026-10-08 has passed and was not met) |
| Command | `test "$(git -C ../conversations rev-parse HEAD)" = "87e8b4a6ec3224b20a560b0e83de857805925572" && test -z "$(git -C ../conversations status --porcelain)" && pwsh -NoProfile -File ../conversations/eng/verify-ext-conv-ai-1.ps1 -Mode Live -LiveManifest /home/administrator/hexalith-conversations-evidence/ext-conv-ai-1/live-manifest.json -ArtifactsPath /tmp/ext-conv-ai-1-live` |
| Contract note | Receiver role per `OQ-5.4-1` item 1, if accepted |

Caveats: the candidate has not been owner-reviewed (spec `in-progress`), and the manifest path is proposed, not installed.

### R-SECRETS — EXT-SECRETS-1 and R-HOST — EXT-HOST-1 (shared Platform target)

| Field | Proposed value |
| --- | --- |
| `TargetVersionOrCommit` (both) | `abbec4093715c1db854433119b32d4e16df20d08`. It is canonical on `origin/main` and contains secrets candidate `9b73d77`. The secrets/custody paths change by 84 lines (tests, provisioning, csproj) between the two commits; none of the secrets/host paths change from `abbec40` to HEAD `d1eb161`. |
| `TargetIntegrationDate` (both) | Owner to set. `qualify.py` currently rejects any value other than `2026-10-08`, so a new date needs that parser fixed. |
| EXT-SECRETS-1 command | The `prepare_acceptance.py` shape, regenerated from a clean `abbec40` checkout into a fresh detached directory: `test "$(git -C ../platform rev-parse HEAD)" = "abbec4093715c1db854433119b32d4e16df20d08" && test -z "$(git -C ../platform status --porcelain)" && env EXT_SECRETS_1_RUNTIME_CONFIGURATION=<detached>/runtime-reference.json EXT_SECRETS_1_ACCEPTANCE_PACKET=<detached>/replacement-acceptance-packet.json pwsh -NoProfile -File ../platform/eng/verify-ext-secrets-1.ps1 -Mode Live -EventStoreSourceRoot ../eventstore -ArtifactsDirectory <unique> -EvidenceDirectory <unique>` |
| EXT-SECRETS-1 `RuntimeQualificationBinding` row | `{"configurationSha256":"<regenerated>","planSha256":"16dd493ff0414fea13be95f137d372e54088881550665aa3239a5814a9e862ed","executorAnchorSha256":null,"observerAnchorSha256":null,"enrollmentAnchorSha256":null}`. This tenth field is read by `qualify.py`; with `null` anchors it can never match. |
| EXT-HOST-1 command | Packet form: `test "$(git -C ../platform rev-parse HEAD)" = "abbec4093715c1db854433119b32d4e16df20d08" && test -z "$(git -C ../platform status --porcelain)" && bash ../platform/eng/verify-agents-host.sh --mode Full --artifacts-path /tmp/ext-host-1-full` |

Caveat: this re-pins to a canonical target only. At `abbec40`, the EXT-HOST-1 command can never pass (F3), and EXT-SECRETS-1 is blocked by 69 inputs. If you need the original packet's commit instead, the alternative secrets-only target is `9b73d777590a58f847e90d3658b09efd4185be63`. Using it means secrets and host cannot be co-installed for `OQ-5.4-1`.

### R-PARTIES — EXT-PARTIES-1

No replacement is proposed. The pinned `56cb440…` is canonical, and no newer commit has a Live pass path. A separate decision is whether to register `PLAT-ACTOR-HISTORY-1` as the Parties custody dependency.

## Execution status

No owner command was executed. Execution waits for your acceptance under the instruction this run received. Even with every replacement accepted, no command can pass today:

- Parties and host have no pass path.
- Secrets would stop at `DependencyNotAvailable` with its missing inputs.
- Conversations would stop at `BLOCKED` with no installed manifest.

A run would therefore only add more refusal evidence. No record may be marked `Available`.
