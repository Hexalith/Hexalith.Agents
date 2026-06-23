---
title: Product Brief: Hexalith Agents
status: final
created: 2026-06-23
updated: 2026-06-23
---

# Product Brief: Hexalith Agents

## Executive Summary

Hexalith Agents introduces governed AI participants into the Hexalith conversation ecosystem. Hexalith already supports tenant-scoped multi-party conversations through `Hexalith.Conversations`, with participant identity grounded in `Hexalith.Parties`. The missing capability is a native way for conversation participants to explicitly call a named AI assistant, receive an answer inside the conversation, and preserve identity, authorization, approval, and audit guarantees.

The first agent is `hexa`, a general-purpose AI participant called by Parties inside conversations. In V1, `hexa` answers questions using the full conversation context. It does not use long-term memory, external tools, project content, or folder content. That deliberate boundary keeps the first release focused on the product's core promise: safe, attributable AI participation in conversations before broader autonomous or tool-using behavior is introduced.

Hexalith Agents is not just an LLM wrapper. It manages agent identity, instructions, model and provider configuration, permissions, lifecycle, activation rules, response policy, and audit. It gives agent administrators a controlled path from explicit, human-supervised assistance to future ambient agents that can react to project, folder, and conversation changes.

## Problem

Hexalith Conversations can host multi-party conversations, but participants cannot call a governed AI assistant into those conversations. If a Party needs help interpreting the discussion, asking a contextual question, or getting a draft answer from an AI participant, there is no native Hexalith capability for doing that with tenant-safe identity and controls.

Without Hexalith Agents, AI participation would likely happen through ad hoc integrations outside the conversation model. That would weaken attribution, make approval flows inconsistent, and make it harder to prove who called the agent, which agent answered, who approved the answer, and what finally entered the conversation.

## V1 Solution

V1 enables an agent administrator to create and configure `hexa`, including its durable Party identity and response policy. Once configured, a conversation participant can explicitly call `hexa` inside a conversation. `hexa` reads the full conversation context and produces an answer.

The answer follows the configured response mode:

- In automatic mode, `hexa` posts the answer directly into the conversation as an attributed AI participant.
- In confirmation mode, `hexa` creates a proposed answer outside the conversation. An authorized approver can edit the proposal, request regeneration, or approve it. Only approved answers are added to the conversation.

Unapproved answers are not conversation messages. They belong to a separate approval process that manages proposal state, editing, regeneration, approval, and audit evidence. This keeps `Hexalith.Conversations` as the durable record of actual conversation content while allowing Hexalith Agents to govern draft AI output before it becomes part of that record.

## Users

The primary V1 user is the **agent administrator**. Agent administrators configure agent identity, instructions, model and provider settings, activation behavior, response mode, approver policy, and lifecycle.

The primary runtime user is a **Party participating in a conversation**. That Party explicitly calls `hexa` when they need contextual help.

The approver may be configured as one of:

- the conversation owner;
- the Party that explicitly called the agent;
- a predefined list of Parties.

## Identity and Governance

Every agent has a `PartyId`. Agent creation provisions or links the corresponding Party identity through `Hexalith.Parties`, so AI participation is not anonymous system output. When `hexa` replies, the conversation can attribute the message to a known AI participant.

Tenant isolation is strict. Authorization must govern who can configure agents, call agents, approve proposals, edit proposals, regenerate proposals, and post approved answers. Audit evidence must connect the caller, agent, source conversation, approval path, generated proposal, edits or regenerations, and final posted response.

## V1 Scope

In scope for V1:

- create and configure `hexa` as the first general-purpose agent;
- provision or link the agent's Party identity;
- configure instructions, model and provider settings, activation behavior, response mode, and approver policy;
- explicitly call `hexa` from a conversation;
- answer using the full conversation context;
- post automatic replies as attributed AI participant messages;
- manage proposed replies outside the conversation when confirmation is required;
- allow authorized approvers to edit, regenerate, and approve proposed replies;
- add approved replies to the conversation;
- enforce strict tenant isolation and authorization;
- capture audit evidence for generated and approved agent responses.

Out of scope for V1:

- long-term memory;
- configured tools;
- automatic activation on every conversation change;
- activation from project or folder content changes;
- business-state changes outside adding approved conversation replies;
- agent-to-agent orchestration;
- external channel integration beyond Hexalith Conversations.

## Success Criteria

V1 is successful when:

- an agent administrator can create `hexa` with Party identity and response policy;
- a Party can explicitly call `hexa` in a conversation;
- `hexa` answers using conversation context and appears as an attributed AI participant;
- confirmation mode supports the correct approver editing, regenerating, and approving a proposed reply;
- unauthorized Parties cannot call, approve, or configure agents;
- every agent reply has audit evidence for caller, agent, source conversation, approval path, and final posted response.

## Roadmap Direction

V2 expands Hexalith Agents from explicit conversation assistance to broader context-aware activation. `Hexalith.Memories` becomes the memory management foundation. Instead of always sending the full conversation context, V2 may use a configurable compacted conversation summary.

V2 also expands activation sources beyond explicit conversation calls. Agents may be activated by:

- project content changes through `Hexalith.Projects`;
- folder content changes through `Hexalith.Folders`;
- conversation changes through `Hexalith.Conversations`.

This roadmap should remain secondary until V1 proves the governed participation model: named agent identity, explicit invocation, approval policy, tenant isolation, and auditable conversation replies.

## Open Questions

- What is the exact proposal lifecycle model for generated, edited, regenerated, approved, and abandoned answers?
- Which module owns the approval process runtime and storage?
- How much generated content is preserved for audit when an approver edits before posting?
- What latency and cost controls apply to explicit calls?
- What model and provider configuration is required in V1 versus deferred?
- How should `hexa` be introduced to a conversation: participant membership, mention resolution, or both?
