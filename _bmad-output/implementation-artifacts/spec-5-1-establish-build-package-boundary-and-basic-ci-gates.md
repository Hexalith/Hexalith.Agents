---
title: 'Story 5.1: Finalize Build Package Boundary and Basic CI Gates'
type: 'chore'
created: '2026-09-19'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1b0a65b62df5ee9dca2326d448c4bf56144f9967'
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/implementation-artifacts/spec-5-1-correct-platform-hosting-boundary-and-quality-gates.md'
  - '_bmad-output/implementation-artifacts/spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.1's implementation and prior specs are complete, but sprint status remains `review`, one record still describes a five-package inventory, planning names an obsolete Builds gitlink/Test SDK selection, and the expanded `EXT-HOST-1` contract can be misread as reopening completed work. The last full verifier predates later repository changes.

**Approach:** Run the canonical verifier at current HEAD, correct only Story 5.1 failures, and reconcile current planning with append-only historical evidence. Mark the story `done` only after the complete gate is green; Story 5.6 retains production-like host composition.

## Boundaries & Constraints

**Always:** Preserve sole Hexalith.Builds version authority, the import-only wrapper, Debug/source and Release/package modes, the six-package manifest, root-only submodules, warnings-as-errors, and no module-owned host. Record the full current Builds gitlink and verifier result. Treat the original `EXT-HOST-1` entry gate as satisfied by its then-accepted commitment; its expanded contract now gates Story 5.6/live-host work. Keep `DW-22` and `OD-DAPR-SECURITY-1` open because neither is a proven Story 5.1 failure.

**Never:** Do not add local versions, initialize nested submodules, weaken a gate, present historical commits as current, claim live-host/Workflow evidence, close unrelated decisions, deploy, publish, or push. Never mark Story 5.1 done while verification is red.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Current HEAD is conformant | Full verifier runs | Builds, five test projects, negatives, six-package inspection, and isolated consumption pass | Record immutable evidence; close the story |
| An owned regression exists | A Story 5.1 gate fails | Fix narrowly; rerun the complete verifier | Retain `review` and record any unresolved blocker |
| Evidence differs by date | Prior specs cite five packages or an earlier commit | Date historical facts; state current manifest/gitlink separately | Never silently rewrite history |
| Current host contract is uncommitted | `EXT-HOST-1` expanded after the original gate | Story 5.1 stays complete; Story 5.6/live readiness remains blocked | Do not infer availability or weaken the register |

</frozen-after-approval>

## Code Map

- `eng/verify-story.ps1`, `eng/release-packages.json`, and `scripts/validate-*.py` -- existing canonical verifier and six-package consumer proof; do not create a parallel gate.
- `Directory.{Build,Packages}.props`, `.github/workflows/ci.yml`, and `test/Hexalith.Agents.Server.Tests/` -- existing boundary implementation; patch only after a reproduced failure.
- `_bmad-output/implementation-artifacts/spec-5-1-{correct-platform-hosting-boundary-and-quality-gates,adopt-hexalith-builds-as-the-sole-package-version-authority}.md` -- historical and correction evidence to date-scope and append, never overwrite.
- `_bmad-output/planning-artifacts/{epics.md,architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md}` -- current catalog, host-gate, and delivery claims.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- promote the exact `5-1-establish-build-package-boundary-and-basic-ci-gates` key only after all gates pass.

## Tasks & Acceptance

**Execution:**
- [x] `eng/verify-story.ps1` -- run the complete gate at HEAD; fix only reproduced Story 5.1 regressions and rerun after any patch.
- [x] `_bmad-output/implementation-artifacts/spec-5-1-{adopt-hexalith-builds-as-the-sole-package-version-authority,correct-platform-hosting-boundary-and-quality-gates}.md` -- append current six-package/test/gitlink evidence and label earlier five-package evidence as historical.
- [x] `_bmad-output/planning-artifacts/{epics.md,architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md}` -- refresh catalog facts and assign the expanded host contract to Story 5.6 without retroactively reopening Story 5.1.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- set the exact Story 5.1 key to `done` and remove wording that says review remains pending or separate, but only after green verification.

**Acceptance Criteria:**
- Given current HEAD and root-declared dependencies, when the canonical verifier runs, then every build, test, negative, inventory, and consumer gate passes without warnings or skips.
- Given reconciled artifacts, when Story 5.1 is traced across its specs, epic, architecture, and tracker, then current and historical facts are distinct and status is consistently `done`.
- Given current `EXT-HOST-1` is uncommitted, when ownership is evaluated, then Story 5.1 remains complete while Story 5.6/live qualification stays fail-closed.

## Implementation Notes

- No Story 5.1 build or package regression reproduced at baseline HEAD `1b0a65b62df5ee9dca2326d448c4bf56144f9967`. Review then found that the existing test loops did not fail on skipped or under-discovered tests, so this closure also hardens the canonical verifier with lane-specific minimum counts and `--fail-skips on`, pinned by `PackageInventoryTests`; no parallel gate was created.
- The current root-recorded Hexalith.Builds gitlink is `a74e783bd253d8f9f6b8bed22e65855ac0f96306`, recorded by Agents commit `d0f08bf3945d1869e08beed82881eadf8a654527`. The imported catalog selects Microsoft.NET.Test.Sdk `18.10.1`, EventStore `3.106.0`, and Dapr `1.18.7`.
- The exact release manifest contains six packages: Contracts, EventStore, Client, Agents, UI, and Testing. Earlier five-package statements remain immutable, explicitly dated historical evidence.
- The original `EXT-HOST-1` commitment satisfied Story 5.1's platform-boundary entry gate. The later expanded contract is still `Uncommitted` and now names only its current consumers; Story 5.6 retains production-like composition and live-host evidence.
- `DW-22` and `OD-DAPR-SECURITY-1` remain open. No live-host, Workflow, deploy, publish, or release-readiness evidence is claimed.

## Spec Change Log

- 2026-09-19 -- Ran the complete verifier at current HEAD, recorded the current Builds gitlink/catalog and six-package/test evidence, reconciled Story 5.1 versus Story 5.6 host ownership, and promoted the exact sprint key to `done` only after the full gate passed.
- 2026-09-19 -- Review hardening made both test lanes fail on skips and under-discovery, preserved immutable decision version 1 while adding the current version 2 target, advanced the architecture assumption index, and passed the complete verifier again.

## Review Triage Log

| ID | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| BH-01 | false | `in-review` is the required transient workflow state; the spec becomes `done` only after this review, while the sprint key records the already-verified story outcome. | reject |
| BH-02 | medium | `OD-SPRINT-5.1-5.2-1` remains Open and therefore still blocks its affected dependent-work authorization. The register must explicitly record the new historical-completion evidence without implying that the absent Delivery owner and Platform Maintainer approvals were supplied. | patch |
| BH-03 | false | The historical spec records implementation on 2026-08-04 and final completion on 2026-08-09 after the accepted commitment; the new text claims completion after acceptance, not that ready-for-dev preceded it. | reject |
| BH-04 | medium | `ARCH-A-15` changed its exact Builds/Agents commit assumptions, while the Spine rule requires every assumption change to advance both the numeric and rendered index and the memlog. | patch |
| BH-05 | medium | Replacing the exact gitlink inside immutable `OD-DAPR-SECURITY-1` version 1 mutates its decision contract; the current target must be represented as a successor decision version. | patch |
| BH-06 | medium | The verifier records zero skipped tests but plain `dotnet test` does not fail on skips, so later disabled coverage could leave the closure gate green. | patch |
| BH-07 | false | The version-controlled closure record binds the exact Agents HEAD, Builds gitlink, package manifest, counts, and reproducible verifier; this closure does not newly claim a remote clean-checkout CI transcript. | reject |
| BH-08 | low | The optional context list is not a file inventory, although the task map did not name both registers that the approved host-ownership reconciliation reached. The implementation loaded their referenced authority and the only proposed remedy edits this build's spec. | reject |
| BH-09 | low | Both whitespace checks and the active-record audit ran and passed independently; adding their results would only edit this build's spec. | reject |
| BH-10 | false | `DW-22` already records its source spec, location, severity, settling experiment, and open state; this ledger schema does not require a separate owner or target-story field. | reject |
| ECH-01 | medium | The open sprint decision remains a fail-closed dependent-work authorization even when the tracker records historical completion; that distinction must be explicit in the decision row. | patch |
| ECH-02 | false | The canonical verifier ran after all executable repository changes at the captured baseline; the subsequent reconciliation changed only planning/evidence files, which are covered by the post-edit diff and active-record audits rather than compiled tests. | reject |
| ECH-03 | medium | No `--fail-skips` policy exists in either verifier test loop, so a skipped test can still produce exit zero. | patch |
| ECH-04 | medium | Neither verifier loop sets a minimum expected count, so a misconfigured project that discovers zero tests can exit successfully. | patch |
| ECH-05 | low | The implementation and parent audit ran both `git diff --check` and `git diff --cached --check`; correcting the shorter command text would only edit this build's spec. | reject |
| ECH-06 | false | The `rg` command is an inspection query with a stated semantic expectation, not an assertion by exit code; the parent separately ran fail-closed searches over the active records. | reject |
| ECH-07 | low | A child process can wait until its enclosing job timeout, but this is pre-existing and uncommon; safe process-tree termination and output draining is disproportionate to this closure change. | reject |
| VG-01 | medium | Pre-verified: repository and runner configuration contain no fail-skips policy, so an intentionally skipped test leaves both loops successful despite the zero-skips acceptance criterion. | patch |

## Verification

**Commands:**
- `pwsh -NoProfile -File ./eng/verify-story.ps1 -Story 5.1` -- expected: full Story 5.1 lane passes at current HEAD.
- `git diff --check` -- expected: no whitespace errors.
- `rg -n "five-package|000abf867abc3a99cfa74d39b6e73af05c78a602|18\.10\.0|workflow review remains|5-1-establish-build-package-boundary-and-basic-ci-gates" _bmad-output` -- expected: remaining historical values are date-scoped; the active sprint key is `done`.

**Executed evidence (2026-09-19):**

- The final post-review `pwsh -NoProfile -File ./eng/verify-story.ps1 -Story 5.1` run exited 0 at Agents HEAD `1b0a65b62df5ee9dca2326d448c4bf56144f9967` with per-project minimum-count and fail-on-skip enforcement active in both test lanes.
- Shared package authority passed for 12 projects; the EventStore floor passed for 13 selected rows at `3.106.0 >= 3.105.0`.
- Debug/source and Release/package builds succeeded warning-free. Debug/source tests passed 2,968/2,968; Release/package tests passed 2,944/2,944; no tests were skipped.
- Release-source, pack-source, and missing-source-root probes each failed closed with the expected exit code 1.
- Exact validation passed for all six packages at `0.0.0-story-5-1`, and the isolated consumer restored and built all six references successfully.
