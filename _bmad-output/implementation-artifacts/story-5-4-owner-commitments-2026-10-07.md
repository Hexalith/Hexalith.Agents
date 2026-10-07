---
title: 'Story 5.4 owner commitments'
created: '2026-10-07'
status: 'awaiting-delivery-fields'
owner: 'Project owner (user declaration)'
source_instruction: 'I owner do commitments'
complete_commitment: false
production_qualification: false
---

# Story 5.4 owner commitments — 2026-10-07

The user declared project ownership and instructed: “I owner do commitments”. This records the accountable owner and authorizes delivery of the four complete Story 5.4 contracts under the existing approved full scope. Use the user as the project owner acting in each named repository-maintainer role. The existing plan assigns secret-custody delivery to Hexalith.Platform, so that repository selection is now explicit.

The authoritative behavior, evidence levels and consuming stories remain the complete [external dependency register](../planning-artifacts/external-dependency-register.md). `owner-commitment-2026-10-07/contract-basis.json` hashes those unchanged contract fields against the pre-change register snapshot. Branch B preserves the Organization-typed provisioned Party verified by immutable tenant/id. The previously accepted history policy remains 365 fixed days from binding-effective-at with exclusive expiry.

| Record | Accountable owner / delivery repository | Immutable complete target | Delivery date | Full verification entry point proposed from existing owner scripts |
| --- | --- | --- | --- | --- |
| EXT-PARTIES-1 | Project owner acting as Parties Maintainer; Hexalith.Parties | TBD | TBD | From the accepted Parties target: `pwsh -NoProfile -File ./eng/verify-ext-parties-1.ps1 -Mode Live` |
| EXT-CONV-AI-1 | Project owner acting as Conversations Maintainer; Hexalith.Conversations | TBD | TBD | From the accepted Conversations target: `pwsh -NoProfile -File ./eng/verify-ext-conv-ai-1.ps1 -Mode Live` |
| EXT-SECRETS-1 | Project owner acting as Platform Maintainer; Hexalith.Platform | TBD | TBD | From the accepted Platform target: `pwsh -NoProfile -File ./eng/verify-ext-secrets-1.ps1 -Mode Live` |
| EXT-HOST-1 | Project owner acting as Platform Maintainer; Hexalith.Platform | TBD | TBD | From the accepted Platform target: `bash ./eng/verify-agents-host.sh --mode Full` |

These are proposed future complete verification entry points, not accepted passing compatibility commands. All four scripts exist, but their Live/Full branches currently refuse because complete behavior/providers/qualification are absent. Local, LocalScaffold, LiveReadiness or synthetic passes cannot replace the complete commands. The complete target must contain the full executable required matrix; accepting today's partial source commit would not deliver it. The immutable target may still be in delivery when a complete commitment is accepted; availability requires installation and a passing exact-target command with live Level 4 evidence.

## Fields needed to finish the register commitments

Supply one immutable version or full commit covering each complete contract and an integration date for each (a common date is acceptable if intended). For Platform host composition the owner handoff requests a full commit. Accept the final executable complete command for each target, including its reproducible prerequisite/credential/fixture setup and evidence-output contract. Do not put credential values in this document.

A compact reply can provide:

```text
EXT-PARTIES-1: target=<immutable version/full commit>; date=<YYYY-MM-DD>; command=<complete command>
EXT-CONV-AI-1: target=<immutable version/full commit>; date=<YYYY-MM-DD>; command=<complete command>
EXT-SECRETS-1: target=<immutable version/full commit>; date=<YYYY-MM-DD>; command=<complete command>
EXT-HOST-1: target=<full commit>; date=<YYYY-MM-DD>; command=<complete command>
```

Owner authority alone does not supply these values. Current checkout HEADs are inspected partial implementations and are not substituted for complete targets. Delivery dates are owner commitments, not dates inferred from this acceptance record.

## Remaining delivery work

- Parties: complete production custody, source/actor freshness and retained-history expiry/destruction/restore (including authenticated successor proof after expired predecessor destruction), packaged consumer compatibility and live P-01–P-10. The historical strict json-redacted replay blocker is superseded by the current complete passing Local source matrix linked below.
- Conversations: complete authenticated source/compare-append and catalogue/authority, all six seams, automatic approved-deletion publication discovery/backfill/pump, independent authenticated receiver/acknowledgement lookup and persisted failure/restart/race qualification.
- Secrets: qualified production custody; numeric trusted-envelope profile; independently governed decision-verification trust; full S1–S4/v23, issuer replay/credential restrictions, outcome lookup and live qualification.
- Host: complete H1–H4/v23 composition, replicated denial spool/recovery worker and private exact-target credentials; production protection binding/attestation and persisted migration/destruction/compromise/restore qualification.

This owner declaration does not choose unresolved independent Product/Governance/Security policies, manufacture approval evidence or deploy providers. It authorizes delivery under the already approved scope. Four records remain Uncommitted while target/date/complete-command fields are unknown. Story 5.4 stays draft/backlog. Once all nine fields per record are accepted, mark Committed; mark Available only after the complete installed target passes its accepted command. No owner issue comment, Git mutation, deployment or live seam invocation accompanies this record.


## Current Parties source delivery observation — 2026-10-07

[Parties replay/lifecycle owner packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-replay-and-lifecycle-2026-10-07.md) records the complete existing Local matrix passing 784 tests and fresh Platform custody verification passing 115 more, with zero-warning/error Debug builds. Original destroyed-profile replay already passed on the initial current source; new source boundary checks and 27 regression/qualification cases now pass. This clears that local replay blocker while preserving strict metadata and source completeness. Evidence includes initial failed attempts, exact commands, canonical HEAD/worktree observations and source/artifact hashes.

Production providers, all-copy irreversible receipts, restore qualification, fresh actor authority and complete P-01–P-10 live delivery remain open. Expired predecessor destruction still prevents positive successor proof under the current certificate/custody contract; executed source cases return unavailable safely. No immutable complete target, date or full live command was substituted from these local results or concurrent external commits. All commitment/status fields above and the external register are unchanged by this delivery observation.

## Reviewed Parties verification follow-through

[Final reviewed evidence](../../../parties/_bmad-output/implementation-artifacts/tests/owner-continuation-2026-10-07/review-final/README.md) records 802 fresh required Local passes and the unchanged 115-test custody evidence, independently reviewed and hashed. All 14 review findings were corrected. The original replay failure is superseded as a current Local source blocker; strict retained-history and required-snapshot validation remain enforced. The original 784-test packet remains a pre-review snapshot.

The separate 13-case HTTP class is still blocked by the current SDK startup audit before requests. Complete delivery must adopt the SDK sidecar/workload security contract in the Parties host/authenticated fixture and execute that class, in addition to the actor-free expired-transition continuation contract, qualified custody, irreversible all-copy destruction and nonrollback restore. Serialized full-request source coverage passes but cannot replace HTTP or live acceptance. Target/date/complete-command fields and record statuses remain unchanged.

## Current Parties HTTP/source follow-through — 2026-10-07

The historical HTTP startup blocker above is superseded by [fresh final evidence](../../../parties/_bmad-output/implementation-artifacts/tests/http-qualification-2026-10-07/review-final/README.md). The host adopts the existing SDK sidecar/workload contract without changing its audit or authorization checks. All original 13 HTTP cases and 35 security cases pass in a fresh **859-test complete Local matrix**. Fresh health/middleware, unchanged architecture, shared replay and full Platform custody checks bring current verification to **1,027 distinct passes**, with zero-warning/error Debug builds. Three independent reviewers completed; eight findings were corrected and one low finding was rejected with its reason and evidence limit. [Root verification/review](tests/parties-http-qualification-2026-10-07/README.md) independently checks exact names/classes, source/artifact hashes and unchanged story/register/sprint/policy gates, retaining failed concurrent-source attempts separately.

Nine new serialized-restore/lifecycle cases advance synthetic source qualification, including independent lifecycle changes over fixed restored evidence. The accepted 365-day effective-at policy and strict replay/retained-history validation remain unchanged. [Complete-target requirements](tests/parties-http-qualification-2026-10-07/qualification-status.md) still require authenticated actor-free successor continuation, installed qualified production custody, durable irreversible all-copy/lost-ack receipts, nonrollback restore, complete P-01–P-10 live evidence and exact immutable target/date/full-command fields. Fixture backend health and custody remain synthetic; no production health/provider or positive persisted actor/pubsub qualification is inferred.

Canonical revisions and worktree hashes are source observations, not complete target commitments. Every frontmatter/commitment field above and the external register remains unchanged; original Story 5.4 stays draft/backlog and the full parent remains in progress. This workflow preserved unrelated edits and performed no staging, commit, push, submodule update, deployment or owner message.

Later unowned shared replay edits occurred after the successful stable run/root audit. A separate fresh attempt passed all tests but failed source consistency during further edits. The stable 859/1,027 packet remains the verified snapshot, with reviewed owner source/SDK security/gates/artifacts still matching; [later-source limits](tests/parties-http-qualification-2026-10-07/root-post-closure-check.json) are explicit and no whole-current-worktree qualification is claimed.


## Reviewed Parties query deadline follow-through — 2026-10-07

[Fresh root evidence](tests/parties-query-deadline-2026-10-07/README.md) and [owner packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-query-deadline-2026-10-07.md) record the selected B06 source prerequisite: one bounded entry-to-verdict budget for current/history source and custody reads, independent wait/provider cancellation, original caller-token propagation, safe timeout/invalid-bound results and near-boundary success. All three independent reviews completed; seven findings were corrected and two pre-existing runtime qualification requirements remain explicitly deferred. Root's fresh complete owner Local matrix passes **919 distinct cases**, including 141 query regressions, six operational/policy cases, all thirteen original HTTP names and thirty-five security cases, with warning-free Debug builds and exact source/artifact evidence.

The retained policy remains 365 fixed days from binding-effective-at with exclusive expiry; Branch B and previous approvals are preserved. Initial 893-pass evidence and later concurrent changes remain separate; final fresh source consistency passed. The package-mode Aspire baseline still fails on the SDK sidecar extension mismatch. Complete owner target/date/command fields, installed custody/current authority, actor-free successor continuation, irreversible all-copy/lost-ack receipts, nonrollback restore and full live P-01–P-10 remain unresolved. Source deadlines do not forcibly terminate arbitrary synchronous authority work or blocked provider workers.

Every commitment/frontmatter field and external record status above is unchanged. No source checkout revision was promoted to a complete target, no integration date or acceptance was inferred, and original Story 5.4 remains draft/backlog while its prerequisite parent remains in-progress. No staging, commit, push, submodule update, deployment, live seam invocation or owner message accompanied this observation.
