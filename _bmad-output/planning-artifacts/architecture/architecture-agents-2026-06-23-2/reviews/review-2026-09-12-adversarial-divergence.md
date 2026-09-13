# Adversarial-Divergence Review — 2026-09-12 Architecture Update

## Verdict

**CHANGES REQUIRED.** The update closes several previously named UX gaps, but one critical recovery-gate ambiguity and seven high-severity cross-artifact divergences still let independently implemented units deadlock data-handling acceptance, disagree about the Agent Party type, or ship behavior contrary to the approved segregation-of-duties and accessibility contracts.

## Scope And Method

This pass reviewed the current:

- `ARCHITECTURE-SPINE.md`;
- PRD;
- UX `EXPERIENCE.md` and `DESIGN.md`;
- external-dependency register; and
- launch-readiness register.

For each finding, the test was: can Team A and Team B implement independently, each cite current normative text, and still produce units that cannot interoperate or that disagree on a governed outcome? Security, tenant isolation, consent, audit integrity, and fail-closed recovery were treated as first-order concerns.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 7 |
| Medium | 2 |
| Low | 0 |

## Critical

### C1 — `DataHandlingAcceptance` can be gated by the condition it exists to clear

**Evidence**

- `launch-readiness-register.md:145` makes matrix version 3 the single contract consumed by API, BFF, UI, workflow, and the readiness projection.
- `launch-readiness-register.md:171` requires `LR-PROVIDER` for `DataHandlingAcceptance`.
- `launch-readiness-register.md:210-212` defines Provider readiness as blocked when the tenant has not accepted the current `DataHandlingVersion` and no grace applies.
- `ARCHITECTURE-SPINE.md:206` says the same condition produces `DataHandlingAcceptanceLapsed` until acceptance.

**Two-team divergence**

- Team A implements `LR-PROVIDER` as a target-aware gate and blocks `DataHandlingAcceptance` whenever the target Provider readiness result is `Blocked/DataHandlingAcceptanceLapsed`. The tenant can never perform the command that clears the blocker.
- Team B treats `LR-PROVIDER` as integration-evidence readiness only, allows the command while the target resource is acceptance-lapsed, and clears the condition.

Both readings are plausible from the current register, but one produces an unrecoverable consent deadlock. A failed recovery path in a consent gate is critical even though it fails closed.

**Actionable fix — autofix in the register and spine**

Define recovery-gate semantics explicitly. Preferred: remove target acceptance from the `LR-PROVIDER` evaluation used by `DataHandlingAcceptance`, while still requiring a fresh, complete target catalog record and all non-acceptance Provider controls. State that `DataHandlingAcceptanceLapsed` is the one blocker this command is authorized to clear and that every other Provider blocker remains fail-closed. Add a focused contract test proving an acceptance-lapsed model can be accepted but a stale, missing, incomplete, foreign-tenant, or superseded version cannot.

## High

### H1 — Operation-family version and lock ownership remain contradictory across Architecture, UX, and audit governance

**Evidence**

- `ARCHITECTURE-SPINE.md:168` classifies the public administrative retry as lock-bearing `ProposalResolution`, with only the accepted workflow steps declaring `ConversationPosting`.
- `ARCHITECTURE-SPINE.md:222` makes `DataHandlingAcceptance` lock-bearing and makes `ProviderInvocation`, `ConversationPosting`, and `SystemTimer` workflow-only and outside browser advisory locks.
- `launch-readiness-register.md:145,171` makes version 3 authoritative and adds `DataHandlingAcceptance`.
- `EXPERIENCE.md:275` still says `ConversationPosting` and the administrative-retry family are undecided and conservatively treats retry as lock-bearing `ConversationPosting`.
- `EXPERIENCE.md:283,579` still tells the UI to consume matrix version 2; `EXPERIENCE.md:306` still labels administrative retry an open Architecture question.
- `ARCHITECTURE-SPINE.md:292` and `EXPERIENCE.md:400` say all **nine** lock-bearing families require justification, but the new family makes ten; the explicit lists omit `DataHandlingAcceptance` even though `EXPERIENCE.md:283,307` says no confirmation family is exempt.

**Two-team divergence**

- The API/domain team declares matrix v3, locks `ProposalResolution`, and starts workflow-only `ConversationPosting` after acceptance.
- The UI team follows UX v2, locks `ConversationPosting`, emits an old matrix version, and does not know whether the new data-handling family belongs to the universal justification rule.

This yields unknown-version rejection, different accepted-by lock identities, double or missing advisory locks, and incompatible audit evidence.

**Actionable fix — autofix across source artifacts**

Update UX to matrix v3; replace the open-question paragraphs with the AD-5/AD-12 split; update AD-22 and UX from nine to ten (or stop using a count); include `DataHandlingAcceptance` in the explicit justification/audit roster; and make the UI confirmation row emit `ProposalResolution` for administrative retry. Add one contract fixture enumerating every v3 family and its browser-lock classification.

### H2 — The new `EXT-PARTIES-1` transition contradicts the spine's strict AI-type and immutable-identity rules

**Evidence**

- `ARCHITECTURE-SPINE.md:144` requires provisioning to obtain or create an AI-type Party.
- `ARCHITECTURE-SPINE.md:182` unconditionally requires the Parties seam to verify the linked Party remains AI-type.
- `ARCHITECTURE-SPINE.md:180` makes the Party link immutable and provides no live replacement/repair command.
- `prd.md:941` and `prd.md:1076` permit an Organization-typed provisioned identity, verified by id rather than type, until `EXT-PARTIES-1` lands, and say that identity is immutable.
- `external-dependency-register.md:93` repeats that transitional path, while `external-dependency-register.md:99` also names Stories 5.2, 5.4, and 6.6 as consumers of the still-`Uncommitted` dependency.
- `external-dependency-register.md:57` requires Conversations to verify AI type before membership, with no transitional identity branch.

**Two-team divergence**

- Team A follows the spine and blocks provisioning/membership until a true AI Party type exists.
- Team B follows the PRD/register, provisions an immutable Organization-typed identity and verifies it by id.

If `EXT-PARTIES-1` later becomes available, the immutable Team-B identities cannot satisfy Team-A's unconditional type check and cannot be replaced. Conversations integration also cannot know which rule applies.

**Actionable fix — discuss, then apply one coherent branch**

Choose one of two explicit V1 contracts. Strict branch: remove the Organization fallback and state that the consuming stories stay blocked. Transitional branch: add a durable `ProvisionedPartyIdentityKind/ContractVersion`, permit id-only verification for grandfathered identities, define whether new tenants switch at commitment or availability, and define a safe one-way migration or permanent grandfathering rule without replacing `PartyId`. Mirror the chosen branch in `EXT-CONV-AI-1` and its compatibility tests.

### H3 — UX UJ-3 still instructs the regeneration requester to approve the regenerated version

**Evidence**

- `ARCHITECTURE-SPINE.md:190-192` excludes the regeneration requester from approving that version and rejects regeneration before Provider work if no other Eligible Approver remains.
- `prd.md:66` now uses a second Eligible Approver after Anika requests regeneration.
- `prd.md:256-261` makes this the Product-approved five-moment predicate.
- `EXPERIENCE.md:783-785` still has Anika request regeneration and then approve the regenerated version herself; `EXPERIENCE.md:791` mentions a second Approver only for editing.

**Two-team divergence**

- The proposal API correctly returns `CanCurrentUserApproveSelectedVersion = false` for Anika.
- The UX journey and automation expect her Approve control to be available and the proposal to move to `Approved`.

The integrated journey cannot pass. A weaker server implementation that follows the UX would violate the approved segregation-of-duties rule.

**Actionable fix — autofix UX**

Rewrite UJ-3 as the PRD's two-person flow, add the no-other-Approver regeneration failure path, and state that the requester's control is absent or `aria-disabled` with the safe server verdict after regeneration.

### H4 — The Conversation dialog and persistent region still have two incompatible live-region ownership contracts

**Evidence**

- `ARCHITECTURE-SPINE.md:350` says `ConversationAgentCallPanel` owns no live-region node at all and routes validation, submission, and every later announcement through the contributed persistent region.
- `external-dependency-register.md:75,78` repeats that the persistent region is the sole live-region owner.
- `EXPERIENCE.md:652` says the panel announces while open, suppresses “its own pair” only in the Agents harness, and the persistent region handles later outcomes.
- `EXPERIENCE.md:656` says duplicate announcements corrupt `LiveRegionAnnouncedTick` evidence.

**Two-team divergence**

- The Conversations integration hosts only the persistent region, expecting the panel never to speak.
- The Agents UI component retains its own assertive/polite pair while open, following UX.

The result is either duplicate announcements and invalid NFR-14 samples or missing validation/submission announcements when one side suppresses the wrong carrier.

**Actionable fix — autofix UX and seam tests**

Delete the panel-owned-pair branch from UX. Define one persistent carrier for validation through terminal outcome in both production and the harness, and add DOM plus browser-timing tests proving exactly one mutation carrier per event before and after dialog close.

### H5 — `MirrorRefused` has no unambiguous current-operation lifecycle, especially for a refused re-admission mirror

**Evidence**

- `ARCHITECTURE-SPINE.md:182` applies `MirrorPending`/`MirrorRefused` semantics to both block-removal and re-admission-add mirrors.
- `ARCHITECTURE-SPINE.md:184` calls `MirrorRefused` terminal “for that mirror operation,” but models it as a flag and offers only “re-set the block” or “clear it” as remediation.
- `prd.md:648` and `ARCHITECTURE-SPINE.md:248` require status and metrics to expose/count it.
- `EXPERIENCE.md:498-499` describes the flag as the block mirror's outcome, not the re-admission mirror's, and does not say when a new operation clears or supersedes it.

**Two-team divergence**

- The aggregate team preserves `MirrorRefused = true` forever as historical evidence while starting a new `MirrorPending` operation.
- The projection/metrics team treats the flag as the unresolved outcome of the current mirror and clears it when remediation starts or succeeds.

They disagree on whether Pending and Refused may coexist, whether SM-C4 ever recovers, and what a `ReadmitPending` state does after a permanent add refusal. The current “exactly two remediations” also does not map cleanly when the refused operation was already the clear/re-admission mirror.

**Actionable fix — autofix the state contract**

Replace the free boolean interpretation with a current mirror record keyed by at least `BlockVersion`, direction (`Remove`/`Add`), operation id/sequence, and mutually exclusive outcome (`Pending`, `Confirmed`, `Refused`). Preserve prior outcomes in audit history. Define which command supersedes each refusal, when the public current flag clears, how a refused add returns to a remediable state, and that SM-C4 counts only an unresolved current refusal.

### H6 — The 30-day data-handling grace can be reset or misclassified by independently compliant implementations

**Evidence**

- `prd.md:203-207` defines field-level loosening, a 30-day tightening grace, and structural refusal of a false tightening declaration.
- `EXPERIENCE.md:323-330` expects a countdown, structural refusal, decline, and a recorded diff.
- `ARCHITECTURE-SPINE.md:148,206` records a declaration/diff and says “for 30 days,” but defines no stored `GraceExpiresAt`, comparison boundary, source instant, cumulative rule across multiple unaccepted versions, or required comparison against the tenant's last accepted version.
- `ARCHITECTURE-SPINE.md:230` allows the current and in-force versions to differ during grace but does not bind which grace when several unaccepted versions exist.
- `ARCHITECTURE-SPINE.md:326-330` (AD-28) requires durable deadlines to be derived once, but the data-handling grace is not tied to that rule.

**Two-team divergence**

- Team A anchors grace to the first unaccepted tightening version and compares every later version cumulatively against the tenant's last accepted record.
- Team B starts a fresh 30 days for each adjacent “tightening” publication and trusts the operator's declaration, allowing repeated publications to extend unaccepted processing indefinitely or a mixed loosening to inherit grace.

This is a consent and data-governance integrity failure, not just a countdown discrepancy.

**Actionable fix — autofix the architecture contract**

Store one exclusive `GraceExpiresAt` under AD-28, define its source instant and `EvaluatedAt >= GraceExpiresAt` boundary, and state whether later unaccepted versions may shorten but never extend it. Require the cumulative diff from the tenant's accepted version to the candidate current version to satisfy a field-level partial order; any unknown, mixed, or weaker field blocks immediately. Bind acceptance/decline to the named current version and reject a superseded submission.

### H7 — The PRD still contains a direct automatic-post retry contradiction

**Evidence**

- `ARCHITECTURE-SPINE.md:166` gives automatic posting records the same bounded automatic and administrative retry behavior as human-decided proposals.
- `prd.md:343-347` also says automatic posting uses the FR-18 retry bound.
- `prd.md:348` immediately says “Spine AD-5 states that automatic mode retries nothing; FR-11 governs.” That statement is no longer true of the current spine.
- `prd.md:463` says every retry, automatic or administrative, uses the bound.

**Two-team divergence**

- Team A implements automatic retries from the current spine and the main FR-11/FR-18 rules.
- Team B treats the explicit divergence note as an intentional override and disables automatic retry.

Call outcomes, cost reservations, posting-failure metrics, and recovery tests then disagree.

**Actionable fix — autofix PRD**

Delete the stale `prd.md:348` bullet and retire/update the dated divergence inventory in §8.1 so it cannot be mistaken for current normative behavior after architecture index version 6.

## Medium

### M1 — Queue disclosure is decided in Architecture but UX still requests an ordinal position

**Evidence**

- `ARCHITECTURE-SPINE.md:306` explicitly exposes `QueueId` and forbids an ordinal position in V1 for stability and tenant-privacy reasons.
- `EXPERIENCE.md:831-832` still requires “capacity queued with position where safe.”
- `EXPERIENCE.md:860` still describes the matter as unresolved and offers adding a position field or dropping the rendering.
- `launch-readiness-register.md:237` uses internal allocator order/position but does not authorize public disclosure.

**Two-team divergence**

- The capacity/API team returns only `QueueId` and state.
- The UI team waits for or derives an ordinal position, potentially inferring it from polling or allocator order and exposing cross-tenant activity.

**Actionable fix — autofix UX**

Replace the journey text with stable queued state plus a support-safe queue reference; remove the now-resolved Known-gap row. Add a contract test that no public queue DTO or accessible string contains ordinal position.

### M2 — The regeneration-requester exclusion is not bound to explicit durable version provenance and atomic concurrency behavior

**Evidence**

- `ARCHITECTURE-SPINE.md:190-192` makes the requester exclusion version-specific and requires a pre-Provider prospective check.
- `ARCHITECTURE-SPINE.md:166` makes proposal versions immutable, but does not name a durable `RegenerationRequesterPartyId` (or equivalent provenance fact) on a regenerated version.
- `ARCHITECTURE-SPINE.md:244` requires only a server-evaluated approve verdict on reads.
- `ARCHITECTURE-SPINE.md:228` relies generally on expected revision/idempotency but does not state the concurrent-regeneration winner/loser rule for the new prospective guard.

**Two-team divergence**

- Team A derives the excluded Party from the immutable regeneration command/event that created the selected version and re-evaluates the guard atomically at the aggregate revision.
- Team B derives it from mutable workflow/current-actor state or the latest resolution event and can lose the requester association after replay, a later regeneration, or a concurrent command.

**Actionable fix — autofix the version contract**

State that every regenerated `ProposalVersion` durably records the requesting human `PartyId` (or an immutable reference to the exact regeneration-request event); approval reads only that provenance. Require the prospective guard and version creation to share one aggregate expected revision, with one concurrent request winning and the loser returning a typed superseded outcome before any Provider call.

## Consolidated Two-Team Failure Scenarios

### Scenario A — Provider consent UI and domain service

1. The UI follows UX matrix v2 and submits a named data-handling acceptance under its locally known family set.
2. The API requires matrix v3 and `LR-PROVIDER`.
3. The Provider readiness join reports `DataHandlingAcceptanceLapsed` because acceptance has not happened.
4. Depending on the server team's interpretation, the command is either rejected forever or accepted under an operation/audit family the UI did not lock or justify consistently.

The two units are individually defensible from current documents and do not interoperate.

### Scenario B — Grandfathered Agent identity and Conversations membership

1. Provisioning follows the PRD and stores an immutable Organization-typed Party verified by id.
2. Parties/Conversations integration follows the spine and requires AI type.
3. Every call fails membership after the external seam becomes available, but the link cannot be replaced.

The system has a durable identity it can neither use nor repair.

### Scenario C — Regeneration, approval, and browser announcements

1. Anika follows UX and requests regeneration.
2. The server correctly excludes her from approval, so the journey's next primary action is unavailable.
3. While the rejection/status is rendered, both the panel and the persistent contributed region speak under the stale UX ownership model.

The product simultaneously fails its canonical journey and its exactly-one-announcement evidence rule.

## Gate Recommendation

Do not finalize this update as handoff-ready until C1 and H1-H7 are reconciled in the owning artifacts and the affected register/UX contract tests are named. M1 is a clear source-sync fix. M2 can be fixed directly in AD-5/AD-8/AD-13 without a Product decision because it operationalizes the already approved rule rather than changing it.

---

## Post-Fix Rerun — 2026-09-12

### Verdict

**CHANGES REQUIRED.** The prior critical deadlock is closed and the spine now fixes cumulative grace, atomic regeneration authorization, the versioned current-mirror model, queue disclosure, and the Conversation UI seam. Three high-severity downstream contradictions and one medium inventory mismatch remain: an independently built story can still choose the wrong immutable Party type, implement mirror outcomes as unsuperseded flags, or make administrative retry available at exactly the opposite time from the UI.

### Remaining Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 3 |
| Medium | 1 |
| Low | 0 |

### Prior-Finding Disposition

| Prior finding | Post-fix disposition | Current evidence |
| --- | --- | --- |
| C1 | Closed | `launch-readiness-register.md:171` no longer gates `DataHandlingAcceptance` through `LR-PROVIDER`; stale/superseded/version and tenant checks are bound by spine AD-10/AD-12 and Epic 5.3. |
| H1 | Closed at the authoritative matrix and owning stories; one medium residual below | Matrix v3 and `DataHandlingAcceptance` are aligned in the register (`:145,171`), spine (`:232-234,308`), UX (`:275,283,399,580`), and Epics (`:159,1505,1613,2981-2984`). |
| H2 | Open as PF-H1 | The architecture/register branch is coherent, but the executable Epics still mandate the old strict AI-type identity and omit the new dependency authority. |
| H3 | Closed | PRD UJ-3 (`:66`), UX UJ-3 (`:784-792`), spine AD-8 (`:196-200`), and Epic 7.3/7.4 now use a second Eligible Approver and durable requester provenance. |
| H4 | Closed | Spine AD-31 (`:364-368`), the dependency register (`:77-80`), UX (`:123-127,230,647-653`), and Epic 6.7 bind the persistent contributed region as sole speaker with focus return. |
| H5 | Open as PF-H2 | The spine and UX add `CurrentMirror`, but their preceding block-only summaries and the PRD/Epics implementation handoff still permit independent flags. |
| H6 | Closed | Spine AD-10/AD-28 (`:216-218,348`), UX (`:329`), and Epic 5.3 bind one cumulative, exclusive, non-extending grace and stale-version rejection. |
| H7 | Closed | PRD FR-11 now states the same bounded retry contract at `:343-348`; the old §8.1 divergence bullets are explicitly historical. |
| M1 | Closed | UX exposes `QueueId` and expressly forbids ordinal position (`:529,832`), matching spine AD-24 (`:322`). |
| M2 | Closed | Spine AD-8 (`:200`) atomically appends `RegenerationAuthorized`, durably binds `RegenerationRequestedByPartyId`, and rejects concurrent losers before Provider work; Epic 7.3/7.4 carries the matching tests. |

### High Findings

#### PF-H1 — The executable Epics still hard-code the pre-branch Party identity and omit `EXT-PARTIES-1`

**Evidence**

- The spine's corrective clauses make Branch A (AI type) and Branch B (Organization-typed, identity-by-id) mutually exclusive owner-approved launch contracts and prohibit replacement (`ARCHITECTURE-SPINE.md:150,190`).
- The dependency register defines both branches, makes branch selection mandatory, and allows Stories 5.2, 5.4, and 6.6 only transitional Branch-B-compatible development while withholding launch evidence (`external-dependency-register.md:89-103`).
- The active Epics requirements still require an immutable **AI-type** Party (`epics.md:38`), its dependency-authority list omits `EXT-PARTIES-1` (`epics.md:167`), and Story 5.2 again requires an immutable AI-type Party while declaring no external dependency (`epics.md:1287-1291,1315-1332`).

**Two-team divergence**

- Team A implements Story 5.2 literally, provisions only an AI-type Party, and either blocks development or invents that type without a committed Parties contract.
- Team B follows the register's permitted transitional path and persists the immutable Organization-typed identity verified by id.

Both can cite current handoff material, but their immutable identities cannot be reconciled by replacement. Team A can also create a security-sensitive Party type or authorization contract that the owning Parties system never accepted.

**Actionable fix — autofix Epics**

Add `EXT-PARTIES-1` to the dependency-authority inventory; replace strict AI-type language in FR1 and Story 5.2 with the selected-branch contract; and state on Stories 5.2, 5.4, and 6.6 that uncommitted Branch-B-compatible work is development-only and cannot supply launch evidence or `RQ-1`.

#### PF-H2 — `CurrentMirror` is not yet the one end-to-end mirror contract

**Evidence**

- Spine AD-7 correctly defines one versioned `CurrentMirror(BlockVersion, Direction, Outcome, AttemptId)`, mutually exclusive derived flags, supersession, late-outcome handling, both refused directions, and SM-C4 current-only counting (`ARCHITECTURE-SPINE.md:188`).
- Its immediately preceding summary still says every refusal leaves the authoritative block in force and offers block-only remediation (`ARCHITECTURE-SPINE.md:186`), which is false for a refused `Readmit` that line 188 says remains `ReadmitPending`.
- UX repeats the stale block-only definitions and remediation in its status rows before a later paragraph adds both directions (`EXPERIENCE.md:495-500`).
- The governing PRD still describes `MirrorPending`/`MirrorRefused` as flags and reports/counts them without current-record or supersession semantics (`prd.md:167-168,648,1033,1076`).
- Epic 6.6 requires only a `MirrorPending` flag and tests success/failure clearing, with no current record, permanent-refusal direction, superseded-history rule, or current-only metric (`epics.md:40,2101-2112,2139`).

**Two-team divergence**

- Team A stores the spine's single current record and audits superseded operations; a refused re-admission leaves `ReadmitPending`, and a successor attempt immediately removes the public refusal.
- Team B implements Epic 6.6/PRD flags independently, preserves `MirrorRefused` alongside a new `MirrorPending`, and treats all historical refusals as current SM-C4 failures with block-oriented remediation.

The projection and aggregate then disagree on whether two outcomes coexist, whether re-admission is blocked or pending, and whether an old refusal still signals an active operational incident.

**Actionable fix — autofix PRD, UX summaries, and Epic 6.6**

Replace the block-only summaries with direction-aware current-record wording; make `MirrorPending` and `MirrorRefused` derived and mutually exclusive everywhere; and add Epic 6.6 acceptance/negative tests for refused Remove, refused Readmit, successor supersession, late lower-version outcomes, and SM-C4 current-only counting.

#### PF-H3 — Administrative retry has an impossible availability window across PRD and UX

**Evidence**

- PRD FR-18 allows a Tenant Agent Administrator's retry **only after the automatic budget is exhausted** (`prd.md:457`).
- UX exposes administrative retry while retries remain and removes it when the three-attempt bound is exhausted (`EXPERIENCE.md:268-269`), while identifying it as the only user retry control (`EXPERIENCE.md:273-275`).
- Spine AD-5 and Epic 7.7 instead describe one administrative attempt consuming the common three-attempt/15-minute bound, with no post-exhaustion exception (`ARCHITECTURE-SPINE.md:168-170`; `epics.md:2626-2629`).

**Two-team divergence**

- The API team follows the PRD and rejects the administrator until automatic attempts are exhausted.
- The UI team shows the control only before exhaustion and removes it at the instant the API would first allow it.

No integrated administrative retry can succeed. A compensating client that races an automatic retry also risks two accepted recovery commands unless the aggregate revision is the sole arbiter.

**Actionable fix — discuss Product ownership, then synchronize**

Choose whether administrators may consume a remaining attempt from the common bound or receive a separately bounded post-automatic allowance; then make PRD FR-18, AD-5, UX, Epic 7.7, the server `CanAdministrativeRetry` verdict, and concurrency tests express the same window and winner/loser rule.

### Medium Finding

#### PF-M1 — Epics' condensed UX lock inventory remains a local six-family subset despite matrix v3 propagation

**Evidence**

- Epics correctly declares matrix v3 and propagates `DataHandlingAcceptance` to its owning and conformance stories (`epics.md:159,1363-1411,1503-1507,2981-2984`).
- Its still-active UX-DR31 nevertheless defines the advisory lock only across six named families, omitting current lock-bearing `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, and `DataHandlingAcceptance` (`epics.md:232`). UX-DR40 repeats the same local subset for restrictive viewports (`epics.md:250`).
- The spine and UX say the v3 lock-bearing set has ten families and that consumers may not maintain local subsets (`ARCHITECTURE-SPINE.md:232-234,308`; `EXPERIENCE.md:283,399,580`).

**Two-team divergence**

An Epic-level UI conformance team can test only the six locally named locks while a feature team implements the four additional locks, producing inconsistent pending behavior across surfaces even though both report matrix v3.

**Actionable fix — autofix Epics**

Replace both six-family lists with a citation to the authoritative matrix-v3 lock-bearing classification, or enumerate all ten from one generated/shared fixture and make the restrictive-viewport rule apply to every lock-bearing family.

### Post-Fix Gate Recommendation

Do not mark the update handoff-ready until PF-H1 through PF-H3 are reconciled. PF-M1 is a direct source-sync edit and should land in the same pass so the newly propagated matrix-v3 claim cannot coexist with a stale local lock subset.

---

## Final Reconciliation Rerun — 2026-09-12

### Verdict

**FAIL — CHANGES REQUIRED.** Three of the four post-fix findings are closed: Epics now carries the branch-selected `EXT-PARTIES-1` contract and dependencies; `CurrentMirror` is direction-aware and versioned across architecture, PRD, UX, and Stories 6.6/6.8; and the Epic lock inventory covers all ten matrix-v3 families. The retry design is aligned in its main transition row, architecture, UX, and Story 7.7, but two still-active PRD clauses separately authorize administrative retry after the shared attempt bound is exhausted. That residual contradiction permits incompatible authorization implementations and keeps the gate from passing.

### Remaining Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 0 |
| Low | 0 |

### Prior-Finding Disposition

| Post-fix finding | Final disposition | Current evidence |
| --- | --- | --- |
| PF-H1 | Closed | Epics FR1 and dependency authority are branch-neutral (`epics.md:38,167`); Stories 5.2 and 6.6 carry `EXT-PARTIES-1`, the selected-branch launch dependency, and the development-only Branch-B allowance (`epics.md:1290,1318,1329-1331,2094,2099,2143-2147`). |
| PF-H2 | Closed | The single direction-aware `CurrentMirror` and current-only refusal semantics are synchronized in the spine (`ARCHITECTURE-SPINE.md:186,190,262`), PRD (`prd.md:97,166-168,1068,1077`), UX (`EXPERIENCE.md:502,858`), and Stories 6.6/6.8 (`epics.md:368,2101-2102,2144,2242,2268`). |
| PF-H3 | Partially closed; residual FR-H1 below | The shared budget, server `CanAdministrativeRetry`, and expected-revision single-winner rule are aligned in the main PRD contract (`prd.md:457,470`), spine (`ARCHITECTURE-SPINE.md:172`), UX (`EXPERIENCE.md:277`), and Story 7.7 (`epics.md:2631-2639,2662-2665`), but stale PRD permissions remain. |
| PF-M1 | Closed | Epics applies the authoritative matrix-v3 set to every lock-bearing family and explicitly tests all ten (`epics.md:232,250,2999`). |

### High Finding

#### FR-H1 — Two PRD clauses still authorize administrative retry after the shared budget is exhausted

**Evidence**

- The normative transition row permits automatic or administrative retry only while one attempt remains in the shared three-attempt bound and expressly says there is no automatic sub-budget (`prd.md:457`).
- The server verdict repeats that `CanAdministrativeRetry` includes the remaining shared attempt count and that concurrent retry requests have one expected-revision winner (`prd.md:470`). Architecture, UX, and Story 7.7 state the same contract (`ARCHITECTURE-SPINE.md:172`; `EXPERIENCE.md:277`; `epics.md:2631-2639`).
- In direct conflict, the exhausted-budget recovery list says a proposal can exit because “an audited administrative retry succeeds” (`prd.md:463-466`).
- The FR-33 role matrix separately grants the Tenant Agent Administrator permission to retry “after its retry budget is exhausted” (`prd.md:514`).

**Two-team divergence**

- Team A implements the transition table and `CanAdministrativeRetry`: exhaustion makes every retry ineligible, and recovery is limited to lookup confirmation, abandon, removal/block handling, or expiry.
- Team B implements the exhausted-budget paragraph and FR-33 permission: an administrator receives an additional retry after the shared bound reaches zero.

Both implementations cite active PRD requirements. Their APIs disagree on authorization, their attempt ledgers disagree on whether the three-attempt bound is enforceable, and a client following the server-verdict contract cannot invoke the extra action that the role matrix promises. In this governed posting path, treating the stale permission as an override can bypass the stated safety and cost bound.

**Actionable fix — PRD source reconciliation**

Delete `prd.md:466` from the exhausted-budget exit list and change `prd.md:514` to authorize administrative retry only while the server's `CanAdministrativeRetry` verdict is true and one shared attempt remains. Keep the main transition row, server verdict, architecture, UX, and Story 7.7 unchanged.

### Fresh Divergence Result

No additional critical, high, or medium two-team divergence was found within the 2026-09-12 scope after rechecking regeneration-requester segregation, cumulative data-handling grace, branch-selected Parties identity, direction-aware mirror/status behavior, queue disclosure, `EXT-CONV-UI` ownership, the four UI artifacts, operation-family propagation, and all ten lock-bearing families. The sole remaining gate issue is FR-H1 above.

---

## Final Focused Closure Recheck — 2026-09-12

### Verdict

**PASS.** FR-H1 is closed. The exhausted-budget recovery list no longer offers administrative retry (`prd.md:463-468`), and the FR-33 permission now requires both the shared retry budget and time bounds to permit the action through the server `CanAdministrativeRetry` verdict (`prd.md:513`). This matches the PRD transition and concurrency contract (`prd.md:457,469`), architecture (`ARCHITECTURE-SPINE.md:172`), UX (`EXPERIENCE.md:277`), and Story 7.7 (`epics.md:2631-2639,2662-2665`). A focused search found no remaining post-exhaustion administrative-retry permission.

### Remaining Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |

### Final Finding Disposition

| Finding | Disposition | Evidence |
| --- | --- | --- |
| FR-H1 | Closed | `prd.md:463-469,513` now permits administrative retry only under the shared remaining-attempt and time-bound verdict and provides no retry exit after exhaustion. |

### Final Gate Recommendation

The adversarial-divergence reviewer gate passes for the 2026-09-12 architecture update. No remaining two-team incompatibility was found in the reviewed scope.

---

## Final Current-Tree Rerun — 2026-09-12

### Verdict

**FAIL — CHANGES REQUIRED.** The retry reconciliation remains closed, but the broader current-tree pass found one high and one medium data-handling divergence still present in active PRD/UX text.

### Remaining Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 1 |
| Low | 0 |

### High Finding

#### CT-H1 — Contractual-reference changes have two incompatible grace classifications

**Evidence**

- Architecture makes every contractual-reference change incomparable and requires fresh tenant acceptance; it can never receive tightening grace (`ARCHITECTURE-SPINE.md:218`).
- PRD and UX forbid only a **weaker** contractual reference from being declared tightening (`prd.md:206-207`; `EXPERIENCE.md:325-326`). Their wording permits a team to classify a perceived stronger reference as tightening and continue processing for 30 days without acceptance.
- Story 5.3 blocks only changes its implementation classifies as neutral or loosening and does not pin the contractual-reference field to the architecture's always-incomparable rule (`epics.md:1372-1374`).

**Two-team divergence**

- Team A follows AD-10 and blocks every contractual-reference change until the tenant accepts it.
- Team B follows PRD/UX and grants grace to a reference it classifies as stronger.

Both can cite current active text, but Team B processes tenant Conversation content under changed contractual terms that the tenant has not accepted while Team A refuses the same Provider/model. This is a sensitive data-governance integrity difference.

**Actionable fix**

Replace the PRD and UX “weaker contractual reference” classification with an explicit rule that **any** contractual-reference change is incomparable, requires fresh acceptance, and cannot receive grace; add that rule and a negative test to Story 5.3.

### Medium Finding

#### CT-M1 — The Agent configuration UI disables activation during a grace the authoritative contract allows

**Evidence**

- The `agent-config-form` names acceptance of the **current** `DataHandlingVersion` as an unconditional activation blocker (`EXPERIENCE.md:224`).
- The same UX permits continued use under the last accepted version during a valid tightening grace (`EXPERIENCE.md:325,331`).
- Architecture explicitly applies that grace exception to both activation and every Provider-dependent step (`ARCHITECTURE-SPINE.md:216,218`), while Story 5.3 says the tenant remains callable until the exclusive deadline (`epics.md:1372-1374`).

**Two-team divergence**

- The UI team follows the component contract and disables activation whenever current version differs from accepted version.
- The API/readiness team follows AD-10 and permits activation while a valid grace keeps the prior accepted version in force.

The server supports an action the primary UI cannot initiate, and activation/readiness parity tests receive opposite expected results.

**Actionable fix**

Qualify the `agent-config-form` blocker: current-version acceptance is required unless the server readiness result supplies a valid unexpired tightening grace and an `InForceDataHandlingVersion`; the UI must consume that verdict rather than infer it locally.

### Current-Tree Gate Recommendation

Reconcile CT-H1 and CT-M1 before handoff. All previously reported Party-branch, mirror, retry, queue, operation-family, regeneration, and Conversation-UI findings remain closed.

---

## Current-Tree Data-Handling Closure — 2026-09-12

### Verdict

**PASS.** CT-H1 and CT-M1 are closed, with no remaining adversarial divergence in the reviewed current-tree scope.

### Remaining Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |

### Final Finding Disposition

| Finding | Disposition | Evidence |
| --- | --- | --- |
| CT-H1 | Closed | PRD now requires fresh acceptance after a version change unless a valid declared-tightening grace remains, defines cumulative tightening, and makes any contractual-reference change incomparable, fresh-acceptance-only, and grace-ineligible (`prd.md:205,207`); UX states the same acceptance exception, classification, and structural refusal (`EXPERIENCE.md:320,326,329`), matching spine AD-10 (`ARCHITECTURE-SPINE.md:218`). |
| CT-M1 | Closed | The Agent configuration component now permits activation when either the current version is accepted or a valid declared-tightening grace is in force, rendering the latter as a warning (`EXPERIENCE.md:224`); the UX's main acceptance rule now says fresh acceptance is required after a version change unless that grace remains (`EXPERIENCE.md:320`), and its detailed rules consume the server-owned deadline and in-force version (`EXPERIENCE.md:325,331`), matching spine AD-10 (`ARCHITECTURE-SPINE.md:216,218,220`). |

### Terminal Gate Recommendation

The adversarial-divergence reviewer gate passes for the current 2026-09-12 tree. All findings raised by this reviewer are closed.
