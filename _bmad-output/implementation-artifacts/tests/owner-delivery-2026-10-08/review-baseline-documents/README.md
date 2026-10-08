# Owner prerequisite source delivery — 2026-10-08

This continuation delivers reviewable source corrections and private cryptographic prerequisites across the existing owning repositories. **Full prerequisite delivery remains incomplete**: all four records remain Uncommitted, full immutable targets/accepted commands are TBD, and no live Level 4/5 or Story 5.4 completion/readiness is claimed. Owner integration date 2026-10-08 is accepted separately. Branch B, Platform custody ownership and the accepted 365-day effective-at/exclusive-expiry policy remain preserved.

The four full requirement maps are [Parties](ext-parties-1.md), [Conversations](ext-conv-ai-1.md), [custody](ext-secrets-1.md), and [host](ext-host-1.md). Each distinguishes actual Local source proof, unfinished authorized engineering, missing cross-owner decisions/contracts and production qualification. Other owner questions remain unanswered; the packets do not reopen accepted choices.

## Source and review scope

[Owned unified diff](owned-changes.diff), [19-file ownership/before/after hashes](owned-changes.json) and exact `before/`/`after/` copies are the review target. They preserve the original delta even where concurrent external SDK/Platform commits now contain some changes. Unowned package/CI/planning/host/Integration edits are preserved and not attributed to this delivery. No staging, commit, push, branch, submodule initialization/update, deployment or owner message was performed by this implementation.

Changes: EventStore whole-read monotonic deadline/caller-first terminal completion and defensive source snapshots; Conversations protected/opaque metadata rejection and cancellation-safe JSON replay; Parties Local source version consistency; Platform private AES256GCM export wrapping/owned key buffer and candidate detached ES256/offline public-anchor verification; strict Unicode identity validation; complete Local custody class selection/XML/build checks with full Live refusal retained. Existing host source was not changed by this implementation.

[Initial observation](initial-state.json), [intermediate canonical observation](canonical-source-observations.json), [final timestamped canonical observation](canonical-source-final.json), [authorized date-only delta](authorized-date-update.json), [source before consistent repeats](source-before-consistent-repeat.json), [source after](source-after-consistent-repeat.json), and [drift report](final-source-drift.json) record the current checkout basis. The owned19 files matched their review copies at final capture. Five unowned paths changed during whole-source verification, so whole-worktree immutability is explicitly not claimed. Test evidence is separately bound to compiled artifacts and XML/log hashes. Current scaffold source stayed unchanged during its final build and is preserved in `host-current-source/`.

## Fresh results

| Command/evidence | Result | Meaning |
| --- | --- | --- |
| Parties corrected eleven-lane Local, `parties-owner-consistent/` | 1,107 pass; all 11 warning-free Debug builds | Shared SDK/identity/custody and Parties source simulations; complete live P01–P10 still absent. |
| Conversations final Local, `conversations-owner-final/` | 1,553 pass (618+185+711+39); all 4 warning-free Debug builds | Full four suites plus 7 named synthetic lanes; focused 27 already included in Server 711. |
| Custody final Local, `custody-owner-final/` | 155 pass (68 custody+47 cleanup+40 private crypto); warning-free Debug build | S1/S2/cleanup/private S3 byte prerequisites; no direct delivery/lifecycle/receipts/S3/S4 completion. |
| Current host LocalScaffold, `host-current-scaffold.log` | exit0, no warnings/errors; checked source hashes unchanged | Current scaffold only; full host remains unavailable. |
| Negative gates, `negative-gates/evidence.json` | 11 refusals; zero verifier child-dotnet calls | PartiesLive, ConversationsLive, custody default/Live/invalid, host default/Full/invalid/malformed options refuse before build/live resource use. |
| Pre-edit isolated Aspire, `aspire/` | start/describe/ownedstop; zero application resources | Exact isolated scaffold observation only; logs redacted. |

Do **not** add these counts: SDK 20 reader cases overlap PartiesClient 119; custody 115 within Parties overlaps the final custody 155; focused Conversations 27 overlap Server 711. Repeated commands are distinct verification observations, not additional unique coverage.

Meaningful failed-before evidence is retained: SDK 17 = 10 pass/7 fail→current 20 pass (3 further terminal-cancellation vectors); Conversations 27=16pass/11fail→ 27 pass; malformed-Unicode custody 155 = 149 pass/6 fail→ 155 pass. A fresh full Parties graph initially failed (Contracts 28 executed/25 failed) because a source 3.117.1 consumer got a copied SDK 3.117.0 assembly. `parties-owner-final/` is preserved; the corrected fresh full 1,107 result supersedes that attempt without waiving it or changing package pins. One intermediate NSubstitute compilation failure and the corrected negative-gate startup harness attempt remain separately recorded.

[Machine outcomes/exact argv](verification-outcomes.json) and [artifact/evidence hashes](artifact-manifest.json) are the reproducible basis. Builds use existing Debug source-reference routes and isolated artifacts. NuGetAudit=false is a Local build input, not security/production approval.

## Exact final owner commands

Run from `/home/administrator/projects/hexalith/parties`:

```sh
pwsh -NoProfile -File eng/verify-ext-parties-1.ps1 -Mode Local -EvidenceDirectory /home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/parties-owner-consistent -ArtifactsDirectory /tmp/hexalith-owner-parties-owner-consistent-20261008
```

Run from `/home/administrator/projects/hexalith/conversations`:

```sh
pwsh -NoProfile -File eng/verify-ext-conv-ai-1.ps1 -Mode Local -ArtifactsPath /tmp/hexalith-owner-conversations-owner-final-20261008
```

The unique actual subdirectory was `/tmp/hexalith-owner-conversations-owner-final-20261008/run-20261008T1121206133116Z`; root logs/XML/evidence are copied to `conversations-owner-final/`.

Run from `/home/administrator/projects/hexalith/platform`:

```sh
pwsh -NoProfile -File eng/verify-ext-secrets-1.ps1 -Mode Local -ArtifactsDirectory /tmp/hexalith-owner-custody-owner-final-20261008 -EvidenceDirectory /home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/custody-owner-final -EventStoreSourceRoot /home/administrator/projects/hexalith/eventstore
bash eng/verify-agents-host.sh --mode LocalScaffold --artifacts-path /tmp/hexalith-owner-host-current-final-20261008
```

Use fresh artifact/evidence suffixes for a new repeat to preserve this run. The machine index records exact original commands/exitcodes/locations. Full owner compatibility commands remain TBD; these Local commands do not replace them. Full entry/execution/owner qualification and independent root review remain separate required work.
