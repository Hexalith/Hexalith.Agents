# Spine Pair Review — agents

## Overall verdict

The spine pair is broadly usable for architecture and story planning: the PRD's four key journeys are carried forward, the FrontComposer/Fluent inheritance model is explicit, and the component names align across `DESIGN.md` and `EXPERIENCE.md`. The main downstream risk is not visual identity; it is implementation ambiguity around per-surface states, proposal discovery entry points, and requirement traceability.

## 1. Flow coverage — adequate

Checked source UJs and launch requirements from the brief/PRD against `EXPERIENCE.md` Key Flows. UJ-1 through UJ-4 are present with named protagonists, numbered steps, climax beats, and failure paths where applicable.

### Findings

- **medium** The Key Flows preserve the four PRD UJs, but they do not trace the PRD's FR-1 through FR-25 requirement names/IDs to flows or IA surfaces, so downstream architecture/story extraction must reread the PRD to prove full requirement coverage (`_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md:106`, `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md:371`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:224`). *Fix:* Add a compact FR-to-surface/flow coverage table, or cite FR IDs directly in IA rows and flow steps.

## 2. Token completeness — adequate

Checked every frontmatter token family and every `{path.to.token}` reference in `DESIGN.md`. The local token references resolve, and the Fluent/FrontComposer inheritance posture is deliberate.

### Findings

- **medium** Load-bearing status colors are mapped to inherited Fluent semantic roles, but the spine does not state the contrast target or verification obligation for those role combinations; `EXPERIENCE.md` says visual contrast lives in `DESIGN.md`, but `DESIGN.md` only states no-color-only and semantic role use (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:121`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:131`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:178`). *Fix:* Add an explicit contrast target, forced-colors expectation, and build-time verification rule for status badge, action, and focus combinations.
- **low** The inherited color and shape tokens are encoded mainly as human-readable `note` objects, not as flat hex values or structured Fluent enum bindings; this follows the Tenants precedent, but strict `design.md` consumers may not be able to mirror the frontmatter mechanically without custom parsing (`.agents/skills/bmad-ux/references/design-md-spec.md:15`, `.agents/skills/bmad-ux/references/design-md-spec.md:17`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:17`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:45`). *Fix:* Add machine-readable inherited bindings such as Fluent `BadgeColor.*` / shape-owner fields while keeping the no-hex inheritance discipline.

## 3. Component coverage — adequate

Checked component names in `DESIGN.md` frontmatter/body against `EXPERIENCE.md` Component Patterns. The twelve Agents-specific component names align across both spines.

### Findings

- **medium** Proposal discovery depends on "notification entry" and "Conversation status" routes, but those are not given behavioral component rows and the notification path remains open; this leaves approver discovery vulnerable to story-level invention (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:46`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:47`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:292`). *Fix:* Add explicit component patterns for proposal notification/status entry points, or mark them as deferred behind OQ-4 with required interim discovery behavior.

## 4. State coverage — thin

Walked each IA surface and compared it with the canonical state tables. The spine has strong domain state vocabularies, but it does not allocate those states per surface.

### Findings

- **high** The IA lists ten surfaces, while State Patterns only provides canonical lifecycle sets plus a generic "every grid/list surface" rule; there is no per-surface state matrix for cold load, empty, filtered-empty, permission-denied, stale/degraded, error, in-flight, expiry, and read-only modes (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:39`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:152`). *Fix:* Add a table with one row per IA surface and the required states/treatments for that surface.
- **medium** Latency, timeout, and cost-control states are named as product risks/open questions and even appear in visual status language, but they are not landed as behavioral state treatments for Agent Calls, provider status, or operational status (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:125`, `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md:498`, `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md:499`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:293`). *Fix:* Add explicit slow/timeout/cost-warning states, or state that they remain blocked pending OQ-5/OQ-6 with no UI behavior beyond generic generation progress.

## 5. Visual reference coverage — adequate

Checked `mockups/`, `wireframes/`, `imports/`, and `.working/`. There are no visual files to link or orphan, and the spines-win-on-conflict rule is stated once.

### Findings

- **low** The workspace has no mockups, wireframes, or imports, and the memlog says mock coverage is still pending; layout-heavy surfaces such as proposal detail/editor and conversation invocation therefore rely entirely on prose/table contracts (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/.memlog.md:20`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:23`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:47`). *Fix:* Add a mock coverage decision and either link key-screen references or explicitly mark each IA surface as spine-only.

## 6. Bloat & overspecification — strong

Checked for source restatement, decorative narrative, pixel-level overreach, and prose where tables would serve downstream consumers better.

### Findings

- No findings. The spines mostly keep product content out of the UX contract, use tables for IA/components/states, and avoid inventing a bespoke visual system where FrontComposer/Fluent already owns the behavior.

## 7. Inheritance discipline — strong

Checked source resolution, source term preservation, component naming consistency, and token-reference discipline. The cited local sources exist, and the pair follows the FrontComposer/Fluent inheritance posture established by the Tenants reference.

### Findings

- No findings. UJ names, proposal lifecycle terms, component names, and FrontComposer responsibilities are consistent enough for downstream extraction; unresolved source decisions are carried as open questions rather than silently filled in.

## 8. Shape fit — strong

Checked canonical `DESIGN.md` section order and required `EXPERIENCE.md` sections. Required-when-applicable sections are present because the sources/memlog trigger FrontComposer inheritance, responsive behavior, and external inspiration/anti-patterns.

### Findings

- No findings. `DESIGN.md` follows the required body order, `EXPERIENCE.md` contains the default sections plus justified FrontComposer Readiness and Open Questions sections, and no Mermaid blocks are present.

## Mechanical notes

- Both spines are still `status: draft`; that is consistent with the memlog note that reviewer gate, mock coverage, and open UX questions are pending (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:4`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:3`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/.memlog.md:20`).
- `DESIGN.md` and `EXPERIENCE.md` source frontmatter resolves to local files for the brief, PRD, addendum, reconciliation note, Tenants UX/code references, and FrontComposer references (`_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md:7`, `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md:6`).
- No broken `{path.to.token}` references were found in the spines.
- No Mermaid syntax is present.
