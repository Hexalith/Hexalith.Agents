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

