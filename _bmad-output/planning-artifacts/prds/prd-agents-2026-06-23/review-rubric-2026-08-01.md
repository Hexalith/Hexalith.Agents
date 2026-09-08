# PRD Quality Review — Hexalith Agents

## Overall verdict

The PRD is a strong, coherent basis for UX, architecture, and epic decomposition: its V1 boundary, governance model, journeys, stable requirements, and fail-closed posture are unusually explicit. It is not yet a deterministic release-decision contract, however, because `RQ-1` depends on underspecified metric calculations and evidence classifications, while the promised audit trail does not require the exact input and configuration snapshot needed to prove how a response was produced.

## Decision-readiness — adequate

The major product choices are genuinely decided rather than hidden: §1 names the bet, §5–§6 make omissions explicit, and §13 records binding choices with owners and resolution dates. A decision-maker can authorize downstream design work, but cannot yet apply the final READY / NOT READY gate consistently.

### Findings

- **high** The release gate lacks a reproducible measurement contract (§7 NFR-9 and NFR-14; §11; §12 SM-2 and SM-3) — The PRD specifies thresholds such as “at least 30 production-like executions” and a “rolling 30-day window,” but does not define the percentile estimator, measurement window, event/timestamp authorities, late-event treatment, the boundaries of “eligible Conversations” and “accepted Agent Calls,” or how 26-hour proposal maturation is censored at the end of a rolling window. Two evaluators could derive opposite `RQ-1` decisions from the same events. *Fix:* Add a normative metric dictionary for every launch gate: numerator, denominator, cohort inclusion/exclusion, authoritative events and timestamps, percentile algorithm, observation window, late-data policy, minimum-sample rule, and `InsufficientEvidence` outcome.
- **high** Evidence Levels are labels rather than an operational authority model (§11 “Evidence And Release Qualification”) — “Live component integration” and “cross-system production-like evidence” distinguish intent, but do not define required environment properties, evidence producer/approver, artifact contents, freshness, applicability, or pass/fail rules. The statement that Levels 4 and 5 are required therefore does not prevent a weak artifact from being self-labelled at those levels. *Fix:* Define the acceptance contract for each level and map each `RQ-1` concern to its required evidence artifact, authority, environment, verification command or procedure, validity period, and blocking result.

## Substance over theater — strong

The content is earned by the product’s risk profile. Identity, provider governance, approval state, tenant isolation, safety, context bounds, retention, compatibility, capacity, and recovery all have product-specific consequences; the NFRs use concrete boundaries instead of generic claims. The journeys also drive distinct decisions rather than serving as decorative personas.

### Findings

No substantive findings.

## Strategic coherence — adequate

The feature set follows a clear thesis: prove governed AI participation in Conversations before adding memory, tools, ambient activation, or multiple exposed Agents. The metrics strongly cover adoption activity, workflow completion, authorization, audit, and surface parity, but do not establish that the generated assistance is useful or reliably delivered.

### Findings

- **high** The launch metrics can pass while user-facing utility is poor (§1 “core V1 bet”; §12 SM-1 through SM-6) — SM-1 requires only one tenant, SM-2 counts Conversations with an “accepted Agent Call,” and SM-3 treats `Rejected` and `Abandoned` as human resolution. No metric bounds call/generation/post failure rate or measures whether automatic replies and approved proposals are useful enough to sustain repeat use. A system with frequent failures or mostly rejected output could satisfy the stated thesis-validation set. *Fix:* Add a reliability metric over all authorized calls and an outcome-quality metric covering both response modes, with explicit failure/refusal denominators and a counter-metric preventing low-quality output from being hidden by activity volume.

## Done-ness clarity — adequate

FR-1 through FR-28 are contiguous and nearly every FR contains observable consequences; the quantified latency, recovery, capacity, compatibility, expiry, and safety boundaries are particularly useful. Two launch-critical behaviors remain ambiguous enough to produce materially different implementations.

### Findings

- **high** “Complete Audit Evidence” omits the causal request and configuration snapshot (§4 FR-8, FR-9, FR-24; §12 SM-5) — FR-8 records caller, Agent, Source Conversation, timestamp, and mode; FR-9 requires only “enough context metadata”; FR-24 makes policy/version identifiers conditional with “where available”; and SM-5 repeats a minimum list that excludes the caller prompt, exact Conversation revision/order, Agent Instructions/configuration version, and mandatory policy versions. The system could meet SM-5 yet be unable to prove what exact inputs and rules produced a posted response. *Fix:* Define a mandatory, privacy-safe evidence envelope per attempt containing the invocation input or protected reference, immutable Conversation snapshot boundary, Agent/configuration and instruction versions, provider/model/capability version, context and safety policy versions/decisions, attempt identifiers, timestamps, output/proposal version, and final posting outcome.
- **medium** Failed-content handling contradicts the proposal safety boundary (§4 FR-10 versus FR-27) — FR-10 says a failed generation does not create an “approvable Proposed Agent Reply unless generated content exists and is explicitly marked as failed or incomplete for audit only,” which grammatically permits an approvable proposal in the exception; FR-27 says failed content “cannot become an approvable Proposed Agent Reply.” *Fix:* State unambiguously that failed or incomplete content never creates an approvable proposal and define the separate quarantined/audit-only record, its visibility, and its terminal state.

## Scope honesty — strong

The PRD explicitly names V1 non-users, non-goals, MVP exclusions, fail-closed dependencies, and all resolved decisions. It does not silently defer memory, retrieval, tools, extra invocation modes, external notifications, or multiple product-visible Agents.

### Findings

No substantive findings.

## Downstream usability — adequate

The glossary, named protagonists, stable IDs, grouped FRs, and explicit UX/architecture implications make the artifact highly extractable. The main usability break is that several binding companion authorities are referenced only by generic name.

### Findings

- **medium** Binding companion artifacts are not resolvable from the PRD (§7 NFR-12 and NFR-13; §8) — Numeric limits live in “the readiness registry,” dependency commitments “remain in the external dependency register,” and “the final UX spines are binding launch authority,” but no canonical paths/IDs, ownership, version binding, or conflict-precedence rule is supplied. Architecture, UX, epics, and release reviewers cannot reliably source-extract the same authority set from this final PRD. *Fix:* Add a companion-authorities table naming each artifact’s canonical path or durable ID, owner, required status/version, consumers, and precedence when it conflicts with this PRD.

## Shape fit — strong

The shape fits a launch-level, governance-heavy B2B platform capability. Four journeys are proportionate to the distinct administration, invocation, approval, and integration workflows, while the capability groups and cross-cutting constraints support the intended UX → architecture → epics chain without persona or feature-list theater.

### Findings

No substantive findings.

## Mechanical notes

- FR IDs are unique and contiguous from FR-1 through FR-28; UJ IDs are unique and contiguous from UJ-1 through UJ-4; primary/secondary SM IDs are unique and contiguous from SM-1 through SM-6, with SM-C1 through SM-C3 clearly separated.
- Every explicit § / FR / NFR cross-reference inspected resolves.
- §0 says the document includes “an assumptions index,” but there is no Assumptions Index and no inline `[ASSUMPTION]` tag. Remove the claim or add an explicit empty index stating that no assumptions remain.
- Normalize glossary-term capitalization such as “approver policy” / “Approver Policy” and define operational terms that carry gate meaning, especially “production-like profile” and “terminal interaction.”
