---
title: 'Resolve original draft releases through the complete release inventory'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: 'b088be0c7d6c759c146e94d967d56c98f19fc8fc'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="user-authorized unattended CI/CD repair and release">

## Intent

Release 40634214812 stopped before NuGet publication because the REST release-by-tag endpoint does not return its unpublished draft. Resolve the unique exact tag from a complete authenticated, paginated release inventory, validate the response shape, and pass the original record through every existing draft/source/tag/archive/provenance gate. Preserve immutable original archives, refusal of ambiguous or malformed responses, verify-only and staged-only behavior, secret redaction, and all publication preflights. No retagging, repacking, deletion, or publication dispatch belongs to this change.

</frozen-after-approval>

## Implementation Notes

- Actual evidence: `/releases/tags/v1.2.1` returns 404 while the authenticated release inventory contains draft 426274664 with the original six packages and provenance at source b088be0c7d6c759c146e94d967d56c98f19fc8fc.
- Scope: recovery locator, existing recovery regressions, and this frozen record. The parent owns committing/pushing and subsequent exact-source release/version selection.
- Qualification: reproduce the published-only tag endpoint behavior, exercise pagination and missing/duplicate/malformed inventory refusal, retain existing recovery fault controls, then run the real staged-only verifier against the retained original draft before the source commit advances.
- Reproduction: the existing recovery subprocess test failed against a fixture whose release-by-tag endpoint returns no unpublished draft, matching the real release failure.
- Implementation: the locator reads every `gh api --paginate` JSON array page, validates release identities, and requires one exact tag across the complete stream. Existing source/tag/asset/digest/provenance/publication gates remain. Malformed asset collections and names are refused before download.
- Focused verification: all 13 recovery tests pass, covering later-page drafts, duplicate/missing records, malformed pages/identities/assets, published-record refusal, trailing malformed JSON, read-only modes, original archive checks, partial remote inventories and redacted command failures.
- Live verification: while main remained b088be0c7d6c759c146e94d967d56c98f19fc8fc, the actual staged-only command successfully found v1.2.1 and verified its retained original assets against `/tmp/agents-published-originals-1.2.1`. Publication credentials were absent; no tag, release, package or original archive changed.
- Parent owns the fix commit, exact-source CI and later semantic release version. The existing v1.2.1 draft/tag must remain bound to its original source; this new fix belongs to the next planned version.

## Review Triage Log

- Accepted: independent parent review confirms complete pagination, strict exact-tag uniqueness/types, and unchanged source/tag/provenance safeguards.
- Patched: the first live check exposed an older supported GitHub CLI without `--slurp`. Replaced that flag with standard-library decoding of the complete concatenated JSON stream from supported `--paginate`; the subprocess fixture rejects unsupported flags, and regressions reject malformed trailing data and non-JSON whitespace.
- Accepted: parent independently reviewed the final decoder and its absolute offsets, page-array validation and complete-stream parsing. The live staged-only proof and all 13 focused tests then passed; whitespace checking also passed.
