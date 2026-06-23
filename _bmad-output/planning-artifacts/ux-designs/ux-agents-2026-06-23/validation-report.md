# Validation Report - agents

- **DESIGN.md:** `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- **EXPERIENCE.md:** `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- **Run at:** 2026-06-23T16:54:29+02:00

## Overall verdict

The spine pair is broadly usable for architecture and story planning: the PRD's four key journeys are carried forward, the FrontComposer/Fluent inheritance model is explicit, and component names align across `DESIGN.md` and `EXPERIENCE.md`. The main downstream risk is not visual identity; it is implementation ambiguity around conversation invocation, proposal ownership, authorization/evidence boundaries, and per-surface state coverage.

The extra reviewer lenses sharpen the picture: accessibility is directionally strong, but governance and implementation readiness are not safe to hand off as final. Treat the current pair as a strong draft that needs blocking amendments before architecture and story-dev rely on it.

## Category verdicts

- Flow coverage - adequate
- Token completeness - adequate
- Component coverage - adequate
- State coverage - thin
- Visual reference coverage - adequate
- Bloat & overspecification - strong
- Inheritance discipline - strong
- Shape fit - strong

## Findings by severity

### Critical (3)

**Governance / Implementation** - Conversation invocation is unresolved (`EXPERIENCE.md:52`, `EXPERIENCE.md:244`, `prd.md:494`)

The primary runtime entry point is still open across mention, command, action button, participant membership, mention resolution, or a combination. This blocks consistent identity attribution, prompt capture, keyboard entry, accessible naming, denial copy, proposed-vs-posted rendering, API route naming, and acceptance tests.

Fix: Close OQ-1 before Agent Call stories. Choose the V1 invocation pattern, or a constrained set, and specify owning surface, required fields, visible `hexa` identity, permission checks, in-flight/no-post states, keyboard/focus behavior, accessible names, and a low-fi Conversation wireframe.

**Implementation** - Proposed Agent Reply ownership is unresolved (`EXPERIENCE.md:290`, `prd.md:495`)

The runtime and storage owner for Proposed Agent Replies is still an open question, so architecture cannot assign aggregate boundaries, proposal versions, expiry, authorization decisions, audit evidence, status ownership, queue/editor responsibility, or API contracts without guessing.

Fix: Add an ownership contract naming the module/aggregate responsible for proposal state, versions, expiry, authorization decisions, audit evidence, and boundaries with Conversations, Parties, Providers, admin UI, and public API.

**Governance** - Approver authorization does not require current Source Conversation access (`EXPERIENCE.md:89`, `prd.md:305`)

The approver policy can include tenant roles and predefined Parties, but the UX contract does not require proposal list/detail/edit/regenerate/approve to also pass fresh Source Conversation access. A same-tenant Party could be authorized by policy while not allowed to inspect the source Conversation content.

Fix: Require both Approver Policy authorization and current Source Conversation access for proposal discovery, detail, edit, regenerate, approval, rejection, abandonment, and audit inspection. Add blocked/hidden/denied states for stale or missing access.

### High (8)

**Implementation** - Open questions are not triaged into blockers vs carry-forward assumptions (`EXPERIENCE.md:283`, `prd.md:490`)

The UX spine copies OQ-1 through OQ-10 but does not classify which questions block flow specification, aggregate design, provider contracts, proposal lifecycle stories, audit stories, or context-building stories.

Fix: Replace the flat list with a triage table: blocker/carry-forward, owner, downstream gate, affected surfaces, and minimum assumption if deferred.

**Rubric** - Per-surface state coverage is thin (`EXPERIENCE.md:39`, `EXPERIENCE.md:152`)

The IA lists ten surfaces, while State Patterns provides canonical lifecycle sets plus a generic grid/list rule. Story-dev still lacks a surface-by-surface state matrix for cold load, empty, filtered-empty, permission-denied, stale/degraded, error, in-flight, expiry, and read-only modes.

Fix: Add one table row per IA surface with required states and treatments.

**Governance** - Action-time reauthorization is missing (`EXPERIENCE.md:123`, `prd.md:323`)

The state model authorizes a call once but does not define reauthorization when Conversation access, Party identity, Agent lifecycle, provider state, or approver policy changes while a proposal/editor is open.

Fix: Add `revalidating`, `authorization stale`, and `denied after revalidation` states. Require re-checks before generation, regeneration, approval, posting, and automatic posting.

**Governance / Implementation** - API/admin parity is not a build contract (`EXPERIENCE.md:271`, `prd.md:320`, `prd.md:347`)

Parity is a journey and success criterion, but there is no operation-by-operation matrix for shared actions, authorization outcomes, structured errors, status, and audit references.

Fix: Add a parity table for Provider administration, Agent configuration, Agent invocation, proposal workflow, status, and audit. Map UI action, API/client operation, shared authorization result, error shape, and audit/status evidence.

**Implementation** - Component contracts are not detailed enough for story-dev (`EXPERIENCE.md:82`)

Components have useful behavioral descriptions, but not build-level inputs, actions, events, ownership, auth/read-only variants, data fields, or status dependencies.

Fix: Add a component contract table per named component with owner, consumed data, emitted commands, loading/empty/error/denied states, accessible-name requirements, and linked API/status dependencies.

**Governance / Accessibility** - Proposal discovery, notifications, and expiry are not safely bounded (`EXPERIENCE.md:46`, `EXPERIENCE.md:91`, `EXPERIENCE.md:259`, `prd.md:496`, `prd.md:497`)

The queue and notification paths are unresolved while pending and historical proposal discovery is allowed. The spine does not define safe row/notification payload tiers, generated-text exposure, denial-safe counts, announcement timing, or expiry warning behavior.

Fix: Define queue-row and notification payload tiers. Default notifications to opaque references until authorization is proven, avoid generated snippets by default, define expiry countdown/warning/terminal behavior, and mark queue-only discovery as the launch assumption if notifications are deferred.

**Governance** - Audit availability is modeled as display state, not a side-effect invariant (`EXPERIENCE.md:156`, `prd.md:362`)

Downstream stories could report approved/posted success while evidence is delayed, missing, or uncorrelated.

Fix: Require every side-effecting success state to show a durable audit/correlation reference, or define an explicit `posted with audit pending` incident state with retry/escalation and no silent success.

**Accessibility** - Proposal version selection and focus recovery are under-specified (`EXPERIENCE.md:171`, `EXPERIENCE.md:183`, `prd.md:281`)

Approval posts exactly the selected version, but the UX does not define an accessible selection model. Focus return also assumes the triggering row/action still exists after approval, rejection, abandonment, expiry, or filtering.

Fix: Specify the version list semantics as tabs/listbox/grid with selected/current state, deterministic focus order, labelled editor region, confirmation naming exact version timestamp/source, and fallback focus targets when the triggering row leaves the view.

### Medium (14)

**Rubric** - FR traceability is incomplete (`prd.md:106`, `prd.md:371`, `EXPERIENCE.md:224`)

The four PRD UJs are covered, but FR-1 through FR-25 are not traced to flows or IA surfaces.

Fix: Add a compact FR-to-surface/flow coverage table, or cite FR IDs directly in IA rows and flow steps.

**Rubric** - Contrast and forced-colors verification are not stated (`DESIGN.md:121`, `DESIGN.md:131`, `EXPERIENCE.md:178`)

Load-bearing status colors inherit Fluent roles, but `DESIGN.md` does not state contrast targets or verification obligations for status badge, action, and focus combinations.

Fix: Add explicit contrast targets, forced-colors expectations, and build-time verification rules.

**Rubric** - Latency, timeout, and cost-control behaviors are named but not landed (`DESIGN.md:125`, `EXPERIENCE.md:293`, `prd.md:498`, `prd.md:499`)

These appear as risks/open questions and visual language, but not as behavioral state treatments.

Fix: Add slow/timeout/cost-warning states, or state that they remain blocked pending OQ-5/OQ-6 with only generic generation progress in V1.

**Rubric** - Proposal notification/status entry components are missing (`EXPERIENCE.md:46`, `EXPERIENCE.md:47`, `EXPERIENCE.md:292`)

Proposal discovery references notification entry and Conversation status routes, but they are not represented as behavioral component patterns.

Fix: Add component patterns for proposal notification/status entry points, or defer them behind OQ-4 with interim queue discovery behavior.

**Accessibility** - Responsive FC-TBL row detail needs acceptance criteria (`EXPERIENCE.md:182`)

Hidden columns and row details must preserve header relationships, action names, keyboard expand/collapse semantics, "needs my action", and expiry context.

Fix: Add FC-TBL criteria for collapsed columns and row detail semantics.

**Accessibility** - Live-region behavior is too broad (`EXPERIENCE.md:184`)

Transitions are listed, but not mapped to politeness, atomicity, de-duplication, or source-specific announcement text.

Fix: Add a transition matrix for created, posted, failed, expired, denied, and ordinary pending progress.

**Accessibility** - Narrow viewport disabled actions need accessible reasons (`EXPERIENCE.md:211`)

Responsive fail-closed approval requires a visible reason, but not an accessible disabled-action reason.

Fix: Require focusable explanatory text or `aria-describedby` for unavailable high-impact actions.

**Accessibility** - Assistive text localization is not explicitly covered (`DESIGN.md:139`)

Visible domain copy is whole-string localizable, but accessible names, aria descriptions, tooltips, and live-region announcements are not explicitly included.

Fix: Extend FC-L10N requirements to all non-visible assistive text using safe named placeholders.

**Governance** - Version rows need immutable references and stale-selection protection (`DESIGN.md:198`)

Version preservation is strong, but approval safety needs immutable version ids, fingerprints or references, and locking when a newer version appears.

Fix: Require stable version id/reference, source, timestamp, selected-version lock, and block approval if a newer version appears after selection.

**Governance** - Party identity display and hydration failure rules are missing (`EXPERIENCE.md:109`)

The UX covers Party identity conceptually, but does not require posting/approval surfaces to show stable Agent Party references or handle display hydration failure.

Fix: Add display rules for `hexa` name plus stable PartyId/support-safe reference in confirmations and audit; fail closed for posting when command-time Party validation fails.

**Governance** - Provider secret field lifecycle is under-specified (`DESIGN.md:188`)

Secrets are not displayed, but rotation, replacement, clearing, validation failure, copy/export, and accessibility behavior are undefined.

Fix: Add `not configured`, `configured`, `replace pending`, `rotation failed`, and `cleared` states with redacted validation errors and no secret value in DOM text, tooltips, copied text, logs, or audit.

**Governance** - Automatic mode needs explicit activation confirmation (`EXPERIENCE.md:87`)

The mode toggle is mutually exclusive, but the UX does not require an activation/reconfiguration review that states automatic mode posts generated content without human approval.

Fix: Add a review step naming tenant, Agent Party, provider/model, call eligibility, future-only effect, audit reference, and "posts without approval" consequence.

**Implementation** - FrontComposer implementation constraints are not carried into the spine (`Hexalith.FrontComposer/_bmad-output/project-context.md:29`)

The spine inherits FrontComposer but omits implementation-critical rules such as the exact Fluent package pin, raw HTML control prohibition, custom icon factory, and multi-section FluentAccordion guideline.

Fix: Add a short FrontComposer implementation appendix for these non-obvious inherited constraints.

**Implementation** - Mock/wireframe coverage is undecided (`.memlog.md:20`, `EXPERIENCE.md:47`)

The workspace has no mockups, wireframes, or imports. Complex surfaces rely entirely on prose/table contracts.

Fix: Add minimal wireframes or an explicit no-mock rationale for Conversation invocation, proposal detail/editor with version history, approver policy builder, provider catalog, proposal queue, and narrow-width review-only behavior.

### Low (2)

**Rubric** - Inherited tokens may need machine-readable bindings (`DESIGN.md:17`, `DESIGN.md:45`, `.agents/skills/bmad-ux/references/design-md-spec.md:15`)

Inherited color and shape tokens are mostly `note` objects, which is deliberate, but strict `design.md` consumers may not mechanically resolve them.

Fix: Add machine-readable inherited bindings such as Fluent `BadgeColor.*` and shape-owner fields while keeping the no-hex inheritance discipline.

**Rubric** - Visual reference coverage is prose-only (`EXPERIENCE.md:23`, `.memlog.md:20`)

No visual files are orphaned because none exist, but layout-heavy surfaces have no visual reference.

Fix: Record mock coverage as spine-only per IA surface or add key-screen references.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-governance.md`
- `review-implementation-readiness.md`

