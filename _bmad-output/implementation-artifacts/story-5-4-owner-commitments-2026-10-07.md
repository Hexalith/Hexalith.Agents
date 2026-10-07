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

- Parties: complete production custody, source/actor freshness and retained-history expiry/destruction/restore, packaged consumer compatibility, P-01–P-10 and the unresolved strict json-redacted replay lane.
- Conversations: complete authenticated source/compare-append and catalogue/authority, all six seams, automatic approved-deletion publication discovery/backfill/pump, independent authenticated receiver/acknowledgement lookup and persisted failure/restart/race qualification.
- Secrets: qualified production custody; numeric trusted-envelope profile; independently governed decision-verification trust; full S1–S4/v23, issuer replay/credential restrictions, outcome lookup and live qualification.
- Host: complete H1–H4/v23 composition, replicated denial spool/recovery worker and private exact-target credentials; production protection binding/attestation and persisted migration/destruction/compromise/restore qualification.

This owner declaration does not choose unresolved independent Product/Governance/Security policies, manufacture approval evidence or deploy providers. It authorizes delivery under the already approved scope. Four records remain Uncommitted while target/date/complete-command fields are unknown. Story 5.4 stays draft/backlog. Once all nine fields per record are accepted, mark Committed; mark Available only after the complete installed target passes its accepted command. No owner issue comment, Git mutation, deployment or live seam invocation accompanies this record.
