---
title: UX Update reconciliation - 2026-09-09 post-validation (FR-33)
created: 2026-09-09
updated: 2026-09-09
spines:
  - DESIGN.md
  - EXPERIENCE.md
input: validation-report.md (96 findings; 7 critical filings / 6 distinct, 35 high, 33 medium, 21 low)
prd_revision: prd.md as of its 2026-09-09 revision
---

# Reconciliation: the 2026-09-09 post-validation Update

## 0. Why this run exists, and what changed underneath it

The re-validation of 2026-09-09 (four lenses, 96 findings) established that the pair was mechanically the cleanest it had been — 22/22 component parity, every token and source path resolving — while being **a revision behind on the one requirement that says who may do what**. The rubric and governance lenses independently found that `prd.md`, a listed source of both spines, carries a 2026-09-09 reconciliation whose centrepiece is **FR-33, a nineteen-row authorization matrix over six roles that OQ-21 declares authoritative**, and that the spines' five `RequiredPolicy` constants contradicted it on six operations.

Two things must be recorded about the baseline, because they change what parts of that report still mean.

**An approved sprint change proposal executed between the validation and this Update.** `sprint-change-proposal-2026-09-09.md` (Architecture Spine backlog reconciliation, `execution_status: complete`) lists `EXPERIENCE.md` under `amends_if_approved` and routed the UX owner a deliberately narrow task: apply two ownership/contract wording patches and introduce no route or journey redesign. It landed three row edits at 08:31, after the lenses had read the file, and it independently resolved or advanced findings this Update would otherwise have made: the **two-person deletion rule** (now a Platform Operator request with a distinct Compliance Inspector approval before execution), **compliance inspection** (Story 8.8, `AuditInspectionResult`, an inspection-case reference), the **platform/tenant content-safety contract pair**, `EXT-PROTECTION-1`, and the regeneration of `sprint-status.yaml`. Those changes are preserved intact; this Update builds on them rather than over them.

**One reported finding was wrong on its own terms.** The governance lens reported the platform-scoped constant as `Agents.PlatformProviderAdministrator`. The spine says `Agents.PlatformOperator`; the stale name survives only in `reconcile-validation-2026-09-09.md`. The finding's substance — the constant is not registered in shipped code and was missing from § Shipped-code corrections — was real and is now applied under the correct name.

## 1. What was applied

### The authorization model (the critical)

A new **§ Authorization roles** section binds each of the six FR-33 roles to a policy constant at a stated scope, and the `RequiredPolicy` column is re-derived from the matrix row by row rather than carried forward. Nine of the fifteen IA rows changed.

| FR-33 role | Constant | Change |
|---|---|---|
| Platform Operator | `Agents.PlatformOperator` (platform) | Now gates the full Provider catalog read, every `ProviderCatalogMutation`, per-tenant Provider/model enablement, platform Content Safety Policy publication, cap and rate-limit configuration, the cap override, the kill switch, and the deletion request |
| Tenant Agent Administrator | `Agents.Administrator` (tenant) | Retains tenant configuration; **loses** cap authorship above current values, the cap override, platform policy publication, and unfiltered catalog read; **gains** the two administrative `PostingFailed` exits and the per-Conversation block |
| Release Operator | `Agents.Operator` (tenant) | Launch readiness, cost-control posture, cap and rate-limit configuration, and the kill switch on recorded trigger conditions |
| Approver | `Agents.Approver` + FR-7 per-proposal resolution | The constant alone now authorizes no proposal action |
| Compliance Inspector | `Agents.ComplianceInspector` (tenant) | **New.** Legal hold, export, the deletion approval stage, and compliance inspection |
| Conversation Participant | none | Conversation membership and read access through the seam, plus the A-11 Agent call permission |

`Agents.AuditOperator` is **retired**: FR-33 has no role for it. Audit evidence now resolves per record — operational and posted-provenance evidence to the three operator roles and to a current Participant with read access; unposted protected content to an Eligible Approver or a Compliance Inspector.

Consequences applied across the pair: every governed write renders its **authorization basis** (FR-33 role and scope) in its confirmation and carries it in the audit record; a missing or stale assignment fails closed; the four `[ASSUMPTION]` rows in FR-33 (A-9 to A-12) are `UnretiredAssumption` blockers the readiness panel renders; and the **Agent call permission** (A-11) is named as a configuration field rather than left implicit.

### The three self-set boundaries

- **Cost controls** is now two authorization tiers on one surface. A Platform or Release Operator authors caps and rate limits; a Tenant Agent Administrator **may only lower** them, with the input rejecting any higher value with a typed reason and the confirmation stating that the boundary cannot be raised from this surface. The audited override is Platform Operator only and its control is *absent* for everyone else.
- **Content safety** splits platform publication (`Agents.PlatformOperator` with a rendered **Security approval stage**, a second separately authorized acceptance stage rather than a checkbox) from a tenant **stricter-only** delta whose editor cannot move a category toward permitted. Permitting a restricted category is a relaxation and therefore a platform act.
- **Provider catalog** scopes the **read** as well as the write: a tenant reader sees only tenant-enabled models, never configured state or `ConfigurationReferenceId`, and a secret-derived readiness reason (`Unconfigured`, `SecretUnavailable`) collapses to one undifferentiated `Blocked`, closing the reason-code disclosure channel. A **zero unit price is now a readiness blocker** (`Blocked / Blocked / Unpriced`), not a warning: it reserved nothing, so it made `BudgetBlocked` unreachable for every tenant using the model from one platform edit.

### Segregation of duties

The approval predicate was `sole Approver of their own call`, which let a caller approve their own reply whenever a second Approver existed. It is now the **Eligible Approver predicate in full** — five conjuncts, including *not the caller* and *not the last editor* — with `CanCurrentUserApproveSelectedVersion` returning that predicate and one typed reason per failing conjunct. **Edit-time eligibility** is added (FR-7): Edit is blocked when no Eligible Approver would remain after it, which was the PRD's remedy for a lone Approver stranding a proposal until expiry. `ApproverPolicySourceKind.Caller` is **retired** from the builder, and FR-7's exact configuration-time predicate replaces the generic "cannot satisfy segregation of duties".

### Lifecycle integrity (FR-18)

- `Expired` is scoped to `Pending`, `Edited`, `Regenerated`; **approval freezes expiry**, stated on all three post-decision rows. The old blanket rule would have rendered `Expired` over a `PostingFailed` proposal and collapsed its rail.
- An exhausted `PostingFailed` proposal **always has an exit**: the four FR-18 exits are enumerated, and *administrative abandon* and *administrative retry* are added to the rail under `Agents.Administrator`, audited, **with no Conversation read access required** — which is what made a proposal whose Conversation had gone unreachable by anyone.
- `retry posting` moves from operation family `ProposalResolution` to **`ConversationPosting`**, the only family whose gate set requires `LR-CONVERSATIONS-MEMBERSHIP-POSTING` and `LR-SAFETY`. Its bound is A-7's 3 attempts within 15 minutes, Architecture-owned, **not tenant-configurable**, and its confirmation now names all four FR-18 pre-post re-validation checks individually.
- `Abandoned` carries the four typed reasons; system-abandoned proposals are counted separately and excluded from the SM-3 denominator.
- Three pre-Provider rejection states are added: `NoEligibleApprover`, `KillSwitchActive`, `RemovedInConversations`.

### The two governed operations that had no surface

The **per-tenant kill switch** is a control on Launch readiness with its FR-28 semantics rendered in the confirmation (rejection and abandonment permitted, approval not; `Approved` and `PostingPending` complete on their own terms; nothing deleted). **Block and re-admit of `hexa`** is a control on Operational status beside that Conversation's history row, under the Tenant Agent Administrator or Conversation Facilitator (A-9), every set and clear audited — replacing the seam paragraph that described the notification design the addendum records as *rejected*. The surface-closure claim now says which operations are controls rather than routes, instead of asserting closure over two gaps.

### Denial evidence

Operational status gains a **per-tenant authorization-denial count** and audit gains a denied-attempt evidence class (actor, tenant, operation family, resource class, denial reason code, timestamp — never the inaccessible resource's identity). SM-4 requires zero unauthorized actions to be demonstrable and FR-28 makes a confirmed one the immediate kill-switch trigger; a trigger nobody can observe is not a control.

### Accessibility

- **Save and Discard now exist** in the action rail, in all enumerations. The previous Update's dirty-state rules blocked Approve and version selection "until you save or discard" while the rail contained neither control — a dead end with no exit but leaving the page.
- Version radios are **never disabled**: `DisabledFocusable` is not declared on `FluentRadio`. Dirtiness is handled at the selection event through the unsaved-changes confirmation, preserving the native radiogroup contract where the arrow key *is* the selection.
- Every `aria-disabled` control now carries its reason through **`aria-describedby`**, asserted in `LR-UI-CONFORMANCE`. Adjacent text is not an accessible description.
- `EXT-CONV-UI-1` gains a **third artifact kind**: a persistent Agents-owned status region beside the trigger, plus a focus-return contract. The panel's own nodes are destroyed by the Submit that closes the dialog, so every outcome of the only V1 invocation path was announcing into a removed node.
- The politeness table gains `submitted`, `awaiting projection`, `pending in another session`, connection restored, draft saved, draft discarded, and every display-only outcome; the FrontComposer speaker list is **closed**, with `FcStatusBadge` and `FcNewItemIndicator` forbidden.
- Focus follows an `FcAggregateDetailState` **slot swap** — the ordinary cold load of every detail route — and the five real state slots are named, with `StaleBanner`/`DegradedBanner` explicitly carrying no heading.
- **WCAG 2.2.1** is re-argued as an essential-exception claim on its own merits (pinned context, policy version, held reservation), recording that none of Turn off, Adjust, or Extend is satisfied and that the 15-minute floor is a usability commitment, **not** a 2.2.1 mechanism.
- The nearing-expiry chip carries visible text (`Expires in {duration}`), closing a colour-and-glyph-only status.
- `FcStatusFilterChips` is **not used on either grid** (its label is always `slot.ToString()`), and `FcFilterResetButton` comes off the required set because it clears a Fluxor snapshot the Agents-owned toggles do not write.

### Implementation readiness

§ Shipped-code corrections grows from eleven rows to seventeen: the `role="status"` row is widened from five badges to the **15 sites across 13 files**, the `Disabled` row from one control to a class across eleven pages, and six rows are added — **no CSS ships at all** behind ~115 BEM class names, the **two missing `not available` keys** while the forbidden per-cause keys exist, the three constant divergences, `AgentReadiness.MapState` and its UI-local enum, and the two raw-HTML violations of the repo UX law. Both status enums are declared **versioned additive** with an unknown value rendering `authoritative pending`. The `ProviderReadinessResult` by-Provider/model query gets an interim because the disable action ships in 5.3, before 5.5.

## 2. Prior decisions revised

| Prior decision | Revision |
|---|---|
| `Agents.Administrator` gates Content safety, Cost controls, and the full Provider catalog read | Re-gated per FR-33; the tenant role loses cap authorship, the override, platform publication, and unfiltered catalog read |
| `Agents.AuditOperator` gates audit evidence and governance | Retired; replaced by per-record resolution and `Agents.ComplianceInspector` |
| Approval blocked when "sole Approver of their own call" | Replaced by the five-conjunct Eligible Approver predicate |
| `Caller` is a buildable Approver Policy source | Retired; read-only for legacy policies and publication blocked while present |
| `Expired` rendered on any read | Scoped to the three awaiting-decision states; approval freezes expiry |
| Exhausted `PostingFailed` offers only a new Agent Call | Four FR-18 exits, including two administrative ones needing no Conversation read access |
| `retry posting` under family `ProposalResolution` | `ConversationPosting` |
| Retry maximum is a `hexa` configuration field | A-7's fixed 3 attempts over 15 minutes, Architecture-owned |
| Version radios `DisabledFocusable` while dirty | Never disabled; unsaved-changes confirmation at the selection event |
| A zero unit price is a warning | A Provider readiness blocker |
| `FcStatusFilterChips` mandatory "where its label contract allows"; `FcFilterResetButton` mandatory | Neither is used; Agents owns both filter and reset controls |
| The four-name grid set is FC-TBL's | An Agents-owned set; FC-TBL's envelope rules named separately |
| The 15-minute floor is required for the 2.2.1 exception | A usability commitment; the essential exception is argued on its own merits |
| Surface closure is final | Final for V1, with the kill switch and the per-Conversation block named as controls rather than routes |

## 3. Deferred, with named owners

- **Product** — confirm the four FR-33 `[ASSUMPTION]` rows (A-9 removal authority, A-10 per-tenant enablement, A-11 call permission, A-12 deletion approval) before the first tenant is enabled; each is an `RQ-1` `UnretiredAssumption` blocker until retired. Also A-6 (regeneration ceiling 3, range 1-10) and A-13 (SM-3/SM-7 thresholds); the UX-side ceiling deferral is withdrawn, since FR-32 now documents the numbers.
- **Product + Security** — OQ-18 unsafe historical content, due 2026-10-15 (A-14). UX ships blocked-history rendering only.
- **Architecture** — A-7 retry bound confirmation; the `ProviderReadinessResult` by-Provider/model query and whether it lands in 5.3 or 5.5; the polling and nudge contract, which the Architecture Spine still does not name while the two shipped paths diverge (250 ms/5 s and 200 ms/8 s); versioned-additive declarations for both status enums.
- **Conversations Maintainer** — `EXT-CONV-UI-1` remains `Uncommitted` with owner `TBD`, and its `RequiredArtifact` still specifies a narrower, incompatible seam (provenance from message metadata) than the three artifact kinds this spine now requires. Treated as **blocking, not clerical**: `epics.md` § Story 6.7 still reads "No new external seam" and the register's `ConsumingStories` is still `TBD`, so two of three documents a sprint planner reads call 6.7 ready.
- **Epics maintainer** — Story 6.2 owns `/agents/context-policy` with no AC naming a read model or route; the Operational status extras (per-tenant blocked counts, per-Conversation history, cost consumption, projection id/version) have no contract; `/agents/audit` and `/agents/audit-governance` still name no single owning story.
- **FrontComposer** — six nav glyphs (the fallback duplicates Overview's `Apps20` until they land), domain-keyed `FcStatusFilterChips` slots with localized labels, and a localizable shell badge label.
- **Security** — the approval stage on platform `PolicyPublication` needs a named authorization source; the spine specifies the stage, not who holds it.

## 4. Noted, not applied

- Six of the eight DESIGN component sections still restate behavior EXPERIENCE owns. Two were cut in this run (`pending-command-indicator`, `agent-response-marker`); the remaining six are a low-severity cleanup left for the next doc-standards pass, recorded here so they are not rediscovered as new.
- `## FrontComposer Readiness` remains a cross-index carrying no decision of its own. Kept by decision: every pointer resolves and it is the table a FrontComposer reviewer opens first.
- The surface-count denominators (15 IA rows, the DESIGN FullWidth/Constrained split counting Audit evidence twice) are each internally defensible; one stated count is deferred.
- Component Patterns cell density is unchanged by decision, per the Tenants precedent.

## 4b. Corrected after the fact: the Compliance Inspector policy constant

Section 1 above records `Agents.ComplianceInspector` as a **new** constant and `Agents.AuditOperator` as **retired** on the grounds that FR-33 has no role for it. **That was wrong, and it is reverted.**

AD-30 is the naming authority for FrontComposer policy constants and states: "FR-33 roles map one-to-one to FrontComposer policies `Agents.PlatformOperator` (Platform Operator), `Agents.Administrator` (Tenant Agent Administrator), `Agents.Approver`, `Agents.AuditOperator` (Compliance Inspector), and `Agents.Operator` (Release Operator)." The Compliance Inspector **role** was renamed; its **wire identifier** was not. This is the same convention AD-8 applies to `ApproverPolicySourceKind.ConversationOwner`, which UX already renders correctly as Conversation Facilitator, and it is what AD-30's own *Prevents* clause — "two role vocabularies for one matrix" — exists to stop.

Three findings settle it:

- `prd.md` FR-33 names the six roles and **no policy constant anywhere**. Constant naming is AD-30's alone, so this Update had no PRD basis for coining one.
- `Agents.ComplianceInspector` occurs nowhere in the Architecture Spine, the PRD, the epics, the registers, or `src/`. Outside these UX files its only occurrence is a proposed fix in `prd-agents-2026-06-23/review-implementation-drift-2026-09-09-update-gate.md`, which also proposes `Agents.ReleaseOperator` — a name neither AD-30 nor the shipped registration adopts. An unadopted review recommendation was promoted to authoritative.
- The shipped `AgentsFrontComposerRegistration.cs` declares Administrator, Approver, Operator, AuditOperator: **four of the five correct constants**, not four wrong ones.

Applied: `Agents.AuditOperator` restored in § Authorization roles, the register-constants bullet, the Audit evidence and Audit governance IA rows, § Audit evidence rules, and § Shipped-code corrections; "Compliance Inspector" kept as the rendered role label throughout, with a note binding the identifier to AD-30. The § Shipped-code corrections row was inverted by the same error and now records the one real divergence — `Agents.PlatformOperator` is not registered, so every platform-scoped row falls back to `Agents.Administrator`.

Deferred to Product/PM: add a role-to-policy-name column to the FR-33 table, so a PRD-only reader can reach the constant vocabulary AD-30 owns. That gap is how this collision was reachable.

## 5. What this run did not do

Doc standards (`bmad-review` structure and prose lenses) and the Reviewer Gate were **not** run. This Update rewrote the authorization model across both spines and grew EXPERIENCE.md substantially; a structure pass is likely to find lifted duplication, and a re-validation is the right next step once Architecture and Conversations absorb the deferred items. Neither was skipped for lack of relevance.
