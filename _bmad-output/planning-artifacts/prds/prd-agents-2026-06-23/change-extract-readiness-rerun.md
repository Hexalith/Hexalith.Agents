# Change Extract — Approved Readiness-Rerun PRD Update

## Verdict

The approved change signal requires a **precision update, not an MVP or Functional Requirement change**. Preserve Vision, journeys, FR-1 through FR-28, Non-Goals, MVP scope, success thresholds, and all OQ-1 through OQ-11 decisions. Update the PRD only to add NFR-11 through NFR-14; make external-dependency readiness binding; promote the two residual assumptions to V1 decisions; define V1 public-contract compatibility; make the Level 1-5 evidence taxonomy normative; and separate deterministic metric-calculation proof from operational qualification at release gate `RQ-1`.

The proposal is approved (`status: approved`, approved by Administrator on 2026-08-01) and explicitly says that Architecture, UX, Epics, the dependency/readiness registers, and sprint status remain separate follow-on reconciliations.

## Source Authority And Reconciliation

- **Change authority:** `sprint-change-proposal-2026-08-01-readiness-rerun.md`, especially §§1, 2, 4.5, 5, 7, and 8.
- **Prior approved authority preserved:** `sprint-change-proposal-2026-08-01.md`; its OQ decisions and Level 1-5 evidence definitions remain semantically valid. The rerun changes where the taxonomy is normative and how forward delivery is decomposed, not the taxonomy itself.
- **Current PRD bundle checked:** `prd.md`, `addendum.md`, and `.memlog.md` in this workspace.
- **Original product input checked:** `briefs/brief-agents-2026-06-23/brief.md`. It supports the single named V1 Agent, full Conversation context, explicit invocation, governed approval, and V1 exclusions.
- **Technical research checked where relevant:** `research/technical-dapr-ai-agents-research-2026-06-23.md`. It supports durable recovery/idempotency, explicit versioning, bounded concurrency/queues, capacity validation, and evidence gates as concerns. Its generic recommendations to compact/summarize prompts or add retrieval are not V1 product authority and must not override the approved full-context-or-blocked decision.

## Exact Approved PRD Changes

### NFR Authority

**Targets:** `§7 Cross-Cutting Non-Functional Requirements`; related launch consequences in `FR-28`; UI capability in `FR-22`; glossary only where a newly used authority term needs definition.

The current ten unnumbered §7 requirements correspond, in order, to NFR-1 through NFR-10: Security, Privacy, Reliability, Observability, Auditability, Provider Safety, Content Safety, Context Bounds, Performance, and Cost Control. Assign those stable IDs without changing their meaning, then append the following approved requirements verbatim in substance:

- **NFR-11 Availability And Recovery:** EventStore business state has RPO 0. Restart/replay cannot duplicate Provider attempts, proposal versions, timers, reservations, or Conversation posts. A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.
- **NFR-12 Capacity And Backpressure:** Before enablement, each environment records numeric per-tenant and system-wide concurrency, queue-depth, and backpressure limits. Exceeding a limit queues or rejects with a safe typed outcome before Provider invocation. Cross-tenant fairness and cost caps remain enforced. The release profile and numeric limits are visible in the readiness registry, not hidden in host configuration.
- **NFR-13 Accessible, Localizable, Responsive UI:** The final UX spines are binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior, use whole-string localization with English/French key parity, and fail closed for high-impact actions when the viewport cannot present required context.
- **NFR-14 UI Interaction Performance:** In the production-like profile, an authorized page reaches a usable non-loading state at p95 <= 2.5 seconds; a submitted command renders authoritative pending acknowledgement at p95 <= 500 ms; a projection-visible terminal change renders and is announced at p95 <= 2 seconds. Each gate uses at least 30 executions and returns `InsufficientEvidence`, not pass, when timestamps or samples are missing.

Reconcile `FR-28` so its launch-readiness consequences cite NFR-11 through NFR-14 and the normative evidence section. Do not replace the existing generation/posting latency targets in NFR-9/OQ-5; NFR-14 is a separate UI interaction budget.

### External Dependency Authority

**Targets:** `§8 Integration And Dependencies`; `FR-21 Fail Closed On Dependency Uncertainty`; `FR-28 Define Launch Readiness Controls`. The detailed register is a separate artifact, not PRD content.

Add a binding rule that a critical external dependency is implementation-ready only when the external dependency register contains:

1. named owner;
2. owning repository;
3. required artifact;
4. target version or commit;
5. target integration date;
6. compatibility contract/test and verification command;
7. required evidence level;
8. accepted status; and
9. consuming stories.

`Uncommitted`, a missing target, or a missing compatibility command blocks every consuming story from `ready-for-dev`. Keep the current `CONV-AI-1` dependency statement, but make it subject to this same commitment rule. The initial register must later cover `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`; those operational records should not be copied wholesale into the PRD.

### Compatibility Authority

**Targets:** `FR-23 Provide API And Client Contracts`, especially its current consequence “Breaking contract changes are avoided during V1 unless explicitly versioned”; `§10 API Contracts And Public Surface`.

Replace the ambiguous consequence with the approved V1 rule:

- JSON object evolution is additive within V1.
- Public enums define `Unknown = 0` and may add values without reusing an existing value's meaning.
- No public member or enum value may be removed, renamed, or semantically reused within V1.
- A breaking public change requires a new major package/API version and package-consumer compatibility tests.

Keep this as public-contract behavior. Concrete package pins, SDK choices, DTO transport mechanics, and CI command lines belong to Architecture, the dependency register, or the addendum—not the PRD.

### Evidence And Release Authority

**Targets:** a new normative PRD subsection, preferably adjacent to `§10 API Contracts And Public Surface` and before `§11 Success Metrics`; `FR-28`; the intro/cross-reference in `§12 V1 Decision Register`; glossary entries for `Evidence Level` and `RQ-1` if needed.

Move the already approved taxonomy from the prior proposal into the normative PRD section:

| Level | Normative evidence meaning |
| --- | --- |
| 1 | Contract/structure |
| 2 | Pure domain/unit behavior |
| 3 | Fail-closed deferred seam |
| 4 | Live component integration |
| 5 | Cross-system production-like evidence |

State that production-like readiness requires Levels 4 and 5 for runtime, authorization, tenant isolation, Provider, safety, Conversations, audit, and topology. Lower levels, skips, placeholders, or “where applicable” results remain visible blockers and cannot establish launch readiness. Replace both `FR-28` references claiming that the V1 Decision Register defines the taxonomy; the Decision Register must reference this normative section instead.

Also state the approved qualification split:

- Approved deterministic fixtures may prove metric formulas, sample/window/cohort handling, timestamps, and `InsufficientEvidence` behavior.
- Passing those fixtures completes calculator implementation; it does not prove live metric attainment.
- Real rolling-window/cohort attainment, required live Level 4/5 evidence, and the final READY/NOT READY decision remain the operational release gate `RQ-1` after implementation.

Preserve all existing SM-1 through SM-6 and SM-C1 through SM-C3 thresholds. Reconcile the current `FR-28` sentence that appears to block production-like generation until launch metrics are already recorded, so it does not create a circular prerequisite for collecting qualification evidence.

### Binding Treatment Of The Two Residual Assumptions

**Targets:** `§3 Glossary` (`Conversation Context`), `§6.2 Out Of Scope For MVP`, `§12 V1 Decision Register`, and `§13 Residual Assumptions Index`.

- Remove the `[ASSUMPTION]` marker from the Conversation Context definition and make its exclusions binding: V1 uses only the complete authorized Source Conversation and excludes long-term memory, project content, folder content, external tools, and non-conversation retrieval.
- Remove the `[ASSUMPTION]` marker from the multiple-Agents scope statement and make this binding: generalized internal structures remain permissible, but V1 exposes only `hexa`.
- Append stable Decision Register rows rather than renumber OQ-1 through OQ-11. The proposal does not assign IDs; the least disruptive choice is OQ-12 for the context exclusions and OQ-13 for single-exposed-Agent behavior.
- Remove `§13 Residual Assumptions Index` if no assumptions remain; otherwise retain only genuinely unresolved assumptions.

## Conflicts And Supersessions To Surface Before Editing

1. **No product-scope conflict:** the approved proposal explicitly preserves the MVP, all 28 FRs, prior approved decisions, and completed Epics 1-4. Do not add/remove FRs or change success thresholds.
2. **Assumption-status conflict:** the memlog says open questions were non-blocking/deferred, and the current PRD still labels the two items as assumptions. The new approved proposal intentionally supersedes that status for these two items only; their substance already matches the brief and current PRD.
3. **Evidence-authority defect, not taxonomy conflict:** the prior approved proposal defines Levels 1-5, while current `FR-28` incorrectly says the visible Decision Register defines them. Preserve the meanings and move them to a normative PRD evidence section.
4. **Metric-gate ambiguity:** current `FR-28` can be read as requiring real launch metrics before production-like generation can run. The approved change separates deterministic calculation conformance from later operational `RQ-1` attainment; wording must remove that circularity without weakening the real launch gate.
5. **Research/product tension:** the technical research recommends prompt compaction, retrieval summaries, and bounded payload strategies as generic optimizations. For V1 those conflict with the brief, OQ-10, and the approved promotion of complete-context exclusions. Treat the research advice as V2/downstream exploration only.

## Content That Does Not Belong In The PRD

- **External dependency register:** concrete owners, repositories, commits/versions, dates, verification commands, live status, and consuming-story mappings belong in `external-dependency-register.md`.
- **Architecture:** Provider readiness/degraded schema, gate-record schema and freshness rules, projection inventory, command-concurrency scope keys, runtime/host/tokenizer/safety/secret ownership, package/SDK baselines, and production-like topology details belong in `ARCHITECTURE-SPINE.md` or its registries.
- **UX:** warning/success styling, active-versus-callable presentation, command-lock interaction patterns, focus/live-region mechanics, responsive layouts, and the UI conformance plan belong in `DESIGN.md` and `EXPERIENCE.md`; the PRD keeps only measurable NFR authority.
- **Epics and release planning:** replacement Epics 5-8, 24 story definitions, old-story mappings, per-story evidence manifests, and sprint-status rows belong in `epics.md`, the superseded-plan appendix, and sprint status. `RQ-1` is a gate, not a development story.
- **Addendum:** preserve only implementation rationale/options that help downstream design without becoming product authority—for example rejected technology alternatives, why a particular adapter/topology/package baseline was chosen, or technical-research caveats. None of the approved NFR, dependency qualification, compatibility, or evidence rules should be relegated to the addendum.

## Explicit Sequencing Reservation

The approved proposal records that no PRD, Architecture, UX, Epics, or sprint-status source artifact was modified during the Correct Course run. Its handoff assigns Product Manager ownership of the PRD change, then Solution Architect, UX Designer, and Product Owner ownership of their respective reconciliations. Sections 4.2-4.3 and 4.8-4.9 reserve epic/sprint changes; §4.6 reserves architecture contracts; §4.7 reserves UX semantics. The handoff checkpoint is to create dependency/readiness registers and synchronize PRD, Architecture, UX, and Epics before sprint status changes. Therefore this PRD update must not opportunistically rewrite Architecture, UX, Epics, or sprint status.
