---
title: 'Move Submodules to References'
type: 'chore'
created: '2026-08-01'
status: 'done'
baseline_commit: '43d37904c22f7ee9fadd73044b76383ab7700272'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository declares and checks out ten root-owned Git submodules at the repository root even though the repository instructions and ecosystem convention require root-declared dependencies to live directly under `references/`. The mismatch also leaves build discovery and tooling tied to the obsolete layout.

**Approach:** Relocate every root-declared submodule to `references/Hexalith.*`, preserving its URL and recorded commit, then update all active root-repository consumers and path-bearing documentation links to the new layout.

## Boundaries & Constraints

**Always:** Move exactly the ten gitlinks declared by the root `.gitmodules`; preserve section names, URLs, and gitlink SHAs; use explicit non-recursive Git operations; keep every nested submodule uninitialized; retain the intentional `..\Hexalith.*` standalone sibling-checkout fallbacks; update live build/tool paths and actual relative documentation links.

**Ask First:** Stop if a source submodule is dirty, a destination exists, a gitlink SHA would change, a nested submodule is initialized, or the work would require modifying submodule-owned content.

**Never:** Run recursive submodule initialization/update, move `.git/modules` manually, update submodules to newer commits, edit files inside a submodule, rename `.gitmodules` sections, or bulk-rewrite archival story prose and namespace/package references that are not filesystem links.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Clean initialized checkout | Ten clean root gitlinks at recorded SHAs | Ten initialized worktrees under `references/`; no root-level gitlinks | Halt on collision, dirty state, or SHA drift |
| Nested declarations | Root submodules declare their own `references/` dependencies | Nested worktrees remain uninitialized | Never invoke a recursive command |
| Fresh or missing dependency checkout | A required dependency is absent during restore/build | `CheckSubmodules` tests `references/<name>` and reports the exact scoped init command | Fail with actionable `references/...` guidance |
| Review-readiness execution | Tool needs the shared Builds catalog | Catalog resolves from `references/Hexalith.Builds` | Preserve fail-closed missing-file behavior with the corrected path |

</frozen-after-approval>

## Code Map

- `.gitmodules` and the ten `Hexalith.*` gitlinks -- authoritative submodule paths, URLs, and recorded commits.
- `Directory.Build.props` -- sibling-source roots and the pre-restore/build required-submodule check.
- `tools/check-story-review-readiness.py` -- resolves the root Builds package catalog for nested builds.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- path fixtures and representative submodule porcelain coverage.
- `_bmad-output/planning-artifacts/{architecture,ux-designs}/**/*.md` -- five groups of live relative links into sibling checkouts.

## Tasks & Acceptance

**Execution:**
- [x] `.gitmodules`, `Hexalith.*`, `references/Hexalith.*` -- relocate all ten gitlinks with explicit `git mv` operations and preserve Git metadata and pointers.
- [x] `Directory.Build.props` -- resolve normal workspace dependencies under `references/`, retain standalone sibling fallbacks, and correct the missing-submodule diagnostic and init command.
- [x] `tools/check-story-review-readiness.py`, `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- move the Builds catalog assumption and regression fixtures to `references/`.
- [x] `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/{DESIGN,EXPERIENCE}.md`, `_bmad-output/implementation-artifacts/spec-add-hexalith-{builds-submodule,submodules}.md` -- repair relative links that would break after relocation.

**Acceptance Criteria:**
- Given the updated index, when `.gitmodules` paths and mode-`160000` entries are compared, then both sets contain the same ten direct children of `references/` and no root-level gitlink remains.
- Given the pre-move gitlink map, when relocation completes, then every submodule name, URL, and SHA is unchanged and all ten worktrees remain usable.
- Given each root submodule's nested declarations, when their status is inspected without recursion, then every nested entry remains uninitialized.
- Given a normal workspace build and the review-readiness tooling tests, when validation runs, then source roots and the Builds catalog resolve from `references/` and all checks pass.
- Given the repaired relative links, when resolved from their containing Markdown files, then they target existing content under `references/`.

## Spec Change Log

## Design Notes

Keep `.git/modules/Hexalith.*` in place. Modern Git rewrites each moved worktree's `.git` indirection and `core.worktree` during `git mv`; the public checkout path changes while the private repository storage does not.

## Verification

**Commands:**
- Compare sorted `.gitmodules` paths with sorted mode-`160000` index paths -- expected: identical ten-entry sets under `references/`.
- `git submodule status -- <all root-declared references paths>` -- expected: ten initialized entries at their original SHAs.
- `dotnet msbuild src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj -target:CheckSubmodules -property:Configuration=Release` -- expected: success.
- `PYTHONDONTWRITEBYTECODE=1 python3 tests/tooling/story_review_readiness/story_review_readiness_test.py -v` -- expected: all tests pass.
- `dotnet restore Hexalith.Agents.slnx` then `dotnet build Hexalith.Agents.slnx --configuration Release --no-restore` -- expected: success.
- `git diff --check` and `git diff --cached --check` -- expected: no whitespace errors.

## Suggested Review Order

**Submodule manifest and gitlinks**

- Start with the authoritative relocation of all ten root-declared paths.
  [`.gitmodules:2`](../../.gitmodules#L2)

**Build and tooling resolution**

- Follow normal workspace source discovery into `references/` while preserving standalone fallbacks.
  [`Directory.Build.props:17`](../../Directory.Build.props#L17)

- Verify missing build dependencies report the exact scoped initialization command.
  [`Directory.Build.props:47`](../../Directory.Build.props#L47)

- Confirm nested builds consume the relocated shared Builds catalog.
  [`check-story-review-readiness.py:402`](../../tools/check-story-review-readiness.py#L402)

**Regression coverage**

- Check submodule porcelain parsing with the new nested path shape.
  [`story_review_readiness_test.py:313`](../../tests/tooling/story_review_readiness/story_review_readiness_test.py#L313)

- Check the successful gate fixture creates Builds beneath `references/`.
  [`story_review_readiness_test.py:421`](../../tests/tooling/story_review_readiness/story_review_readiness_test.py#L421)

**Documentation links**

- Verify architecture provenance now resolves through the relocated sibling checkouts.
  [`ARCHITECTURE-SPINE.md:21`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#L21)

- Verify UX design sources follow Tenants and FrontComposer under `references/`.
  [`DESIGN.md:12`](../planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#L12)

- Verify experience sources retain precise component-level targets after relocation.
  [`EXPERIENCE.md:11`](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#L11)

- Finish with the historical submodule-spec review links.
  [`spec-add-hexalith-builds-submodule.md:23`](spec-add-hexalith-builds-submodule.md#L23)
  [`spec-add-hexalith-submodules.md:33`](spec-add-hexalith-submodules.md#L33)
