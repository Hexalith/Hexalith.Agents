---
title: 'Resolve five deferred package-tooling and contributor items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Deferred release-tooling gaps admit traversing archive paths, inconsistent package metadata, and unrelated archives at publication. Contributors also lack guidance for the enforced commit/release/test workflow and coupled Builds updates.

**Approach:** Complete these five distinct ledger items: reject unsafe archive paths before normalization; enforce the centrally declared MIT license, project/repository provenance and packaged README; publish only validated manifest-declared package archives for the requested version; document Conventional Commits, Node tooling, semantic-release and Microsoft.Testing.Platform; document the coupled Builds gitlink/workflow/test update procedure. Preserve the six-package inventory, publication freeze, source recheck and collision-failing policy. Add executable local archive and publisher regressions, update the five ledger entries, review, commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Baseline: clean main at `f21d8adfb0ac7a7b5f06ec176dad1c8fd204fc77`. Freeform five-item request explicitly authorizes this bundle and main push; no sprint story key.
- Route facts: no unresolved user-visible choices, migrations, deployment or package publication. Bounded reversible tooling/documentation changes with no new runtime API.
- Code map: `scripts/validate-nuget-packages.py` reads nuspec metadata and checks exact inventory/dependencies but strips leading traversal and accepts arbitrary nonempty license. `Directory.Build.props` supplies MIT, GitHub project/repository URLs, git repository type and README.md. `scripts/publish-release-packages.sh` already checks absence then re-proves source, but pushes a wildcard. `eng/release-packages.json` owns six IDs. Existing tooling discovery automatically includes new fixture tests; no executable archive-validator fixtures currently exist.
- Planned changes: extend the existing validator to select and validate exact manifest/version paths for publication while its ordinary full-directory gate keeps rejecting extras; shell publication preflights the complete selected set before any push. Add Python archive fixtures and controlled publisher subprocesses proving unrelated archives never reach dotnet and preflight/source/push failures stop publication. Add `CONTRIBUTING.md` linked from README, with current repository commands, freeze/dispatch semantics, and an explicit atomic Builds pin-update procedure naming CI/release/security callers and conformance assertions.

- Implemented the five selected items. Publication selection validates every manifest archive and its embedded ID/version, dependencies, metadata and safe paths before emitting any filename; the shell pushes those exact paths. Ordinary directory validation still rejects unrelated archives, while publication selection leaves them untouched. Existing absence, exact-source and post-publication gates remain in order.
- All 73 tooling tests passed, including 14 new archive/publisher tests. Shell syntax, actionlint and whitespace checks passed. The six existing local archives passed the new metadata/path reader (their historical dependency inventories are not evidence for the current release). No .NET runtime code changed. The independent blind review is in progress.

- Blind review returned seven concrete findings; all received bounded corrections. Archive reading now checks ZIP CRCs and rejects duplicate normalized asset paths, wrong package roots, repeated metadata containers, and directory entries presented as README or DLL files. Publication versions follow SemVer numeric/prerelease rules. The packaged README links to the contributor guide using its repository URL. Publisher fixtures now fail the third push after two successes and prove subsequent pushes and presence checks stop.
- Final validation: all 78 tooling tests passed after review corrections; `bash -n scripts/publish-release-packages.sh`, `actionlint -no-color` and `git diff --check` passed. Six existing archives passed the metadata, safe-path and ZIP-integrity reader. Exactly five ledger items were closed. No review items were deferred.

## Review Triage Log

- R1 — medium / patch: A README.md/ directory with payload bytes normalized to README.md and passed the new readme check. Require a file entry; an archive fixture reproduces the rejection. Also reject directory entries as DLL inventory members.
- R2 — medium / patch: The new publication preflight could accept a DLL member with corrupt CRC because only the nuspec/readme were read. Check ZIP member integrity and exercise a deliberately altered assembly byte through both the validator CLI and publisher preflight.
- R3 — medium / patch: The normalized DLL set hid conflicting duplicate entries, leaving the new publication validation ambiguous. Reject duplicate normalized paths case-insensitively; fixtures cover exact duplicates, current-directory aliases and case aliases.
- R4 — low / patch: Metadata field counts allowed an empty metadata container followed by the populated one. Require a package root with exactly one metadata container, with malformed-root/container regressions.
- R5 — low / patch: The new publication version parser admitted leading-zero numeric components and malformed prerelease identifiers. Apply strict SemVer syntax and cover both invalid values and valid prerelease/build identifiers through the real CLI.
- R6 — low / patch: The README is packaged without CONTRIBUTING.md, so its relative guide link failed for package readers. Use the repository URL, which works both in the checkout and packaged README.
- R7 — low / patch: The new sequential publisher test stopped only on its first push. Add a third-push failure after two successes and assert that later packages and the presence verifier are skipped. Existing nontransactional partial-publication recovery remains documented and outside these five items.
