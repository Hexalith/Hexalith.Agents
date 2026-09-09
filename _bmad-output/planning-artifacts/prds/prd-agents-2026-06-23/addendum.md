# Addendum: Hexalith Agents

These notes are contextual only. They inform positioning and risk awareness, and are not authoritative: where they differ from `prd.md` or the product brief, those documents govern.

## Source Inputs

- Product brief: `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/briefs/brief-agents-2026-06-23/brief.md`
- 2026-09-09 reconciliation inputs, in this folder: `change-extract-validation-2026-09-08.md`, `reconcile-validation-2026-09-09.md`, `review-consistency-2026-09-09.md`, `review-rubric.md`, `review-adversarial-general.md`, and `review-implementation-drift.md`
- Second 2026-09-09 update input, in this folder: `validation-report.md` (run 2026-09-09T07:17Z, grade Poor) with its reviewer files archived as `review-rubric-2026-09-09-validate.md`, `review-adversarial-general-2026-09-09-validate.md`, `review-implementation-drift-2026-09-09-validate.md`, and `review-consistency-2026-09-09-validate.md`

## Discovery Research Notes

- Slack AI focuses on summarizing channels, threads, and DMs, and on generating recaps, with workspace-level admin controls for AI features. Hexalith Agents should differentiate through an explicit, named AI participant identity, tenant-safe Conversation attribution, and confirmation workflows rather than generic summarization. Source: https://slack.com/help/articles/25076892548883-Guide-to-AI-features-in-Slack and https://slack.com/help/articles/28244420881555-Manage-access-to-AI-features-in-Slack
- Microsoft 365 Copilot in Teams supports meeting summaries, action items, and live or post-meeting questions. Hexalith Agents is closer to governed participation in asynchronous Conversations than to meeting assistance, which is why the PRD defines Source Conversation context, caller identity, and posting authority precisely — as the Conversation Context Policy, the Agent Call record, and the Approver Policy resolved through the Conversation Facilitator. Source: https://support.microsoft.com/en-us/teams/copilot/catch-up-on-meetings-with-microsoft-365-copilot-in-teams
- Zoom AI Companion includes chat summarization and message composition using Conversation context. Hexalith Agents' Confirmation Response Mode needs a sharper approval boundary: Proposed Agent Replies are not Conversation Messages until approved. Source: https://support.zoom.com/hc/en/article?id=zm_kb&sysparm_article=KB0057623
- Atlassian Rovo Agents are configurable AI teammates with knowledge sources and plugins. They can be used in chat, automation rules, and editing workflows. Hexalith Agents V1 deliberately defers tools, long-term memory, ambient triggers, and project/folder activation to prove governed participation first. Source: https://support.atlassian.com/rovo/docs/agents/

## Options Considered in the 2026-09-09 Reconciliation

Rejected alternatives from the validation reconciliation, kept here so downstream readers know what was weighed. `prd.md` governs, and the OQ, SM, SM-C, A, FR, EXT, and RQ identifiers below resolve to its registers.

- **SM-3 and the `RQ-1` gate.** Three repairs competed:
  - keep SM-3 as a gate metric re-expressed as human-decision latency;
  - keep it as a gate metric defined relative to each Agent's expiry ("decision within min(expiry, 26 hours)");
  - cap the OQ-3 expiry range at 24 hours for launch tenants.

  All three left a metric in the gate that only real usage can produce. The PRD instead splits pre-enablement gate metrics from launch-health metrics (OQ-22), keeps SM-3 as a launch-health primary defined as human-decision latency, and moves the expiry rate to counter-metric SM-C5. The 80%/24-hour threshold is provisional (A-13).
- **Status label.** Relabeling the PRD as `approved-pending` or `draft` was rejected because the defects that justified it (OQ-19, an undated OQ-18) were resolved in the same run; §0 now separates requirements authority from build readiness instead.
- **Safe Context Budget.** Aligning the PRD down to the shipped formula (input limit minus output allowance) was rejected because it reintroduces post-reservation Provider overflow; the PRD formula was extended to include the prompt and framing, and the shipped formula declared non-conformant.
- **Blocked context mode.** Recording blocked calls as context mode `Unknown` plus a reason was rejected in favor of an additive `Blocked` value, so the FR-23 sentinel stays meaningless everywhere.
- **Facilitator wire name.** Adding `ConversationFacilitator` and deprecating `ConversationOwner` was rejected as churn with no behavioral gain; the legacy identifier is recorded and the no-owner rule scoped to human-readable text.
- **Membership seam.** A transitional "verified at posting until EXT-CONV-AI-1 lands" clause was rejected; the PRD states the target sequence and names posting-time-only verification non-conformant, leaving the interim to the dependency register.
- **Removal notification.** Requiring Hexalith.Conversations to notify Agents of participant removal was rejected in favor of an Agents-owned per-Conversation block, which needs no new Conversations seam.
- **Disclosure category.** Aligning the PRD to the code's one-required-category-per-policy shape was rejected (a medium-severity drift finding, left unapplied); the PRD keeps the per-source rule with an operator-only default as a requirement.

## Options Considered — Second 2026-09-09 Update

Rejected alternatives (and one deferral) from the decisions recorded as OQ-24 through OQ-30, raised while applying the validation report and at the reviewer gate on the updated PRD. As above, `prd.md` governs and its identifiers resolve to its registers; in addition, the Spine is the Architecture Spine cited in §8.1, AD identifiers are its decisions, and M identifiers are medium-severity findings from the adversarial reviewer.

- **Freezing `ExpiresAt` while the kill switch is pulled.** The adversarial reviewer proposed freezing expiry and the retry window together. Rejected in favor of the Spine AD-12 rule (retry clock paused, expiry keeps running): a proposal that waits out a suspension is stale by construction, and letting it expire avoids the weeks-old-content problem the same review raised elsewhere (its deferred M-12).
- **A `Suspended` lifecycle state.** Rejected in favor of a `Suspended` status reported by FR-25 while the lifecycle state stays `Draft`/`Active`/`Disabled`: the kill switch is a tenant fact, not an Agent fact, and a lifecycle value would have to be cleared on release and could collide with a genuine `Disabled`.
- **Amending the Spine so the Tenant Agent Administrator creates `hexa`.** Rejected; FR-1 and FR-33 were aligned to the Spine's Platform-principal provisioning instead, because the Spine rule was approved after the FR-1 text and a create-only, idempotent Platform provision is the simpler guarantee of exactly one `hexa` per tenant.
- **Registering the SM-2 event feed as a seventh Conversations seam.** Rejected; the feed is stated as an alternative form of seam 4 so that the A-4 fallback is inside a registered seam without adding a register entry from the PRD side.
- **Deleting the A-4 fallback outright.** Rejected because Spine AD-28 already relies on it.
- **A per-tenant Provider secret reference as an alternative to data-handling terms.** Rejected for V1 (§6.2): the platform-owned account plus a data-handling record and tenant acceptance closes the finding without a new credential path.
- **Scanning human edits at edit time (adversarial M-2) and the other medium-severity findings not co-located with an applied fix.** Deferred, not rejected; see this update's report for the deferred list.
- **Making the hourly re-check cadence non-configurable.** Rejected; a bounded range (A-20) lets qualification environments run it faster without a contract change.
- **Dropping the `Approved → Abandoned` row to match Spine AD-5.** The reviewer gate offered two ways to close the abandon-in-flight hole: make `PostingPending` uninterruptible and keep system abandonment from `Approved` on a detected removal, or delete that row and let the pre-post re-validation route the proposal through `PostingFailed`. The first was chosen: a removed `hexa` can never post, so leaving an approved proposal to fail and then be abandoned by hand adds a step without adding safety. AD-5 is recorded in §8.1 as a Spine defect to correct.
- **Letting `Approved` proposals post through a suspension.** FR-28 and Spine AD-12 said `Approved` proposals "complete or fail on their own terms" under the kill switch; the FR-18 pre-post guard contradicted that. Safety won: an `Approved` proposal waits, consuming no retry attempt, because the SM-4 trigger is a confirmed security event and posting approved content through it defeats the switch. AD-12 is listed for Architecture to correct.
- **A host-asserted availability signal for FR-34.** Rejected after the reviewer gate showed a wrapper around the no-op default could report itself as production; replaced by a seal-unseal-erase attestation checked against the register's committed engine identity and version.
- **Platform-recorded tenant acceptance of Provider data-handling terms.** Rejected because no tenant Party would have acted; acceptance is now a Tenant Agent Administrator action against a named `DataHandlingVersion`, with Platform enablement reduced to permitting selection.
- **Evaluating the kill-switch trigger over the platform cohort for small tenants.** Rejected in favor of a scaled minimum sample (for tenants with fewer than 5 Parties, every calling Party and 20 calls), which keeps the trigger per tenant.
