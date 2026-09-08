# Addendum: Hexalith Agents

These notes are contextual only. They inform positioning and risk awareness, and are not authoritative: where they differ from `prd.md` or the product brief, those documents govern.

## Source Inputs

- Product brief: `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/briefs/brief-agents-2026-06-23/brief.md`

## Discovery Research Notes

- Slack AI focuses on summarizing channels, threads, and DMs, and on generating recaps, with workspace-level admin controls for AI features. Hexalith Agents should differentiate through an explicit, named AI participant identity, tenant-safe Conversation attribution, and confirmation workflows rather than generic summarization. Source: https://slack.com/help/articles/25076892548883-Guide-to-AI-features-in-Slack and https://slack.com/help/articles/28244420881555-Manage-access-to-AI-features-in-Slack
- Microsoft 365 Copilot in Teams supports meeting summaries, action items, and live or post-meeting questions. Hexalith Agents is closer to governed participation in asynchronous Conversations than to meeting assistance, which is why the PRD defines Source Conversation context, caller identity, and posting authority precisely — as the Conversation Context Policy, the Agent Call record, and the Approver Policy resolved through the Conversation Facilitator. Source: https://support.microsoft.com/en-us/teams/copilot/catch-up-on-meetings-with-microsoft-365-copilot-in-teams
- Zoom AI Companion includes chat summarization and message composition using Conversation context. Hexalith Agents' Confirmation Response Mode needs a sharper approval boundary: Proposed Agent Replies are not Conversation Messages until approved. Source: https://support.zoom.com/hc/en/article?id=zm_kb&sysparm_article=KB0057623
- Atlassian Rovo Agents are configurable AI teammates with knowledge sources and plugins. They can be used in chat, automation rules, and editing workflows. Hexalith Agents V1 deliberately defers tools, long-term memory, ambient triggers, and project/folder activation to prove governed participation first. Source: https://support.atlassian.com/rovo/docs/agents/

## Options Considered In The 2026-09-09 Reconciliation

Rejected alternatives from the validation reconciliation, kept here so downstream readers know what was weighed. `prd.md` governs.

- **SM-3 and the `RQ-1` gate.** Three repairs competed: keep SM-3 as a gate metric re-expressed as human-decision latency; keep it as a gate metric defined relative to each Agent's expiry ("decision within min(expiry, 26 hours)"); or cap the OQ-3 expiry range at 24 hours for launch tenants. All three left a metric in the gate that only real usage can produce. The PRD instead splits pre-enablement gate metrics from launch-health metrics (OQ-22), keeps SM-3 as a launch-health primary defined as human-decision latency, and moves the expiry rate to counter-metric SM-C5. The 80%/24-hour threshold is provisional (A-13).
- **Status label.** Relabeling the PRD `approved-pending` or `draft` was rejected because the defects that justified it (OQ-19, an undated OQ-18) were resolved in the same run; §0 now separates requirements authority from build readiness instead.
- **Safe Context Budget.** Aligning the PRD down to the shipped formula (input limit minus output allowance) was rejected because it reintroduces post-reservation Provider overflow; the formula was extended to include the prompt and framing and the shipped formula declared non-conformant.
- **Blocked context mode.** Recording blocked calls as context mode `Unknown` plus a reason was rejected in favour of an additive `Blocked` value, so the FR-23 sentinel stays meaningless everywhere.
- **Facilitator wire name.** Adding `ConversationFacilitator` and deprecating `ConversationOwner` was rejected as churn with no behavioural gain; the legacy identifier is recorded and the no-owner rule scoped to human-readable text.
- **Membership seam.** A transitional "verified at posting until EXT-CONV-AI-1 lands" clause was rejected; the PRD states the target sequence and names posting-time-only verification non-conformant, leaving the interim to the dependency register.
- **Removal notification.** Requiring Hexalith.Conversations to notify Agents of participant removal was rejected in favour of an Agents-owned per-Conversation block, which needs no new Conversations seam.
- **Disclosure category.** The drift finding that the code holds one required disclosure category per policy, not per source with an operator-only default, was left unapplied (medium severity); the PRD keeps the per-source rule as a requirement.
