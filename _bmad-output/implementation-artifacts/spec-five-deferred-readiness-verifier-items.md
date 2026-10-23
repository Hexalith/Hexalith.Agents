---
title: 'Complete five deferred readiness verifier items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred readiness verifier entries remain open even though the pinned Parties dependency contains the corrections. Their dated regression harness targets a sibling checkout and cannot exercise the current wrapper's preliminary version-resolution step, leaving no maintained root tooling gate for these fixes.

**Approach:** Complete the requested five-item batch by adding portable subprocess regression tests against the actual pinned verifier scripts, verifying immutable expiry, transition reconstruction, bounded streaming, caller-relative paths and nested binding scope. Reconcile exactly those five unique ledger entries using fresh results, independently review, commit and push main as requested. Synthetic fixtures establish verifier behavior only; dependency acceptance and live qualification remain governed by the existing registers.

</frozen-after-approval>

## Implementation Notes

- Route facts: no unresolved intent choices, new public APIs, migrations, deployments or data mutations. Only root regression tooling and completion evidence change. The user explicitly authorizes a five-item batch and push to main. No sprint story key applies. No owned AppHost exists for this tooling change.
- Clean main baseline: `3e2ddf14542b2d0d7bb90f5749b6a8feb9cd0d57`. Recorded Parties gitlink: `4fc1124eb995a40553cb23b042d4a3c1add3dfb2`. Both obtained directly from Git. Preserve submodule content and pointers.
- Exactly five unique selections from `deferred-work.md`: B08/E05/V04 readiness exact immutable expiry; B09 readiness transition proof; B10 readiness streaming bound; B11/E03 relative evidence paths; E04 readiness nested binding scope. V03 receipt coverage is a supporting negative control, not a sixth claimed item.
- Existing implementation: `references/Hexalith.Parties/eng/verify-ext-parties-history-readiness.ps1` independently folds establish/revoke/rebind events, compares returned scope/interval/custody, checks exact 365-day retention, and reads HTTP bodies with bounded streaming. `verify-ext-parties-1.ps1` resolves caller-relative paths before changing location. The completed `spec-5-4-identity-completion-and-readiness-fixes.md` records historical fixes; no source repair or dependency update is needed.
- Add maintained `tests/tooling/parties_readiness_verifier_test.py` and a focused fixture helper if needed. Use repository-relative source discovery, temporary directories and isolated loopback servers. Execute the production PowerShell scripts; do not import or rewrite dated evidence, alter the real register or use real credentials/endpoints. Clear inherited EXT_PARTIES inputs. Preserve positive revoked/rebound/tick controls and separate mutated expiry/scope/interval negatives with specific diagnostics, request counts and no-success-receipt assertions. Test declared/chunked wire overflow, decoded overflow, receipt denial before HTTP and redirect refusal without destination requests.
- Path tests must exercise the actual wrapper from a foreign caller directory, including PowerShell Set-Location. Fake only dotnet: return a synthetic valid version for the wrapper's preliminary msbuild query, then intentionally fail the first build after capturing arguments and directory. Assert absolute evidence/artifact/Memories/EventStore/Platform paths and no real owner build. Handle the installed PowerShell runtime's dotnet launcher as existing tooling fixtures do.
- Validation: focused maintained suite and complete `python3 -m unittest discover -s tests/tooling -p '*_test.py'`; PowerShell syntax and CRLF-aware whitespace checks. Record actual executed case totals and any skips. Review through the workflow's independent context-free blind reviewer, triage every finding, apply necessary corrections and rerun affected checks. Close ledger entries only after passing verification. Validate the exact Conventional Commit message using pinned commitlint, fetch and inspect divergence, commit and push main.

- Maintained suite implemented in two root tooling files and documented in CONTRIBUTING. The existing CI discovery command includes it automatically. Focused run: 27 tests passed in 123.469 seconds, zero skips. PowerShell parser accepts both pinned scripts. Exact diagnostics and HTTP stages accompany every refusal; positive receipts retain ReadinessOnly and incomplete qualification obligations.
- Independent mutation verification used private temporary copies of both scripts and a stub owner tree. All five selected tests passed against unchanged copies, then failed by assertion after separately disabling original custody expiry, closing-interval comparison, the 32 MiB bound, evidence normalization or returned nested scope. No source checkout was mutated.
- Path scope precision: the first controlled build directly observes evidence/artifact/Memories/EventStore normalization and stops before any Platform lane. PlatformRoot is supplied but later Platform execution is not claimed. This suffices for selected B11/E03 evidence/Memories deferral.
- Exactly five unique ledger entries now reconcile the already shipped owner corrections with fresh maintained tests. Supporting receipt/redirect/Live gates are controls, not additional claimed completions. Complete tooling and independent review are in progress.

- Blind review returned six concrete regression-coverage gaps. All are simple test corrections within this slice; none requires a production or contract change. Added nine cases and strengthened complete historical-query assertions. Final validation is running on the expanded suite.

## Review Triage Log

- R1 — medium / patch: 40 MiB-only overflow cases missed a relaxed 39 MiB limit. Declared overflow now advertises 32 MiB + 1 byte; a separate chunked response crosses by one byte, a valid exactly-32-MiB response succeeds, and the 40 MiB interrupted-send control remains.
- R2 — medium / patch: one oversized event did not distinguish per-event from cumulative decoded-byte limits. Added two otherwise valid padded JSON events of 8 MiB + 1 byte each; the complete wire response remains below 32 MiB and must fail the aggregate 16 MiB guard.
- R3 — medium / patch: matching seven-digit timestamps did not prove refusal of precision loss. Added returned custody expiry differing from its source by one 100 ns tick, requiring the exact original-expiry diagnostic.
- R4 — medium / patch: closing-interval cases did not pin exact opening time. Added a returned validFrom shifted earlier by one microsecond while still containing actionAt, requiring the reconstructed-interval diagnostic.
- R5 — medium / patch: unrelated scope values did not detect case-insensitive comparisons. Added independent case-only tenant/Party changes for both source opening and returned nested evidence; all four must fail at their own scope guard.
- R6 — medium / patch: predetermined replies could hide a malformed outbound historical action instant. Successful controls now assert the full historical request and compare actionAt as an exact instant preserving all seven fractional digits, including the ticks control.

- R7 — medium / patch, review recheck: Python 3.10 rejects seven-digit datetime.fromisoformat fractions, so the new exact-instant request assertion would fail on the documented minimum interpreter. Confirmed with installed Python 3.10; parse the whole-second instant separately and preserve seven fraction digits as integer ticks. Direct 3.10 checks establish offset equivalence, one-tick inequality and zero/absent fraction equivalence. Running the five actual positive script controls on 3.10 additionally verifies use by the harness.


- Final validation: complete tooling command `python3 -m unittest discover -s tests/tooling -p '*_test.py'` passed 142 tests in 503.007 seconds with zero skips before the review additions. The expanded readiness-only command then passed all 36 cases in 177.455 seconds with zero skips. Together these verify all 115 unchanged tooling tests and all 36 current readiness cases; no claim is made that the earlier discovery run contained the later nine cases.
- Python 3.10 compatibility validation passed all five real positive script controls in 29.898 seconds, including seven-digit ticks and exactly 32 MiB. Direct helper probes also preserve timezone equivalence, one-tick inequality and zero/absent fractions. The reviewer rechecked every correction and reported no remaining concrete issue. All seven findings are patched; no review item is deferred.
- Both owner script hashes remain identical to baseline: history verifier SHA-256 `77682dfd9a522b8e3447f4fed995f25ade47781398236966586f7654aaff7893`; wrapper SHA-256 `63e8d7974b267f8baa8a8d14ae1e50503836de5aee21a589240dc55e36277bb1`. The new suite uses root-relative discovery and temporary fixtures; no submodule pointer/content, actual dependency register or production qualification state changed.
- The exact Conventional Commit message passed repository-pinned commitlint. Final root whitespace and fetched divergence checks precede commit/push. The task explicitly authorizes publishing the resulting commit to main.
