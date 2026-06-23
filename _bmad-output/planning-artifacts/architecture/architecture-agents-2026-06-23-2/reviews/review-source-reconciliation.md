# Source Reconciliation Review

Verdict: Pass.

Sources checked:

- `brief-agents-2026-06-23/brief.md`
- `prd-agents-2026-06-23/prd.md`
- `ux-agents-2026-06-23/DESIGN.md`
- `ux-agents-2026-06-23/EXPERIENCE.md`

Coverage:

- `hexa` as first named Agent: covered by conventions, AD-1, AD-15, and Deferred V2/multiple-Agent item.
- Party-based Agent identity: covered by AD-7.
- Explicit conversation-originated invocation: covered by AD-6, AD-11, AD-12, and Deferred invocation affordance.
- Automatic and confirmation modes: covered by AD-4, AD-5, AD-13, and sequence diagram.
- Proposed Agent Reply outside Conversations until approval: covered by AD-5 and AD-6.
- Version preservation for generated, edited, regenerated content: covered by AD-5 and AD-14.
- Provider/model governance: covered by AD-9 and AD-10.
- Admin UI and API/client parity: covered by AD-15.
- Tenant isolation, fail-closed authorization, and dependency uncertainty: covered by AD-12.
- Audit and operational evidence: covered by AD-1, AD-5, AD-13, AD-14, AD-17.
- UX inheritance from FrontComposer/Fluent: covered by AD-15 and Consistency Conventions.

Source tensions surfaced:

- "Conversation owner" is not present in current Conversations public contract. Spine preserves the product intent through a resolver rule and makes V1 facilitator mapping explicit.
- UX invocation pattern is still open. Spine keeps it deferred but binds the normalized backend command.

## 2026-06-23 Dapr Runtime Amendment

Verdict: Pass.

Sources checked:

- User amendment: "agents must use DAPR AI agents and worflows".
- Current `ARCHITECTURE-SPINE.md`.
- Official Dapr Agents, Dapr Workflow .NET SDK, and Dapr AI .NET SDK docs.
- Local package references for Dapr Workflow and Dapr AI in sibling modules.

Coverage:

- Dapr Workflow runtime ownership is covered by AD-18, sequence diagram, Consistency Conventions, Stack, Structural Seed, and Capability To Architecture Map.
- Dapr AI / Dapr Agents usage is covered by AD-18 as adapter/runtime leaves, with SDK types barred from public contracts and EventStore aggregates.
- Existing EventStore domain ownership is preserved by AD-1, AD-3, and AD-18.

Source tensions surfaced:

- Dapr Agents is currently documented as a Python framework while Hexalith Agents is a .NET module. The spine resolves this by requiring the Dapr substrate while deferring exact worker packaging behind an adapter boundary.

## 2026-06-23 Hybrid Runtime Research Amendment

Verdict: Pass.

Sources checked:

- `technical-dapr-ai-agents-research-2026-06-23.md`
- Current `ARCHITECTURE-SPINE.md`
- Primary current sources for Microsoft Agent Framework, NuGet package versions, Dapr Agents, and Dapr `MCPServer`

Coverage:

- Microsoft Agent Framework as the primary .NET agent/workflow layer is covered by the Design Paradigm, AD-18, Stack, and Structural Seed.
- Dapr as the distributed runtime substrate is covered by AD-18 and the Design Paradigm diagram.
- Optional Python Dapr Agents `DurableAgent` workers are covered by AD-18 and Deferred worker packaging.
- The "one durable owner per task" rule is covered by AD-18.
- MCP, Dapr `MCPServer`, A2A, service invocation, pub/sub CloudEvents, and domain commands are covered by AD-19.
- The separate data-plane rule from the research is covered by Consistency Conventions: domain truth, workflow history, agent state, and retrieval/memory state remain separate.
- The research's test guidance is covered by the tightened AD-17 test gate.

Source tensions surfaced:

- The earlier Dapr-only AD-18 was too strong after the hybrid research. It is now amended rather than left as a conflicting rule.
- The user's earlier Dapr runtime preference is still preserved: Dapr remains the sidecar/runtime substrate, Dapr Workflow remains a valid durable owner for deterministic processes, and Dapr Agents remains available as a bounded worker runtime when justified.
