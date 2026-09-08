# Addendum: Hexalith Agents

These notes are contextual only. They inform positioning and risk awareness, and are not authoritative: where they differ from `prd.md` or the product brief, those documents govern.

## Source Inputs

- Product brief: `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/briefs/brief-agents-2026-06-23/brief.md`

## Discovery Research Notes

- Slack AI focuses on summarizing channels, threads, and DMs, and on generating recaps, with workspace-level admin controls for AI features. Hexalith Agents should differentiate through an explicit, named AI participant identity, tenant-safe Conversation attribution, and confirmation workflows rather than generic summarization. Source: https://slack.com/help/articles/25076892548883-Guide-to-AI-features-in-Slack and https://slack.com/help/articles/28244420881555-Manage-access-to-AI-features-in-Slack
- Microsoft 365 Copilot in Teams supports meeting summaries, action items, and live or post-meeting questions. Hexalith Agents is closer to governed participation in asynchronous Conversations than to meeting assistance, which is why the PRD defines Source Conversation context, caller identity, and posting authority precisely — as the Conversation Context Policy, the Agent Call record, and the Approver Policy resolved through the Conversation Facilitator. Source: https://support.microsoft.com/en-us/teams/copilot/catch-up-on-meetings-with-microsoft-365-copilot-in-teams
- Zoom AI Companion includes chat summarization and message composition using Conversation context. Hexalith Agents' Confirmation Response Mode needs a sharper approval boundary: Proposed Agent Replies are not Conversation Messages until approved. Source: https://support.zoom.com/hc/en/article?id=zm_kb&sysparm_article=KB0057623
- Atlassian Rovo Agents are configurable AI teammates with knowledge sources and plugins. They can be used in chat, automation rules, and editing workflows. Hexalith Agents V1 deliberately defers tools, long-term memory, ambient triggers, and project/folder activation to prove governed participation first. Source: https://support.atlassian.com/rovo/docs/agents/
