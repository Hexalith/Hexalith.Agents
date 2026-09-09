# Validation Report — Hexalith Agents

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- **Run at:** 2026-09-08T22:30:49Z
- **Lenses:** rubric walker, accessibility, governance & audit, implementation readiness

## Overall verdict

The pair is a consumable contract: the 22 component names are identical across the DESIGN.md frontmatter, the DESIGN.md sections, and the EXPERIENCE.md Component Patterns table; every token reference in both files resolves; all 33 source paths resolve; UJ-1 to UJ-4 are carried verbatim with protagonist, numbered steps, climax, and failure paths; and 26 of the 28 prior rubric findings are verifiably closed in the text. What keeps it from strong is one high finding, the `FcFluentIcons` inventory in DESIGN.md being stale against its own listed source so that six status glyphs the spine tells downstream to request already exist and three badge components would ship text-only under the spine's own rule, plus six medium gaps a story-dev would otherwise decide alone: a v4 `Appearance.Accent` on the confirm button, a nav-glyph reuse that contradicts the spine's rule, no binding of detail routes to `FcAggregateDetailState`, the `nearing expiry` threshold committed in the reconcile but not carried, regeneration-ceiling numbers pointing at an FR that has none, and no surface action for disabling `hexa`. No critical findings.

The three additional lenses confirm the 2026-09-08 update landed: every one of the 92 prior findings is resolved or partially resolved, none is unresolved, and all four prior criticals are closed. What remains is seam-shaped rather than meaning-shaped. Accessibility finds the spine delegating to FrontComposer surfaces that do not provide what it assumes: `FcAggregateDetailPage` has no heading parameters, `FcStatusFilterChips` cannot express a localized `needs my action` chip, the one-status-node-per-route rule is false the moment a mandatory Fc component renders, nobody owns announcements on the Conversation seam, and the response-mode radio fires a modal on the first arrow key. Governance finds the second side effect and evidence removal under-specified: `retry posting` has no surface or policy, an `Approved` proposal stays editable, hold release and deletion are one role and one click with no justification, and a platform-scoped Provider catalog sits behind a tenant-scoped policy. Implementation readiness finds the FrontComposer services cited by name but not by shape (`IBadgeCountService` is `Type`-keyed, the FC-TBL components need a `ViewKey`), the Conversations seam missing a callability query and a message-marker slot, and the epics and register disagreeing with the spine on whether Story 6.7 is blocked. Findings that converge from independent lenses (the nearing-expiry threshold from all four, the icon inventory, the detail-page heading, the badge count, the live-region duplication, the Conversations seam, `Appearance.Accent`, `DisabledFocusable`) are the highest-confidence items to roll into an Update.

## Category verdicts

- Flow coverage — adequate
- Token completeness — adequate
- Component coverage — strong
- State coverage — adequate
- Visual reference coverage — strong
- Bloat & overspecification — adequate
- Inheritance discipline — adequate
- Shape fit — strong

## Findings by severity

| Severity | Rubric | Accessibility | Governance & audit | Implementation readiness | Total |
|---|---|---|---|---|---|
| critical | 0 | 0 | 0 | 0 | 0 |
| high | 1 | 5 | 5 | 6 | 17 |
| medium | 6 | 7 | 10 | 14 | 37 |
| low | 8 | 10 | 6 | 6 | 30 |
| **total** | 15 | 22 | 21 | 26 | 84 |

Prior run (2026-09-08, pre-update): 92 findings, 4 critical, 23 high, 38 medium, 27 low. Regression: 85 resolved, 14 partially resolved, 0 not resolved across the four lenses.

### Critical (0)

None.

### High (17)

**[Inheritance discipline]** — `FcFluentIcons` inventory is stale; six status glyphs filed as requests already exist (§ DESIGN.md:168, :211-219; FcFluentIcons.cs:66-90)  
The role→glyph table marks six of seven status roles as FrontComposer requests, but the source already exposes `CheckmarkCircle16`, `ArrowSync16`, `Warning16`, `SubtractCircle16`, `DismissCircle16`, `QuestionCircle16`, `InfoCircle16`. Under the spine's own rule (no glyph until a request lands) three badge components ship text-only for six roles, degrading the no-colour-only promise. `DevMode`/`DeveloperBoard` are one glyph, and the `status-subtle` row binds a Size20 nav glyph beside Size16 status glyphs.  
Fix: Rewrite the role→glyph table to the factory methods; restate the curated inventory as it is in the source (ten Size20 nav glyphs by `TryCreate` name, thirteen Size16 glyphs by method); keep only genuinely missing glyphs as requests.

**[Accessibility]** — Route-heading rule cannot be implemented on Constrained detail routes as written (§ EXPERIENCE.md:345, :407; DESIGN.md:81, :236; FcAggregateDetailPage.razor.cs)  
`FcAggregateDetailPage` has no `Heading`, `HeadingTabIndex`, or `PageTitle` parameter; the domain supplies all headers. Its non-ready slots render caller content in place of the body, so `not available` and loading states have no h1, no page title, and no focus target after navigation. Eight of twelve routes are Constrained.  
Fix: State that on detail routes the domain renders `FcPageHeader` above `FcAggregateDetailPage` or as the first child of every state slot; add "heading present and focused in the `not available` state" to the conformance lane.

**[Accessibility]** — The mandatory `needs my action` chip is not expressible with `FcStatusFilterChips` (§ EXPERIENCE.md:154-155, :134, :367; FcStatusFilterChips)  
`AvailableSlots` is typed on the closed `BadgeSlot` enum and the visible chip text is `slot.ToString()`, the raw English enum name. A screen-reader user would hear "Accent, filter inactive" for the chip meaning needs my action, and a French tenant would see an untranslated English word, violating the whole-string parity gate.  
Fix: Raise a FrontComposer request for domain-keyed slots with a localized label; until it lands, render state and `needs my action` filters as Agents-owned `aria-pressed` toggle buttons in a named `role="group"`, or drop `FcStatusFilterChips` from the mandatory list.

**[Accessibility]** — "One `role="status"` node per route" is false once mandatory Fc components render; duplication is unmanaged (§ EXPERIENCE.md:336, :154, :340; DESIGN.md:221)  
`FcFilterEmptyState` and `FcExpandInRowDetail` render their own polite status nodes inside every Agents grid page; `FluentMessageBar` carries an intent-driven `AriaLive` default so page notices announce twice; the shell's `FcProjectionConnectionStatus` already announces connection loss while Agents adds an assertive alert. NFR-14 evidence measures the Agents node, so duplicates muddy it.  
Fix: Replace with "one Agents-owned node of each politeness per route, and every event is announced by exactly one node": `FluentMessageBar` with `AriaLive="Off"` when its text is pushed to the Agents node; `connection lost` shell-announced with Agents carrying the consequence; list which Fc status nodes may speak.

**[Accessibility]** — Nobody owns announcements or post-submit focus on the Conversation seam (§ EXPERIENCE.md:336, :63, :81, :87; DESIGN.md:307)  
The live-region rule is per Agents route, but Call hexa, `submitted`, `denied`, `capacity queued`, `generation failed`, and `Posted` all happen on a Conversation-owned surface with no Agents route. The exported panel is not told to carry status nodes, and the dialog says nothing about what happens after Submit. A blind participant in UJ-2 presses Submit and hears nothing.  
Fix: Make `ConversationAgentCallPanel` self-contained: it owns one polite and one assertive node with the same politeness table; on Submit the dialog closes and focus returns to Call hexa (`aria-disabled` while pending); status rows sit after the button in DOM order. Record the same as an `EXT-CONV-UI-1` requirement.

**[Accessibility]** — `response-mode-toggle` fires the confirmation on change, which is the first arrow key (§ EXPERIENCE.md:151; DESIGN.md:279)  
Radio semantics move selection with arrow keys, so a keyboard user pressing Down to read the second option is already in a modal with Cancel focused; on Cancel the spine does not say the radio reverts. This is the only control for an `AgentSetupMutation`.  
Fix: The radio group changes a draft value only; the confirmation opens from an explicit Apply button `aria-disabled` until the draft differs; Cancel reverts and announces `Response mode unchanged: {mode}`. Apply the rule to every "change opens confirmation" binding.

**[Governance & audit]** — `retry posting` is a Conversation write with no surface, policy, family, bound, confirmation, or evidence rule (§ EXPERIENCE.md:258, :184, :166, :156, :105)  
The lifecycle promises a bounded audited retry reusing the deterministic `MessageId` and Voice offers the copy, but `proposal-editor` lists only edit, regenerate, approve, reject, abandon. A story-dev will put a bare Retry button on the status page for `Agents.Operator`, who is not an Approver, and FR-18's pre-post re-checks will be invisible.  
Fix: Add `retry posting` to the `proposal-editor` action rail for `Agents.Approver` with current Conversation read access; family `ProposalResolution`; the confirmation renders `ApprovedVersionId`, the typed failure reason, attempts used of the configured maximum, and the re-check statement; each attempt is a row in version history and audit; exhaustion leaves only `Start a new Agent Call`; the maximum is a `hexa` configuration field.

**[Governance & audit]** — `Approved`, `PostingPending`, and `PostingFailed` are non-terminal, so edit, regenerate, and approve remain available after approval (§ EXPERIENCE.md:156, :250-258; DESIGN.md:299)  
The editor's only terminal rule is read-only in a terminal state; the regenerate lock covers only the same session. A version other than the approved `VersionId` can be approved and posted, or an edit lands while posting is pending, against FR-17 and FR-18.  
Fix: Once `Approved` is projection-confirmed, edit, regenerate, and approve are absent; the editor pins `ApprovedVersionId` read-only; the only actions are `retry posting`, abandon where permitted, and audit; a new decision requires a new Agent Call.

**[Governance & audit]** — Hold release, export, and deletion are single-role, single-click, unjustified, and confirmed by one generic sentence (§ EXPERIENCE.md:62, :164, :168; DESIGN.md:331; PRD FR-30; epics.md Story 8.3)  
FR-30 requires every governance operation audited with actor and justification and Story 8.3 distinguishes request, confirm, and cancel, yet the spine has one `request deletion` action, no justification input, no scope rendering, no hold interplay. An operator who wants a mistake gone releases the hold and requests deletion in two dialogs.  
Fix: Required justification on `LegalHold` release, `ExportRequest`, `DeletionRequest`, `PolicyPublication`, `TenantBudgetUpdate`; the deletion confirmation renders exact scope, hold state, confirming projections, and an irreversibility line; deletion is `aria-disabled` while a matching hold is active or `EXT-SECRETS-1` is not Available; a separate confirm stage by a different Party, or an explicit Product plus Security deferral if a two-person rule is rejected.

**[Governance & audit]** — The platform-scoped Provider catalog is gated by the tenant-scoped `Agents.Administrator` policy (§ EXPERIENCE.md:52, :154; PRD FR-19, FR-4)  
A hostile tenant admin can disable or reprice a model every other tenant uses; the confirmation shows them the cross-tenant blast radius as a count, which is disclosure control, not authorization. FR-19 permits cross-tenant reach only when explicitly platform-scoped and authorized; the spine states the scope and never the authorization.  
Fix: Split read from mutation policy: `Agents.Administrator` reads; `ProviderCatalogMutation` requires a platform-scoped policy constant registered in `AgentsFrontComposerRegistration`; the confirmation names the platform scope; tenant admins see mutation controls absent.

**[Governance & audit]** — Configuration and governance changes have no audit surface (§ EXPERIENCE.md:61, :152, :167; DESIGN.md:247, :341; PRD FR-24, FR-32)  
`audit-evidence-panel` enumerates interaction evidence only; FR-24 requires evidence for Agent and Provider configuration and every override, FR-32 requires prior and new cap values. No component defines a configuration-change record or where a `ProviderCatalogMutation`, `PolicyPublication`, `TenantBudgetUpdate`, or `AgentActivation` is inspectable end to end.  
Fix: Add a configuration-change evidence class to `/agents/audit` and each write surface's History item: actor, family, resource identity, old to new values where safe, published version, concurrency token, projection id and version, justification, acceptance stages; overrides show scope and expiry.

**[Implementation readiness]** — The Conversations seam rule is stated only in the spine; epics and register disagree; harness de-listing has no owner (§ EXPERIENCE.md:82, :68; epics.md:1841-1843; external-dependency-register.md:75; AgentsFrontComposerRegistration.cs (order 4))  
The spine blocks Story 6.7 while `EXT-CONV-UI-1` is Uncommitted; `epics.md` says 6.7 has no new external seam and the register's consuming stories are TBD, so a sprint planner will mark 6.7 ready. "Delisted now" has no owning story while the harness stays registered.  
Fix: Name the story that de-lists the harness in the IA rules; file the epics and register correction as a deferred item with owner so 6.7's External line and the register's consuming-story field match the spine.

**[Implementation readiness]** — `agent-response-marker` has no seam or provenance source (§ DESIGN.md:309-311; EXPERIENCE.md:90, :159; external-dependency-register.md:69)  
The register's artifact is an action contribution; rendering a badge beside a posted Message is a message-decoration extension, and no contract maps `MessageId` to posted-by-hexa, generated vs edited, and editing Party. Story 6.7 owns the attribution and cannot satisfy it.  
Fix: Widen the `EXT-CONV-UI-1` artifact to a per-message contribution slot with an Agents-side provenance gateway keyed by `MessageId`, or state the marker is rendered from Conversation-side message metadata written at posting and name the story that writes it.

**[Implementation readiness]** — Status glyphs are requested from FrontComposer but already exist, and the curated list is wrong (§ DESIGN.md:168, :187, :207, :213-218; FcFluentIcons.cs)  
`FcFluentIcons` exposes the six status factories today; `DeveloperBoard` is not a name; the string `TryCreate` contract used by nav entries accepts only 12 names. Following "until a request lands, renders without a glyph" ships every badge without an icon, failing the no-colour-only rule and `LR-UI-CONFORMANCE`.  
Fix: Replace the role→glyph table with the factory names, keep only nav-entry glyphs (string contract) as requests, and note that 16 px factories are not reachable through `TryCreate`.

**[Implementation readiness]** — `IBadgeCountService` cannot carry the pending-proposal count as specified (§ DESIGN.md:335; EXPERIENCE.md:165, :404; IBadgeCountService.cs; BadgeCountService.cs:132-202; navigation.md:62)  
Its contract is `IReadOnlyDictionary<Type,int>` populated over the projection catalog's action-queue types, rendered on domain tiles and projection flyouts, not on a nav entry. Agents uses no generated projection lane, so there is no `Type` to key and no slot to render into.  
Fix: State the mechanism: register a read-model type for `pending-proposal-count` with the badge catalog so the count lands on the Agents domain tile, or declare the count hand-rendered inside the overview link and drop the `IBadgeCountService` claim.

**[Implementation readiness]** — `pending in another session` has no data source (§ EXPERIENCE.md:284-293; DESIGN.md:351)  
The advisory lock lives in the submitting tab; `AgentSetupTruthState` carries no submitter or session identity, so a second tab sees only `AuthoritativePending`, which the spine renders as the same-session variant. All nine families hit this.  
Fix: State that the second-tab variant is derived from authoritative pending without a local lock, or add an accepted-by session/actor reference to the contracts-must-grow list with an owning story.

**[Implementation readiness]** — The Conversation-owned Call hexa button must know Agents readiness, and no seam supplies it (§ EXPERIENCE.md:87, :158, :81; DESIGN.md:306; IConversationAgentCallGateway.cs)  
The gateway exposes `RequestCallAsync` and `GetCallStatusAsync` only, yet the button must render `aria-disabled` with the safe blocker whenever `hexa` is not proven callable. Whether the exported panel or the Conversations-side button owns the dialog is also unstated.  
Fix: Add `GetCallabilityAsync(tenant, conversation)` returning the Story 5.5 readiness result and safe blocker to the seam table; state the exported panel is the dialog body and Conversations owns the trigger button.

### Medium (37)

**[Flow coverage]** — FR-3 disable and FR-1 create/enable have no surface action (§ EXPERIENCE.md:51, :152, :212; DESIGN.md:283)  
The `hexa` configuration IA row says only "Configure `hexa` and activate it"; `agent-config-form` names Activation and other writes but never a Disable control, its family, or its confirmation content, and the readiness state `disabled` exists with no way to reach it. The overview assumes `hexa` already exists.  
Fix: Add Disable/Enable to the `agent-config-form` action rail as a named high-risk family (`AgentActivation` covers both directions, or say so), give `high-impact-confirmation` its content line, and state once whether `hexa` is pre-provisioned per tenant or has a not-yet-created state.

**[Flow coverage]** — Regeneration ceiling points at FR-32, which gives no numbers (§ EXPERIENCE.md:152; prd.md:566)  
The row says "documented default and valid range from FR-32", but FR-32 says only "a documented default and a valid range". The reference dangles while the proposal-expiry sibling on the same row carries its numbers.  
Fix: Commit UX-side numbers flagged for Product sign-off, or mark the field "default/range: deferred (owner: Product)" and add it to the reconcile file § 8.

**[Token completeness]** — `FluentButton Appearance.Accent` does not exist in Fluent v5 (§ DESIGN.md:152, :347, :39)  
At the pinned rc.5, `FluentButton.Appearance` is `ButtonAppearance` {Default, Outline, Primary, Subtle, Transparent}. This is the confirm button of all nine high-risk families and contradicts the spine's own Don't. The `brand-accent` note inherits `BadgeColor.Brand`, but its two uses are a button and radio selection chrome, neither of which consumes `BadgeColor`.  
Fix: Replace with `ButtonAppearance.Primary`; reword the `brand-accent` note to the Fluent theme brand tokens as rendered by `ButtonAppearance.Primary` and radio selection, with `BadgeColor.Brand` for badges only.

**[State coverage]** — Detail routes are never bound to `FcAggregateDetailState`; shell `Stale` collides with reserved `Stale` (§ EXPERIENCE.md:345, :308, :198)  
The shell's detail page has states {Loading, Ready, Stale, Degraded, Unauthorized, NotFound, Unavailable}. The spine's one `not available` state spans three shell states, the shell's `Stale` collides with the spine's reserved meaning (evidence past `ValidUntil`), and `catching up` has no shell-state home.  
Fix: A five-row mapping under § List, detail, and deep-link surfaces: `Loading` → cold load; `Unauthorized`/`NotFound`/`Unavailable` → the single `not available` content; shell `Stale` unused, `catching up` renders in `Ready` with the freshness badge; `Degraded` → dependency unavailable.

**[State coverage]** — `nearing expiry` has no start threshold in either spine (§ DESIGN.md:197, :275; EXPERIENCE.md:155, :341; reconcile § 3; .memlog.md:34)  
It is a Warning role, a chip, a grid rendering rule, and a once-per-proposal announcement, but nothing says when it begins. The reconcile committed 10 % of the configured window or 1 h before `ExpiresAt`, whichever is smaller; the spines did not carry it. All four lenses flag this.  
Fix: Add the threshold to the `proposal-state-badge` row or a `nearing expiry` row in § Proposal lifecycle.

**[Inheritance discipline]** — Nav glyph `Search` is reused for two entries, contradicting the no-reuse rule (§ DESIGN.md:179 vs :187; AgentsFrontComposerRegistration.cs:171, :184)  
Operational status and Audit evidence both get `Regular.Size20.Search` while the spine says an entry never reuses another entry's glyph. The shipped registration does the same, so the spine describes drift as if it were the decision.  
Fix: One of the two renders without a glyph until the history glyph request lands; say which.

**[Accessibility]** — The nearing-expiry threshold is a decision without a spine (§ EXPERIENCE.md:150, :341, :365; DESIGN.md:197, :275)  
The badge takes a nearing-expiry flag nothing defines. With a 1-hour window the reconcile rule gives a 6-minute warning for a governed approval, and the timing that the WCAG 2.2.1 essential-exception argument depends on is unstated.  
Fix: Put the threshold sentence in `proposal-queue-grid`, `proposal-state-badge`, and the Timing bullet, and state an explicit floor so a 1-hour window does not produce a warning too short to act on.

**[Accessibility]** — Expiry and warning announcements do not scale on the queue (§ EXPERIENCE.md:340-341, :261, :155)  
`expired` is an alert and the warning is a status event; `Expired` is derived on any read; the queue polls 25 rows sorted nearest-expiry-first. An Approver on the queue at the top of the hour can receive a burst of assertive alerts and up to 25 polite warnings.  
Fix: Scope per-proposal alerts to proposal detail; on the queue announce an aggregate at most once per poll cycle in the status node, never as `role="alert"` unless the focused row is the one that expired.

**[Accessibility]** — `IBadgeCountService` count renders as a bare number; the label rule stays conditional (§ EXPERIENCE.md:165; DESIGN.md:335; FrontComposerNavigation.razor:128-130, :166-168)  
The shell emits the count as an unlabeled `FluentBadge`; `navigation.md` confirms badge labels are shell-owned. The spine's "if the shell exposes no label slot" fallback is the actual V1 behaviour, and a screen-reader user still hears "Agents, 3" on the rail.  
Fix: State the fallback as the V1 rule, say whether Agents suppresses the shell count until the localizable label lands, and keep the FrontComposer request as the deferred item it is.

**[Accessibility]** — The deterministic focus target "status region" shares its name with the live region (§ EXPERIENCE.md:289, :169, :336; DESIGN.md:351)  
A developer can reasonably focus the live-region div, the one element that must never receive focus. The rule also does not require the target to be focusable.  
Fix: Name the target as the item's status cell or badge container with `tabindex="-1"` and an accessible name including item identity and new state; state that the page-level live nodes never receive focus.

**[Accessibility]** — "Polled re-renders never steal focus" is a promise without a mechanism (§ EXPERIENCE.md:155)  
The queue sorts nearest-`ExpiresAt`-first and polls; `FluentDataGrid` keeps a focused cell only when rows are keyed and does nothing to stop a focused row moving to page 2 or off the filter.  
Fix: Bind `ItemKey` to `AgentInteractionId` on both grids; while focus is inside the grid body, defer re-sort and page shifts and announce `{count} proposals changed. Refresh list` with an explicit refresh action.

**[Accessibility]** — Selecting another version while the editor is dirty is unspecified and one keystroke away (§ EXPERIENCE.md:157, :156, :327; DESIGN.md:303, :299)  
Version selection is a radio group whose value changes on arrow keys; nothing says what that does to unsaved edits (overwrite, block, or preserve), and a version change updates the textarea silently.  
Fix: While dirty, version radios are `aria-disabled` with the reason, or the change opens the unsaved-changes confirmation; on change the textarea's accessible name is the version identity and the status node announces `Showing version {n}`.

**[Accessibility]** — `high-impact-confirmation` relies on dialog behaviours Fluent v5 does not document (§ EXPERIENCE.md:168; DESIGN.md:347)  
Fluent documents modal inertness and `PreventDismissOnEscape` but no initial-focus placement or focus restoration, and warns that custom action buttons disable the default Enter and Escape shortcuts. For approval the body renders the full version content, so at 320 px Confirm and Cancel can sit below the fold.  
Fix: State the domain implements initial focus (`AutoFocus` on Cancel) and focus restoration; require `FluentDialogBody FixedHeaderFooter="true"` with a focusable, named scroll region; add Esc-with-custom-actions and Confirm-reachable-at-320-px to the conformance lane.

**[Governance & audit]** — The currency rule is circular on a platform-scoped record (§ EXPERIENCE.md:154, :162)  
The catalog says currency must equal the tenant budget currency; Cost controls say cap currency equals catalog pricing currency. A platform-scoped model has one currency while tenants have many, so "must equal" is evaluated against whichever tenant the operator is in.  
Fix: The tenant budget currency on Cost controls is authoritative; the catalog validates only ISO 4217 well-formedness; a per-tenant mismatch renders `status-important` on that tenant's readiness view and blocks reservations; changing the budget currency after settled spend is a `TenantBudgetUpdate` whose confirmation states settled spend is not converted.

**[Governance & audit]** — Rate-limit refusal has no Agent-call state (§ EXPERIENCE.md:178, :237-239, :166; PRD FR-32)  
FR-32 requires the reason when a call is refused for a per-Party or per-Conversation rate limit, but the table maps only `budget blocked` and `capacity rejected`. A story-dev will fold it into `budget blocked` and point users at a cap that is not the problem.  
Fix: Add `rate limited` (`RateLimited`) with window and scope in the safe reason, `status-severe`, recovery "wait for the window", counted on Operational status; add the value to the must-grow list.

**[Governance & audit]** — The automatic path borrows proposal tokens for posting outcomes (§ EXPERIENCE.md:177-178, :242-244, :439-444; AgentCallOperationStatus.cs)  
UJ-2 says the call enters `PostingPending` and renders `Posted`, but those are `ProposedAgentReplyState` values; `AgentCallOperationStatus` ends at `Generated` and the must-grow list omits the posting tokens Story 6.7 requires.  
Fix: Add `PostingPending`, `Posted`, `PostingFailed` (typed reason, same bounded retry rule) to the `AgentCallOperationStatus` must-grow list and the Agent call table; name the status field carrying the posted `MessageId`.

**[Governance & audit]** — The nearing-expiry threshold is not in the spines (§ EXPERIENCE.md:150, :155; DESIGN.md:197, :275; reconcile § 3)  
Both spines render the chip and pass the flag; the 10 % or 1 hour rule lives only in the reconciliation and memlog.  
Fix: State in the `proposal-state-badge` row: nearing expiry begins at 10 % of the configured window or 1 hour before `ExpiresAt`, whichever is smaller; the flag is server-computed against `ExpiresAt`.

**[Governance & audit]** — Restricted safety categories are unaddressed when the mode switches to Automatic (§ EXPERIENCE.md:151, :161; PRD FR-26)  
Restricted categories are permitted only with Confirmation Response Mode, but switching to Automatic is a separate `AgentSetupMutation` whose confirmation says nothing about them: restricted content either posts automatically or every call fails safety with no visible cause.  
Fix: The `response-mode-toggle` confirmation states any permitted restricted category is blocked while Automatic is active, or the mode change is blocked with a named blocker; `content-safety-policy-editor` shows the effective mode beside each restricted row.

**[Governance & audit]** — Per-Conversation `safety blocked` history is shown to `Agents.Operator` with no Conversation-access rule (§ EXPERIENCE.md:166 vs :167; PRD OQ-18, FR-24)  
Conversation identity plus "permanently blocked for unsafe historical content" is Conversation-derived information about a specific Conversation, visible to an operator who may not be a participant, while `audit-evidence-panel` requires current read access for the same class.  
Fix: The history row shows an opaque Conversation reference and count; name or link renders only with current read access; otherwise `existence only`.

**[Governance & audit]** — The harness removal condition is weaker than Story 6.7's absence requirement (§ EXPERIENCE.md:68, :156, :261; AgentsFrontComposerRegistration.cs:139-147; ProposalDetail.razor:227)  
"Removes or blocks" leaves a blocked route as an alternate entry with an `[Authorize]` surface, which `AlternateInvocationGuardTests` must prove absent. The shipped `Start a new Agent Call` link targets the harness, and the spine never says where it goes once the harness is gone or what policy guards it meanwhile.  
Fix: "Removed: route unregistered and page deleted before Story 6.7 closes"; while it exists the harness carries `Agents.Administrator`, is excluded from `LR-UI-CONFORMANCE`, and writes nothing in production-like profiles; `Start a new Agent Call` navigates to the Source Conversation.

**[Governance & audit]** — `high-impact-confirmation` defines required contents for only three of nine families (§ EXPERIENCE.md:168, :161-164, :152)  
Approval, pricing, and Provider disable get explicit contents; `PolicyPublication`, `TenantBudgetUpdate`, `AgentActivation`, `AgentSetupMutation`, `LegalHold`, `ExportRequest` get the generic sentence, although FR-32 requires prior and new cap values and activation must show the evaluated gate set.  
Fix: Add a per-family required-contents table to the `high-impact-confirmation` row; the rendered contents are the values the audit record will carry.

**[Governance & audit]** — Approver-policy publication is future-only while AD-12 snapshots policy per interaction (§ EXPERIENCE.md:153, :330; ARCHITECTURE-SPINE.md AD-12)  
A revoked approver keeps authority over every pending proposal, and the admin who just published the revocation is told the opposite.  
Fix: The `PolicyPublication` confirmation states pending proposals keep the policy they were created under; the surface shows the count of pending proposals under each prior version with a link to the queue filtered by policy version.

**[Governance & audit]** — Export and deletion have no fail-closed rendering while `EXT-SECRETS-1` is Uncommitted (§ EXPERIENCE.md:164; DESIGN.md:331; external-dependency-register.md § EXT-SECRETS-1)  
Stories 8.2 and 8.3 are blocked on the seam; the panel as written lets a story-dev render enabled Export and Delete buttons that submit into a dependency that cannot resolve keys, and never states artifact expiry or that no plaintext is rendered.  
Fix: Export and deletion render `aria-disabled` with `authority unresolved` treatment naming the dependency while it is not Available; export rows show manifest state, expiry, and download availability; the panel never renders artifact contents or keys.

**[Implementation readiness]** — The mandatory FC-TBL components require a `ViewKey` the spine never assigns (§ EXPERIENCE.md:154-155; datagrid.md:43, :90)  
`FcFilterEmptyState`, `FcFilterResetButton`, `FcExpandInRowDetail`, and `FcStatusFilterChips` all take a `ViewKey`; view-key mismatches fail closed, and Agents has no generated lane to inherit one from.  
Fix: Assign one `ViewKey` per grid (`agents.provider-catalog`, `agents.pending-proposals`) and the `BadgeSlot` mapping for status chips.

**[Implementation readiness]** — `Appearance.Accent` does not exist in Fluent v5 (§ DESIGN.md:152, :347, :357, :367)  
`ButtonAppearance` is Default, Outline, Primary, Subtle, Transparent; `Accent` is the v4 name the spine forbids elsewhere. Every `high-impact-confirmation` and the Approve button reference it.  
Fix: `ButtonAppearance.Primary`.

**[Implementation readiness]** — `FcAggregateDetailPage` has no `Heading`, `HeadingTabIndex`, or `PageTitle` (§ EXPERIENCE.md:345, :407; FcAggregateDetailPage.razor:10; ProposalDetail.razor:15-19)  
Those parameters live on `FcPageHeader`, which the detail page expects the domain to supply; the shipped proposal detail already does it that way.  
Fix: Reword the route-heading rule to "`FcAggregateListPage`, or `FcAggregateDetailPage` with a domain-supplied `FcPageHeader`", focus via `FocusHeadingAsync()`.

**[Implementation readiness]** — The nearing-expiry threshold is not in the spines and no contract carries the flag (§ EXPERIENCE.md:150, :155, :341; DESIGN.md:275; PendingProposalView; ProposalDetailView)  
The rule exists only in the reconcile file and memlog; the views carry no flag, so the client computes it from nothing.  
Fix: Add the threshold sentence to the `proposal-state-badge` row and say it is client-computed from `ExpiresAt` and the configured window.

**[Implementation readiness]** — Regeneration ceiling default and range are undocumented and have no contract (§ EXPERIENCE.md:152; prd.md:565; ProposalDetailView; epics.md Story 5.2)  
The field has no contract, no Story 5.2 acceptance criterion, and the detail view has no regeneration count for the editor's ceiling check.  
Fix: State the numbers in the row (decision or Product deferred item with placeholder); add `RegenerationCount`/`RegenerationCeiling` to the contracts-must-grow list with owner 7.3.

**[Implementation readiness]** — Segregation-of-duties checks need the viewer's Party id; no seam is named (§ EXPERIENCE.md:156)  
Blocking approval when the Approver last edited the version or is the sole Approver of their own call requires comparing `EditorPartyId`/`CallerPartyId` with the current user; the server rejects anyway, but the `aria-disabled` pre-state cannot render.  
Fix: Name the source: a per-version `CanCurrentUserApproveSelectedVersion` flag from Story 7.4, or a current-Party accessor.

**[Implementation readiness]** — Approver policy disclosure category is per row in the spine and per policy in the contract (§ EXPERIENCE.md:153; prd.md:210; AgentApproverPolicy; ApproverPolicySource)  
`AgentApproverPolicy(Sources, DisclosureCategory)` carries one category per policy; `ApproverPolicySource` has none, and Story 5.4 has no instruction to move it.  
Fix: Add `ApproverPolicySource.DisclosureCategory` to "Contracts that must grow" with owner 5.4.

**[Implementation readiness]** — Tenant budget currency has no source before Story 8.4 (§ EXPERIENCE.md:154)  
The pricing mismatch rule cannot be evaluated by Story 5.3, which ships first.  
Fix: Interim rule: before 8.4 render currency as entered with no mismatch state; after 8.4 the budget policy read model supplies the comparison value.

**[Implementation readiness]** — Live-region ownership contradicts shipped components and names no carrier (§ EXPERIENCE.md:336, :310; shipped badge components; FrontComposerShell.razor:146)  
Five shipped badge components each render `role="status"` and panels carry their own regions, so a route has 5 to 10 live regions today; the shell already announces connection loss. No component is named for the two Agents nodes.  
Fix: Name the carrier component, say badges must not carry `role="status"`, and state whether `connection lost` is shell-announced or Agents-announced.

**[Implementation readiness]** — The SignalR nudge has no seam (§ EXPERIENCE.md:195; ARCHITECTURE-SPINE.md; FrontComposer Contracts/Communication)  
The Architecture Spine has no SignalR or polling text, `src/` has no hub client, and FrontComposer exposes `IProjectionChangeDetailNotifier` and `PendingCommandPollingCoordinator`. A dev either ignores the nudge or invents a hub.  
Fix: Name `IProjectionChangeDetailNotifier.ProjectionChangedDetail` as the nudge source filtered by the authoritative projection ids; keep the Architecture pin as the deferred item it is.

**[Implementation readiness]** — The single `not available` deep-link state has no copy key (§ EXPERIENCE.md:69, :308; AgentsResources.resx:32-39)  
Identical copy, status class, and timing are required for unauthorized, foreign-tenant, and non-existent ids; the Voice table has no row and the shipped keys carry different copy per case.  
Fix: Add `Agents.Surface.NotAvailable.Title/Message` EN/FR to the Voice table and bind the detail-page `Unauthorized`/`NotFound` states to it.

**[Implementation readiness]** — Story 6.2 and the Operational status extras are named as owners without acceptance-criteria coverage (§ EXPERIENCE.md:54, :160, :166; epics.md:1571-1620)  
Story 6.2's criteria never mention a UI read model or route; per-tenant blocked counts, per-Conversation safety-blocked history, cost consumption, and projection id/version have no contract and only the 6.1 failure record is named.  
Fix: Record both as deferred items for the epics skill so the owner is a story acceptance criterion, not a spine assertion.

**[Implementation readiness]** — The audit evidence list route is unspecified (§ EXPERIENCE.md:61, :167; DESIGN.md:236; AuditEvidenceResult)  
`/agents/audit` is FullWidth and a list, and also hosts evidence lists for cost and governance, but no list contract, columns, filters, or sort exist; the mandatory grid components cannot apply to a grid with no columns.  
Fix: Declare the list an id-entry surface with no grid, or give it a list contract and owning story.

**[Implementation readiness]** — Provider disable blast radius has no query (§ EXPERIENCE.md:154)  
The confirmation lists in-scope Agents whose callability will be blocked and a cross-tenant count; nothing returns Agents by Provider/model.  
Fix: Name the query, or say the Story 5.5 readiness result supplies it.

### Low (30)

**[Flow coverage]** — `AgentCallOperationStatus.Requested` has no row in the Agent call table (§ EXPERIENCE.md:228-244)  
`submitted` is local only and `authoritative pending` is accepted identity plus projection reference, so which UX state the shipped `Requested` value renders as is unstated.  
Fix: One clause: `Requested` renders as `authoritative pending`.

**[Token completeness]** — `agent-readiness-badge` colour list omits `status-subtle` (§ DESIGN.md:62, :267)  
The frontmatter lists four roles, but the section also renders lifecycle `active` as a `{colors.status-subtle}` chip.  
Fix: Add `{colors.status-subtle}` to the list.

**[Token completeness]** — `rounded` and `colors.*` are note objects, not the spec's literal types (§ DESIGN.md frontmatter)  
Neither is resolver-flattenable. Same convention as the Tenants precedent the spine lists as a source.  
Fix: Note only; keep the convention or convert when Tenants does.

**[Component coverage]** — `ConversationAgentCallPanel` and `conversation-agent-call` are not stated to be the same artifact (§ EXPERIENCE.md:81, :158; DESIGN.md:305)  
The seam names an exported panel while the component carries a kebab name; nothing links them.  
Fix: One clause in the `conversation-agent-call` row: shipped as `ConversationAgentCallPanel`.

**[Bloat & overspecification]** — Component Patterns cells run to 150 to 200 words of unbroken prose (§ EXPERIENCE.md:154, :156, :161, :168)  
The content is real rules, so this is extraction friction, not bloat; the memlog records the column schema as left by decision.  
Fix: Optional: a `### name` subsection with a bullet list per high-density component.

**[Bloat & overspecification]** — Two rules are stated in both spines (§ EXPERIENCE.md:148, :215, :492 and DESIGN.md:267; DESIGN.md:240 and EXPERIENCE.md:346)  
The production-enablement indicator rule appears four times; the accordion heading level rule twice.  
Fix: Keep one statement per spine and reference it.

**[Inheritance discipline]** — PRD FR-18 names seven proposal states; the spine has ten (§ EXPERIENCE.md:248; prd.md:355)  
`Edited`, `Regenerated`, `PostingPending` follow the contract enum but the PRD does not name them; only the `PostFailed`/`PostingFailed` alias is noted.  
Fix: One sentence explaining the fold, and raise PRD/contract alignment with Product.

**[Inheritance discipline]** — `AgentsResourcesParityTests` is named as the enforced gate but does not exist (§ EXPERIENCE.md:134; test/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs)  
The shipped gate is `LocalizationResourceTests`; the spine reads as if the named class exists.  
Fix: Say "`AgentsResourcesParityTests` (today `LocalizationResourceTests`; rename or add)" with the owning story.

**[Accessibility]** — The `aria-disabled` pending control is an attribute, not the Fluent binding that produces it (§ DESIGN.md:351; EXPERIENCE.md:169)  
`FluentButton` exposes `DisabledFocusable="true"` for exactly this; `Loading="true"` removes focusability and would reintroduce the prior critical.  
Fix: Bind `pending-command-indicator` to `DisabledFocusable="true"` and forbid `Loading`/`Disabled` on the activated control.

**[Accessibility]** — Forms state error association but not error content, required marker, or validation timing (§ EXPERIENCE.md:349-354)  
WCAG 3.3.1 and 3.3.3 need each error to name field, fault, and fix; nothing says required fields carry a visible marker or whether validation runs on submit or on blur.  
Fix: Three sentences: error text is field plus fault plus fix as one localized string; required fields show a visible marker; validation on submit with per-field re-validation on blur only after first submit.

**[Accessibility]** — Status badges will be read twice if the icon also carries a label (§ DESIGN.md:207)  
`FluentBadge.IconLabel` sets `aria-label` on the icon, so "checkmark circle, Posted" is the likely output.  
Fix: When visible text is present the glyph is decorative; the accessible name is the text.

**[Accessibility]** — The proposal queue at 320 px has six pinned columns and no stated minimum set (§ DESIGN.md:295; EXPERIENCE.md:385)  
WCAG 1.4.10 allows contained two-dimensional scrolling for data tables, but the spine should claim it and name the surviving columns.  
Fix: Name the 320 px column set (state, expiry, open action) and state the grid scrolls inside its own container, never the page.

**[Accessibility]** — Esc in the Call hexa dialog discards a typed prompt (§ EXPERIENCE.md:327; DESIGN.md:307)  
The Esc rule protects the proposal editor, not the required prompt textarea.  
Fix: Extend the rule to any non-empty text input inside a dialog: confirm before discarding, or keep the draft per session.

**[Accessibility]** — `FcAggregateDetailPage` back link needs a localized `BackLinkLabel` (§ FcAggregateDetailPage (ShowBackLink default true))  
An unset label yields an anchor with no accessible name; HFC1050 does not fire on framework components.  
Fix: Require a localized `BackLinkLabel` or `ShowBackLink="false"` on every detail route.

**[Accessibility]** — No language-of-parts rule for generated and Conversation-derived text (§ EXPERIENCE.md § Accessibility Floor)  
The editor textarea, preview, and evidence show text whose language is the Conversation's, not the UI culture; nothing sets `lang`.  
Fix: Content regions carry `lang` from the Provider result or Conversation language when known; otherwise inherit.

**[Accessibility]** — Regenerate is chargeable but has no double-activation guard (§ EXPERIENCE.md:156, :284)  
Regeneration is not in the nine high-risk families, so it has no pending indicator; Enter twice can queue two chargeable generations.  
Fix: Apply `DisabledFocusable` plus a local pending badge to Regenerate while in flight, without adding it to the confirmation families.

**[Accessibility]** — Type-to-confirm for deletion is left as an undecided conditional (§ EXPERIENCE.md:168)  
"If type-to-confirm is used ... the 3.3.7 exception is recorded" is a conditional with no decision.  
Fix: Decide (recommended: no type-to-confirm; the `DeletionRequest` confirmation names scope and count) and delete the "if".

**[Accessibility]** — Pointer cancellation and motion are inherited without being claimed (§ EXPERIENCE.md § Accessibility Floor)  
Fluent buttons activate on click and `FluentMessageBar` animation is opt-in, so both pass by default, but a developer can drift.  
Fix: Add "no custom `pointerdown`/`mousedown` activation; no `MessageBarAnimation`" to the floor.

**[Governance & audit]** — The override is "bounded" with no bound (§ EXPERIENCE.md:162, :489)  
Actor, justification, scope, and expiry are recorded, but not the amount or maximum, nor where the active override is visible to the Operator.  
Fix: The override carries a numeric ceiling and an expiry shown in the confirmation; Operational status shows the active override with remaining amount and time.

**[Governance & audit]** — Pricing version authorship is unspecified (§ EXPERIENCE.md:154, :168; ProviderCatalog.razor:597)  
FR-4 makes a decreased or reused version a typed rejection; the spine never says whether the operator types it or the server assigns it. The shipped client sends `0`.  
Fix: Server-assigned, displayed as "next pricing version {n}" in the confirmation; a rejection renders `superseded by another decision` with the current version.

**[Governance & audit]** — `Expired` on read does not name the clock (§ EXPERIENCE.md:261, :270)  
A client-clock comparison shows `Expired` early or late and lets Approve stay enabled until `expired at approval`.  
Fix: Compare against the server time carried on the read; the client never uses its own wall clock for governance state.

**[Governance & audit]** — "Operator-only visible" for `ConfigurationReferenceId` is a disclosure category, not a policy (§ EXPERIENCE.md:154, :352)  
The page's only policy is `Agents.Administrator`.  
Fix: Name the policy that may see the masked reference and confirm it is a subset of whoever may mutate.

**[Governance & audit]** — `needs my action` has no state definition (§ EXPERIENCE.md:155, :165)  
A proposal another Approver already moved to `Approved` or `PostingFailed` may still be counted and listed.  
Fix: Define it as `Pending`, `Edited`, `Regenerated` where the viewer is an authorized Approver who did not last edit the selected version, plus `PostingFailed` with retries remaining.

**[Governance & audit]** — `ProposalResolution` reload re-derivation names no projection (§ EXPERIENCE.md:290, :188)  
No projection carries a pending-command notion for proposals; if `proposal-detail` has not caught up the lock is not re-armed and the row looks idle. EventStore concurrency protects the outcome.  
Fix: Name the projection and field per family, or render `catching up` with resolution controls `aria-disabled` whenever the read's projection version is below the last accepted write's expected version.

**[Implementation readiness]** — `aria-disabled` while keeping focus is `FluentButton.DisabledFocusable` in v5; the spine never names it (§ DESIGN.md:155, :351; ProposalRegenerator.razor:25)  
Shipped code uses `Disabled`.  
Fix: Name `DisabledFocusable`.

**[Implementation readiness]** — Parity gate name drift: `AgentsResourcesParityTests` vs shipped `LocalizationResourceTests` (§ EXPERIENCE.md:134; epics.md)  
The spine and epics name a class that does not exist.  
Fix: Say "`AgentsResourcesParityTests` (today `LocalizationResourceTests`; rename or add)".

**[Implementation readiness]** — Shipped drift is overridden without naming the absorbing story (§ reconcile-validation-2026-09-08.md; EXPERIENCE.md § IA rules)  
Polling constants, Launch readiness policy, harness nav entry, queue default sort, version listbox, inline confirmations, `USD`/`0` pricing defaults, per-badge `role="status"` are all correct per spines-win, but a dev cannot tell which story pays.  
Fix: One "Shipped-code corrections" list mapping each item to 5.3 / 5.7 / 7.1 / 7.2 / 8.6.

**[Implementation readiness]** — The Fluent MCP documents a different build than the pin (§ DESIGN.md:166)  
Enum names used in this review were cross-checked against shipped code; any further API claim must be re-verified against `5.0.0-rc.5-26219.1` at build.  
Fix: Note only; the spine already says this.

**[Implementation readiness]** — UJ-1 step 2 "confirms or links the Agent Party identity" has no field or command (§ EXPERIENCE.md:419; agent-config-form inventory)  
Only `HasPartyIdentity`/`MissingPartyIdentity` exist.  
Fix: State it is a read-only status with a blocker, or name the command.

**[Implementation readiness]** — `PricingVersion` assignment is unspecified (§ EXPERIENCE.md:154; ProviderCatalog.razor:597)  
Client-supplied vs server-assigned is unstated; the shipped client sends `0`.  
Fix: Say server-assigned; the client sends none.

## Mechanical notes

- Both spines carry `name`, `description`, `status: final`, `created`, `updated: 2026-09-08`, `sources`; all 33 source paths resolve. DESIGN.md lists `./reconcile-validation-2026-09-08.md` as a source; it is a decision record, not a product source.
- All `{colors.*}`, `{spacing.*}`, `{typography.*}` references resolve (DESIGN 19 distinct, EXPERIENCE 7 distinct). Every FrontComposer name cited resolves in the submodule; `RegistryRevision` and `EnvironmentProfile` resolve only in the readiness register, consistent with the contracts-that-must-grow block.
- Every Fluent v5 component type and enum cited exists at the pin, except `FluentButton Appearance.Accent` (v4 name; should be `ButtonAppearance.Primary`).
- Reconcile § 3 commitments not carried into the spines: the `nearing expiry` threshold (flagged by all four lenses) and "pricing version increases strictly".
- Shipped-code drift the spines supersede (documented, not a spine defect): harness route still registered at nav order 4; nine entries instead of twelve; Launch readiness still under `Agents.Administrator`; polling 250 ms/5 s and 200 ms/8 s; queue default sort `CreatedAt` desc; version list as a custom listbox; inline confirmations; `USD`/`0` pricing defaults; per-badge `role="status"`.
- Name splits: `DevMode`/`DeveloperBoard` (one glyph); `ConversationAgentCallPanel`/`conversation-agent-call`; `AgentsResourcesParityTests`/`LocalizationResourceTests`. `Conversation Facilitator`, `PostingFailed`, the freshness vocabulary, and the ten `ProposedAgentReplyState` values are consistent across spines, PRD, reconcile, and shipped enums.
- No Mermaid used; the single text fence is closed. No mockups, wireframes, or imports exist; spine-only and spines-win are each stated once.
- Regression against the 2026-09-08 pre-update run: 85 prior findings resolved, 14 partially resolved, 0 not resolved (rubric 26/1/0, accessibility 18/3/0, governance 21/5/0, implementation readiness 20/5/0). All four prior criticals are closed. The prior run's files are archived under `.working/validation-2026-09-08-pre-update/`.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-governance.md`
- `review-implementation-readiness.md`
- prior run archived under `.working/validation-2026-09-08-pre-update/`
