# Reviewer Gate — Brownfield Drift Review v6 (2026-09-09)

**Verdict: FAILS TO RATIFY, fixable.** The spine text is internally consistent with reality where it
matters most for this pass: the previously-flagged tracking-integrity problem (`sprint-status.yaml`
marking Story 5.2 "done" against unmet amended acceptance criteria) is now corrected, and no code has
changed since the v3 review (`git log --oneline a191d24..HEAD -- src/ test/` is empty). All Critical/High
items from v3 (AD-2, AD-4, AD-29, AD-30, structural seed) remain open in code exactly as before, but they
remain correctly described as pending, already-tracked implementation debt — with AD-2's gap now also
formally logged as `NC-5.3-PLATFORM-CATALOG-SCOPE` in `external-dependency-register.md`, which is new,
positive tracking-hygiene since v3. This pass found no NEW code-vs-spine contradiction and no
tracking-integrity regression; it is scored "fixable, fails to ratify" only because the underlying
Critical/High implementation debt (still uncorrected in code) means the spine cannot be marked ratified as
current *implemented* reality, only as current *intended* reality — a distinction the spine itself already
draws correctly in most places.

**Method:** Read the full spine (772 lines, current working-tree version, including today's uncommitted
edits) end to end. Read `reviews/review-2026-09-09-brownfield-drift-v3.md` in full. Read the relevant
`.memlog.md` entries (C-6/C-7, the round-2 validate-gate entry). Read `sprint-status.yaml` in full. Diffed
the spine against its last-committed version (`git diff`) to isolate what changed this round from what
carried over. Re-ran the v3 review's exact code checks (`grep`/`find` for `TenantProviderEnablement`,
`AgentsIdentity`, `ConfigurationVersion` in `AgentActivated`/`AgentDisabled`, structural-seed folders,
`Agents.PlatformOperator` policy) to confirm they are unchanged. Read `external-dependency-register.md` in
full for `EXT-CONV-AI-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, and the new `NC-5.3-PLATFORM-CATALOG-SCOPE`
non-conformance record. Spot-checked the actual Conversations submodule source
(`references/Hexalith.Conversations/src/...`) against AD-6/AD-7's six-seam and membership/removal claims,
and the Conversations/EventStore/Tenants/Parties/FrontComposer `project-context.md` files for any stated
seam contradicting the spine's assumptions.

---

## Critical

### C1 — Tracking-integrity problem from v3 (Story 5.2 done despite unmet AC) — RESOLVED this round

**v3 finding:** `sprint-status.yaml:88` marked `5-2-configure-hexa-through-live-eventstore-operations: done`
while its own amended acceptance criteria (`ConfigurationVersion` lifecycle bump,
`AgentLifecycleConfigurationVersionTests`) were unmet in code.

**Current state:** `_bmad-output/implementation-artifacts/sprint-status.yaml` line for Story 5.2 now reads:

> `5-2-configure-hexa-through-live-eventstore-operations: in-progress  # Corrected 2026-09-09 (architecture update pass, C-7): amended AC require the ConfigurationVersion lifecycle bump and AgentLifecycleConfigurationVersionTests, neither of which exist in code (DW-4); was incorrectly marked done.`

This matches `.memlog.md` line 183 (C-6/C-7 decision entry): *"sprint-status.yaml:88 marks Story 5.2 done
against unmet amended acceptance criteria ... corrected to in-progress in the same change."* No other story
in `sprint-status.yaml` shows a similar "done"-but-unmet-AC pattern (checked every `done` line against the
code-existence checks below and against the v3/v2 review scope) — this was the only tracking-integrity
defect identified across all prior rounds, and it is fixed.

**Disposition:** Not a current finding. Recorded here for continuity since it was the headline item the
task brief asked this round to re-verify.

---

## High (already-tracked implementation debt, unchanged since v3 — informational)

### H1 — AD-2 platform-scoped `ProviderCatalog` / `TenantProviderEnablement` split absent

`src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs:17` still documents itself as
"tenant-scoped," and `grep -rln "TenantProviderEnablement" src/ test/` returns zero hits — unchanged from
v3. **New corroboration this round:** `external-dependency-register.md` now carries a dedicated
`NC-5.3-PLATFORM-CATALOG-SCOPE` "Known Consumer Non-Conformance" record (status `Open`, owner "Agents
Runtime Maintainer," affected Story 5.3) stating exactly this gap and its migration disposition
(`MigratedFrom`-tagged idempotent migration, frozen legacy streams). This is a positive tracking-hygiene
improvement, not a new drift — it makes the already-tracked debt independently checkable outside the spine
and `sprint-status.yaml`. No spine amendment needed.

### H2 — AD-4 `ConfigurationVersion` not bumped by `AgentActivated`/`AgentDisabled`

`src/Hexalith.Agents/Agent/AgentAggregate.cs:267,298` — `AgentActivated`/`AgentDisabled` still carry no
`ConfigurationVersion` argument, unlike `AgentPartyIdentityLinked` at line 346. Unchanged from v3. Tracked
by DW-4 and Story 5.2 (now correctly `in-progress`, see resolved C1 above). No spine amendment needed.

### H3 — AD-29 single `AgentsIdentity` canonicalizer absent

`grep -rln "AgentsIdentity" src/ test/` → zero hits; the five separate identity helper classes under
`Hexalith.Agents.Server/Application/AgentInteractions/` remain unchanged from v3. Tracked by Stories
6.4/7.1–7.3. No spine amendment needed.

### H4 — Structural seed: `Server/Aggregates`, `Application/Tools`, missing `IntegrationTests` project

`src/Hexalith.Agents.Server/Aggregates/README.md` and `src/Hexalith.Agents.Server/Application/Tools/.gitkeep`
still exist; `find test -maxdepth 1 -type d` still shows no `Hexalith.Agents.IntegrationTests`. Unchanged
from v3. Tracked by Story 5.6. No spine amendment needed.

### H5 — AD-30 HMAC-tagged trusted extensions vs. plain string-equality gate; `Agents.PlatformOperator` policy absent

`src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs` still defines only four policy
constants (`AgentsAdministratorPolicy`, `AgentsApproverPolicy`, `AgentsOperatorPolicy`,
`AgentsAuditOperatorPolicy`); no `Agents.PlatformOperator`/`PlatformOperatorPolicy` anywhere in `src/`or
`test/`. `ProviderCatalogAggregate.cs`'s `IsProviderAdmin` gate is still plain string equality, no HMAC.
Unchanged from v3 (H3/M3 there). Tracked by Stories 5.3/5.4/5.5. No spine amendment needed.

---

## Medium

### M1 — Spine wording clarified this round on three points that v3/v4 flagged; no residual defect found

Diffing the working-tree spine against its last commit shows three targeted prose edits made this round,
all of which resolve items named in the memlog's round-2 validate-gate entry rather than introduce new
claims:

- AD-16/UX section: `AgentReadinessStatus` growth states are now described as "sibling fields... not
  alternate values of one enum, per AD-10's composite-wrapper fix" — this removes the ambiguity AD-10
  itself already fixed but that AD-16's cross-reference had not yet echoed. Consistent, not contradictory,
  with AD-10's own text (line 166 in the earlier read).
- AD-22 (data governance): the `AuditInspection` "case" scope is now defined ("an Inspector-assigned
  free-text identifier grouping multiple named Conversations... always wider than one Conversation and so
  always requiring Platform Operator approval") and the second-party TAA computation is now explicitly
  keyed to "the specific Tenant Agent Administrator principal(s)... a historical fact about who held the
  role during the calls, not about who holds it now," closing a live/historical-principal ambiguity.
  Self-consistent with the rest of AD-22's text.
- Stack table / ARCH-A-4: the .NET SDK `10.0.3xx` out-of-servicing claim is now dated and sourced precisely
  (10.0.303 in the 2026-08-11 bundle, no `10.0.3xx` successor in the 2026-09-08 bundle) rather than
  asserted loosely, and ARCH-A-4's target column now explicitly escalates ahead of Story 5.6 on a security
  advisory. This is a factual tightening; not independently re-verified against the live dotnet/core
  release notes in this pass (no network fetch was in scope for a code/tracking-focused lens), but the
  claim's internal shape (dates, bundle numbers) is consistent with the source already listed in the
  spine's own `sources:` frontmatter (`github.com/dotnet/core` release notes, added this round).

No residual contradiction found in any of the three edited passages against the rest of the document.

### M2 — `EXT-CONV-AI-1` seam is more architecturally scaffolded in Conversations than "Uncommitted" might suggest, but no contradiction

Spot-checking `references/Hexalith.Conversations/src/`: `ParticipantType.AiAgent` (canonical wire value
`"AIAgent"`, `Hexalith.Conversations.Contracts/Participants/ParticipantType.cs`) and
`ParticipantRole.Facilitator`/`Member`/`Observer`
(`Hexalith.Conversations.Contracts/Participants/ParticipantRole.cs`) already exist, along with
`AddParticipantCommand`/`ParticipantAddedDomainEvent`/`AddParticipantValidation.cs`. However,
`grep -rln "RemoveParticipant\|ParticipantRemoved"` and `grep -rln "ConversationDeleted\|PrincipalRemovedFromConversation"`
over the same tree return **zero hits** — the AI-participant removal path (seam 1's second half) and the
typed `ConversationDeleted`/`PrincipalRemovedFromConversation` existence-read answers (seams 2 and 5) do not
exist yet in Conversations. This is exactly consistent with `external-dependency-register.md`'s
`EXT-CONV-AI-1` record, which is `Uncommitted` (owner "Conversations Maintainer," `TargetVersionOrCommit:
TBD`) and explicitly lists all six required seam pieces as not yet committed. AD-6/AD-7's text already
treats `EXT-CONV-AI-1` as an uncommitted, blocking dependency ("An `Uncommitted`... record blocks consuming
stories and runtime callability"), so the partial existence of membership/role types with no removal or
typed-deletion support is not a contradiction of the spine — it is the expected shape of a dependency that
is genuinely partially built and correctly marked `Uncommitted` rather than `Committed`. No spine amendment
needed; flagged here only as informational confirmation, not a finding requiring action.

### M3 — Sibling `project-context.md` files carry no Agents-specific seam content to check against

`references/Hexalith.Conversations/_bmad-output/project-context.md`,
`references/Hexalith.EventStore/_bmad-output/project-context.md`,
`references/Hexalith.Tenants/_bmad-output/project-context.md`,
`references/Hexalith.Parties/_bmad-output/project-context.md`, and
`references/Hexalith.FrontComposer/_bmad-output/project-context.md` all exist and were read in full (115–410
lines each). None mentions `EXT-CONV-AI-1`, `AiAgent`, `SecretReference`/`ConfigurationReferenceId`,
`TenantProviderEnablement`, `IProjectionChangeDetailNotifier`, or the `Agents.*` FrontComposer policy names.
These files are generic repo-conventions/pitfalls documents (AGENTS.md-style), not API contract specs, so
their silence on Agents-specific seams is expected and not evidence of a conflicting claim. `EXT-SECRETS-1`
is correctly Platform-owned with `Repository: TBD` (not `Hexalith.EventStore`), which explains why
EventStore's project-context has no secret-handling content — no contradiction. No spine amendment needed.

---

## Low

### L1 — No further new drift found

Extended the v3 review's scope (which covered AD-2/AD-4/AD-29/AD-30/structural-seed plus AD-15/AD-16/AD-19)
into the register's `Known Consumer Non-Conformance` section and the five sibling `project-context.md`
files. No additional code-vs-spine or tracking-vs-code contradictions found beyond the already-tracked
H1–H5 items above, which are unchanged from v3.

### L2 — Confirmed unchanged since v3 (byte-identical code)

`git log --oneline a191d24..HEAD -- src/ test/` returns empty — no commit has touched `src/` or `test/`
since the commit v3 reviewed. Every code-level claim in v3 (submodule pins, version pins, no AppHost, gate
ids, matrix version) still holds by construction and was not re-verified byte-for-byte in this pass beyond
the specific H1–H5 checks re-run above.

---

## Summary Table

| # | Severity | AD/Section | Status | One-liner |
|---|---|---|---|---|
| C1 | Critical (resolved) | tracking | **fixed this round** | Story 5.2 corrected from `done` to `in-progress` with C-7 annotation matching DW-4/unmet AC |
| H1 | High | AD-2 | already-tracked, unchanged | Tenant-scoped `ProviderCatalog`; `TenantProviderEnablement` absent; now also logged as `NC-5.3-PLATFORM-CATALOG-SCOPE` |
| H2 | High | AD-4 | already-tracked, unchanged | `ConfigurationVersion` not bumped by `AgentActivated`/`AgentDisabled` |
| H3 | High | AD-29 | already-tracked, unchanged | No `AgentsIdentity` canonicalizer; 5+ separate identity helpers |
| H4 | High | Structural Seed | already-tracked, unchanged | `Server/Aggregates`, `Application/Tools` present; `IntegrationTests` project absent |
| H5 | High | AD-30 | already-tracked, unchanged | Plain string-equality provider-admin gate, no HMAC; `Agents.PlatformOperator` policy absent |
| M1 | Medium | AD-16/AD-22/Stack | confirmed consistent | This round's 3 prose edits resolve prior ambiguities without introducing contradictions |
| M2 | Medium | AD-6/AD-7 | confirmed consistent | `EXT-CONV-AI-1` partially scaffolded in Conversations (membership/role types) but removal/deletion-signal seams absent — matches its `Uncommitted` status, no contradiction |
| M3 | Medium | — | confirmed consistent | Sibling `project-context.md` files carry no Agents seam content to contradict; `EXT-SECRETS-1` correctly Platform-owned, not EventStore |
| L1 | Low | — | confirmed | No further new drift found |
| L2 | Low | — | confirmed | Code byte-identical since v3's checkout |

## Verdict Rationale

This is scored **FAILS TO RATIFY, fixable** rather than PASS because the spine's `status: final` claim
still sits atop five open Critical/High implementation gaps (H1–H5) that make it inaccurate to say the
spine *is* the shipped system today — only that it correctly describes the target the shipped system has
not yet reached, with each gap already routed to a named backlog story. Per this lens's brief (informational
brownfield debt vs. genuine new drift), none of H1–H5 requires a spine text change: the spine's own words
already describe these as pending. The one genuine defect this lens exists to catch — the sprint tracker
contradicting the spine/code state — has been fixed. Recommend the gate treat this document as ratified for
*intent* and continue gating actual "final"/production-ready status on Stories 5.2–5.6 landing the named
AD-2/AD-4/AD-29/AD-30/structural-seed corrections, exactly as `sprint-change-proposal-2026-09-09.md` and the
register's `NC-5.3-PLATFORM-CATALOG-SCOPE` record already plan.

## Files Consulted

- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (full, working tree, 772 lines) and its `git diff` against the last commit
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/reviews/review-2026-09-09-brownfield-drift-v3.md` (full)
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/.memlog.md` (C-6/C-7 and round-2 validate-gate entries)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (full)
- `_bmad-output/planning-artifacts/external-dependency-register.md` (`EXT-CONV-AI-1`, `EXT-CONV-UI-1`, `EXT-HOST-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, `Known Consumer Non-Conformance` section)
- `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs`
- `src/Hexalith.Agents/Agent/AgentAggregate.cs`
- `src/Hexalith.Agents.Server/Application/AgentInteractions/*.cs` (identity helpers)
- `src/Hexalith.Agents.Server/Aggregates/README.md`, `src/Hexalith.Agents.Server/Application/Tools/.gitkeep`
- `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs`
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Participants/ParticipantType.cs`, `ParticipantRole.cs`
- `references/Hexalith.Conversations/src/Hexalith.Conversations/Validation/AddParticipantValidation.cs`
- `references/Hexalith.Conversations/_bmad-output/project-context.md`
- `references/Hexalith.EventStore/_bmad-output/project-context.md`
- `references/Hexalith.Tenants/_bmad-output/project-context.md`
- `references/Hexalith.Parties/_bmad-output/project-context.md`
- `references/Hexalith.FrontComposer/_bmad-output/project-context.md`
- `git log --oneline a191d24..HEAD -- src/ test/`, `git diff --stat -- ARCHITECTURE-SPINE.md`, `find src test -maxdepth 3 -type d`
