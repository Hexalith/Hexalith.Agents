---
title: 'Reconcile five deferred verification and tooling entries'
type: 'chore'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open deferred entries still describe missing release configuration, audit severity, CI evidence, packaged identity transport and Party query deadlines that subsequent changes have supplied. Their stale state obscures the remaining qualification work, and the maintained root identity command does not yet require the owner deadline regressions.

**Approach:** Complete the requested five-entry batch by verifying those existing fixes, adding the owner deadline cases to the maintained identity verifier with fail-closed subprocess coverage, and recording precise closure evidence. Preserve each original deferral and all separate live qualification, source/package alignment, authority freshness and resource reclamation requirements. Review, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Baseline: clean main at `e8294427aedeb1d9a917a4a8eaa4222d430b49e3`, obtained directly from Git. Freeform ledger reconciliation; no sprint story key applies. The five entries form one evidence reconciliation goal. Existing policies settle all choices; changes are small and reversible, with no new public API, deployment, live data mutation or package publication. Main push is explicitly authorized.
- Code map: `scripts/verify-parties-identity.py` selects exact owner method inventories, validates fresh native XML and writes a success-only local receipt. Reuse those guards for existing Parties current/history deadline methods: noncooperative async and synchronous providers, cumulative source/custody budget, elapsed time before delayed callbacks, invalid timeout, setup budget, positive pre-expiry control, final authority expiry, caller cancellation race and late callback fault. Keep owner sources and submodule pins unchanged.
- `tests/tooling/parties_identity_verification_test.py` supplies independent controlled native results. Extend its expected selections and exercise incomplete, skipped and substituted deadline scenarios after earlier methods have succeeded, requiring absent success receipts and no later stages. Update `CONTRIBUTING.md` to describe the additional local evidence.
- Reconcile exactly the five entries titled shared semantic-release configuration, high/critical audit decision, broad Story 5.2 CI/release evidence, packaged SDK transport B01, and Party query deadline B06. Existing already-closed references to planner/audit work remain historical evidence; this batch claims five ledger closures, not five newly implemented runtime features. Preserve original ledger bytes except append-only closure fields.
- Evidence: run the maintained identity command with fresh artifacts, complete tooling tests, fresh Debug/source Server package-inventory and build-conformance selections, workflow lint and whitespace checks. Inspect Client 3.126.0 with dotnet-inspect and compile an isolated package-only transport API probe; record exact resolved assets and API limitations. The dated reconciliation record retains canonical source revisions, actual commands, result counts and durable evidence hashes. A context-free blind review must be triaged before commit. Validate the Conventional Commit message, fetch and inspect divergence, then push main.


- Initial verification passed: 62 exact owner cases (including 38 deadline rows), 14 receipt cases, 26 Server build/package conformance cases, 31 harness tests and the isolated package-only API probe. Workflow lint, JavaScript syntax, exact five ledger insertions with every original byte retained, and new documentation links/anchors passed. The full tooling suite continues separately.
- Blind review supplied seven concrete improvements. The planner now fails before producing a plan when top-level analysis/shareable configuration, unreviewed plugins or exec analysis hooks would otherwise be omitted; ordinary publication hooks remain excluded and documented plugin-tuple analysis stays supported. Ten real scratch-repository tests cover supported decisions and unsupported configuration refusals. This bounded correction belongs to the selected shared-configuration entry and adds two existing tooling files to the footprint.
- Expanded the maintained identity inventory with 12 blocking-callback and four cancellation-after-deadline/fault cases. The final selection is 78 owner rows, including 54 deadline rows across twelve methods, plus 14 receipt rows. Controlled negatives now independently omit, substitute or skip each of those twelve methods (36 executions). Final verification must use the expanded inventory before completion.
- The dated record now limits the checkout-credential claim to the original Agents policy/root caller finding. The source spec explicitly identified agents-policy, and historical CI confirms that omission; the shared CI checkout's default credential behavior is disclosed separately rather than claimed fixed.
- Evidence now requires embedded native case identities/outcomes and summaries, supplementing temporary log hashes. Added and executed a maintained documentation command reconstructing the isolated package probe against the current shared catalog, under the repository's copied global.json. Record actual SDK and inspector versions. The original /tmp Server fixture discovery failure remains documented alongside its successful repository-artifact rerun.

## Review Triage Log

- R1 — medium / patch: reproduced top-level analyzeCommits divergence. Reject unsupported analysis overrides/extends, exec analysis hooks and unreviewed plugins before planning, retaining configured analysis-plugin options and publication-hook isolation. Add refusal diagnostics/no-plan/source-ref regressions and document supported configuration.
- R2 — low / patch: the unqualified checkout claim included a shared CI checkout that retains default credentials. Cite the original agents-policy finding, narrow the statement to root callers and disclose the reusable workflow behavior. Original parent-owned work is complete; no claim of shared checkout hardening is made.
- R3 — medium / patch: callback-fault rows do not establish blocking-callback responsiveness. Require the existing twelve current/history/source/custody/timer/elapsed/caller blocking-callback rows and negative native-result checks.
- R4 — medium / patch: caller-first cancellation rows do not establish cancellation after deadline/fault processing starts. Require the existing four opposite-order rows and their negative native-result checks.
- R5 — low / patch: temporary report hashes alone cannot preserve inspectable native results. Embed each native test's identity/outcome and assembly summary in the durable evidence JSON.
- R6 — low / patch: package probe reconstruction required manual extraction from JSON. Add a runnable documented reconstruction command using the current catalog and repository SDK selection, and execute it.
- R7 — low / patch: inspector and outside-root SDK provenance were missing. Record actual dotnet-inspect and effective .NET SDK versions; the reconstruction copies global.json to honor repository SDK selection.


Final verification: the initial full tooling suite passes 213 tests; post-review final focused suites pass all 31 harness and ten planner tests. Fresh actual owner runs pass 78 exact native cases and 14 receipt cases. Final Server conformance passes all 26 cases. Every native result is successful with zero errors, failures, skips or unrun cases. The package-only probe and its documented reconstruction both pass; the actual SDK is 10.0.402 and inspector is 0.26.0+d236a7a. Final machine-readable evidence embeds individual native outcomes, current source hashes and phased tooling results. No hosted run, deployment, publication or production qualification is claimed.

The independent reviewer rechecked every correction and confirmed no remaining concrete findings: all 17 owner native reports match required scenarios, all 13 current source hashes and 54 result hashes match, Server and receipt counts agree, and tool/reconstruction provenance is present. All seven findings are patched; nothing is newly deferred. Final workflow lint, JavaScript syntax, documentation links, five-closure byte preservation, append-only completed-story history, whitespace and exact Conventional Commit validation pass. Fetch reports zero divergence from origin/main. Commit and push main complete the authorized delivery.
