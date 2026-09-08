# Consistency Review — PRD Hexalith Agents (post 2026-09-09 batch revision)

Scope: internal contradictions and dangling references only, across §0, §2, §3, §4, §7, §8, §9, §11, §12, §13 of `prd.md` (updated 2026-09-08). Line numbers refer to `prd.md` as reviewed.

Checked clean (no finding): every FR-1..FR-33, OQ-1..OQ-22, SM-1..SM-7, SM-C1..SM-C5, NFR-1..NFR-14, A-1..A-14, UJ-1..UJ-4 and EXT-* identifier referenced in the document is defined; proposal state names and the terminal set are identical in FR-18, §6.1, §9, SM-3, SM-C5, OQ-3; the expiry rule (only `Pending`/`Edited`/`Regenerated` expire; approval freezes expiry) is identical in FR-18 and OQ-3; membership is the last pre-Provider acceptance step in §3, FR-2, OQ-16, and a re-validation in FR-18/FR-21/§8; `ConversationOwner` wire name and Conversation Facilitator label agree in §3, FR-7, OQ-14; RQ-1 inputs vs launch-health metrics agree in §3, §11, §12, FR-28, OQ-19, OQ-22; SM-C2 and SM-C5 both counterbalance the decision-latency SM-3 coherently; `Indeterminate`, the hard-posture list, the deprecate-and-reject register, and the 24 h / 1–72 h hold agree in FR-4, FR-12, FR-23, FR-28, NFR-10, NFR-11, OQ-6, A-8; Safe Context Budget subtracted terms, 10% / 5–25% margin, and `Full`/`Bounded`/`Blocked` agree in §3, FR-9, FR-24, NFR-8, OQ-10, A-5; per-tenant instantiation and tenant-wide response mode agree in §3, UJ-3, FR-6, OQ-20 with no per-Conversation mode text elsewhere.

## Findings

### Substantive contradictions

1. **Content Safety Policy authority: FR-26 vs FR-33.**
   - FR-26 (line 540): "Authorized administrators or release operators can define the active Content Safety Policy for `hexa`."
   - FR-33 (line 431): "Publish or version the Content Safety Policy (FR-26) | Platform Operator with Security approval; a tenant may only add restrictions through a stricter mode-specific policy | Platform".
   - FR-26 grants the Release Operator (and unnamed "administrators") an authority the FR-33 matrix reserves to Platform Operator with Security approval. FR-33 declares itself authoritative (OQ-21), so FR-26 must follow it.
   - Fix: reword FR-26 opening to "The Platform Operator, with Security approval, publishes and versions the active Content Safety Policy for `hexa`; a Tenant Agent Administrator may only add restrictions through a stricter mode-specific policy (FR-33)."

2. **FR-5 "complete selection-eligibility set" omits the per-tenant enablement that FR-33 introduces.**
   - FR-5 (line 187): "The system validates the complete selection-eligibility set before Agent activation: the Provider and model are enabled; the Provider is configured; ... and its `CapabilityVersion` is not regressed."
   - FR-33 (lines 424–425): "Enable a Provider/model for a tenant | Platform Operator | Tenant. A tenant sees and can select only Providers/models enabled for it" and "Provider/model selection among tenant-enabled options".
   - A set described as "complete" and testable does not include "enabled for this tenant", so an Agent could pass FR-5 activation on a globally enabled model that the tenant may not select.
   - Fix: add "the Provider and model are enabled for the Agent's tenant (FR-33)" to the FR-5 eligibility list, tagged `[ASSUMPTION]` with A-10.

3. **Who configures cost caps, rate limits and the regeneration ceiling: FR-32 vs FR-33.**
   - FR-32 (line 598): "Authorized administrators or release operators can configure the per-tenant monthly and per-call cost caps, the per-Party and per-Conversation rate limits, and the per-proposal regeneration ceiling".
   - FR-33 (line 429): "Platform Operator or Release Operator sets them; the Tenant Agent Administrator may lower a cap or limit for their own tenant and never raise it, so the boundary is not self-set"; and (line 425) the regeneration ceiling is listed under "Configure `hexa` ... | Tenant Agent Administrator".
   - FR-32 lets undifferentiated "administrators" set caps with no lower-only constraint, which is exactly the self-set boundary FR-33 forbids; and it assigns the regeneration ceiling to the same actors that FR-33 assigns to the Tenant Agent Administrator alone.
   - Fix: split FR-32's opening sentence: caps and rate limits are set by the Platform Operator or Release Operator and may be lowered (never raised) by the Tenant Agent Administrator; the regeneration ceiling is configured by the Tenant Agent Administrator (FR-33).

### Dangling references

4. **§8.1 "Where" column names sections that carry no `[ASSUMPTION]` tag.** §8.1 (line 702) states "Every `[ASSUMPTION]` tag in this PRD is listed here with ... Where", and the review brief requires each Where entry to point at a tagged section. These entries do not:
   - A-2 → "FR-11, FR-17, NFR-11": none of FR-11 (lines 276–286), FR-17 (351–362), NFR-11 (670) carries a tag; only §8 (line 693) does.
   - A-3 → "OQ-14": OQ-14 (line 824) says "is assumption A-3" but carries no tag.
   - A-4 → "SM-2": SM-2 (line 794) carries no tag.
   - A-6 → "FR-32": the FR-32 regeneration-ceiling bullet (line 607) carries no tag.
   - A-11 → "FR-8": FR-8 (lines 234–242) carries no tag.
   - A-13 → "OQ-11": OQ-11 (line 821) says "(A-13)" but carries no tag.
   - A-14 → "§13": OQ-18 (line 828) says "(A-14)" but carries no tag.
   - Fix: either add the `[ASSUMPTION: ... (A-n)]` tag at each named location, or trim each Where cell to the locations that actually carry the tag.

5. **FR-18 "audited administrative retry" has no FR-33 row.**
   - FR-18 (line 373): "the proposal remains `PostingFailed` ... until an authorized Approver abandons it or an audited administrative retry succeeds."
   - FR-33 (line 419): "Every authorization rule in this PRD resolves to one of six roles at a stated scope." No row covers an administrative retry of a `PostingFailed` proposal.
   - Fix: add an FR-33 row "Retry a `PostingFailed` proposal after the retry budget is exhausted (FR-18) | Tenant Agent Administrator `[ASSUMPTION]` | Proposal", or name the role inline in FR-18.

6. **FR-25 / FR-10 operational-status inspection has no FR-33 row.**
   - FR-25 (lines 515–516): "Authorized administrators can identify whether `hexa` is callable for a tenant." / "Authorized administrators can distinguish configuration errors, authorization failures, ..." and FR-10 (line 266): "visible to authorized administrators or callers."
   - FR-33 (line 419): "Every authorization rule in this PRD resolves to one of six roles" — the matrix has no row for status inspection (FR-25) or failed-call visibility (FR-10).
   - Fix: add an FR-33 row "Inspect operational status and failed-call outcomes (FR-10, FR-25) | Tenant Agent Administrator, Release Operator, and the calling Party for their own calls | Tenant" (tag `[ASSUMPTION]`).

7. **FR-4 cites FR-32 as the requirement that reserves against pricing.**
   - FR-4 (line 178): "Pricing metadata is the input to the cost estimation that FR-32 and OQ-6 reserve against".
   - FR-32 (line 596 ff.) configures caps, rate limits and the regeneration ceiling; reservation is defined in FR-28 (line 587: "The system reserves the maximum estimated attempt cost before Provider invocation") and OQ-6.
   - Fix: change "FR-32 and OQ-6" to "FR-28 and OQ-6".

### Wording drift

8. **"Agent Administrator" vs "Tenant Agent Administrator".** FR-33 (lines 425–430), A-9 (line 714), OQ-16 (line 826) use "Tenant Agent Administrator"; §2.1 (line 30), UJ-1 (line 46), §3 glossary (line 81), FR-1 through FR-7 (lines 125–211), FR-18 (line 371) use "Agent Administrator", and FR-2's own assumption text (line 146) reads "removal authority is Agent Administrator plus Conversation Facilitator" while its index row A-9 reads "Tenant Agent Administrator plus Conversation Facilitator". FR-33 maps no role named "Agent Administrator".
   - Fix: add to the §3 "Agent Administrator" entry: "Its FR-33 role name is Tenant Agent Administrator; the two names are the same role", and align the FR-2 assumption text with A-9.

9. **"Authorized administrators" administer the Global Providers Aggregate (FR-4, FR-22) where FR-33 names the Platform Operator.**
   - FR-4 (line 167): "Authorized administrators can configure the Global Providers Aggregate"; FR-22 (line 452): "authorized administrators to manage Global Providers Aggregate entries, configure `hexa`".
   - FR-33 (line 423): "Administer the Global Providers Aggregate ... | Platform Operator | Platform".
   - Fix: FR-4 → "The Platform Operator can configure ..."; FR-22 → "allows the Platform Operator to manage Global Providers Aggregate entries and the Tenant Agent Administrator to configure `hexa` ...".

10. **UJ-1 and SM-1 have a tenant actor enabling Providers, which FR-33 makes a Platform Operator action.**
    - UJ-1 (line 47): "Nora ... with permission to manage Agent configuration and Provider settings"; SM-1 (line 784): "At least one launch tenant configures `hexa`, enables a Provider/model, and records successful Agent Calls".
    - FR-33 (line 424): "Enable a Provider/model for a tenant | Platform Operator".
    - Fix: UJ-1 → "permission to manage Agent configuration and select among tenant-enabled Providers/models"; SM-1 → "configures `hexa`, has a Provider/model enabled for it, and records ...".

11. **FR-33 says "six roles" but its matrix names seven actors plus Security.**
    - FR-33 (line 419): "resolves to one of six roles"; the "Who" column names Platform Operator, Tenant Agent Administrator, Conversation Participant, Approver, Conversation Facilitator, Release Operator, Compliance Inspector, and "Security approval" (line 431).
    - Fix: state the role list explicitly after "six roles" (or correct the count), and say whether Conversation Facilitator and Security are Agents roles or external authorities.

12. **FR-33 "Configure `hexa`" row omits the proposal expiry duration that FR-18 assigns to the Agent Administrator.**
    - FR-18 (line 371): "An Agent Administrator may configure a duration from 1 hour through 30 days"; OQ-3 (line 813): "configurable per Agent".
    - FR-33 (line 425): "Configure `hexa`: identity, Agent Instructions, Provider/model selection ..., Response Policy, Approver Policy, lifecycle, regeneration ceiling" — no expiry duration.
    - Fix: append "proposal expiry duration (FR-18)" to that row's operation list.

13. **§10 places Content Safety Policy under "Agent administration" while FR-33 makes it Platform-scoped.**
    - §10 (line 742): "Agent administration: configure `hexa`, ..., Conversation Context Policy, and Content Safety Policy."
    - FR-33 (line 431): Content Safety Policy publication is "Platform Operator with Security approval | Platform"; §10 already has a separate bullet "Content Safety Policy publication" (line 749).
    - Fix: drop "and Content Safety Policy" from the Agent-administration bullet (keep the dedicated publication bullet), and note that Conversation Context Policy is tenant-selected only if FR-33 gains a row for it.

14. **UJ-3 state list omits `PostingFailed`.**
    - UJ-3 (line 66): "every state it passed through — awaiting decision, approved, posting, posted, rejected, abandoned, or expired".
    - FR-18 (line 365) and §6.1 (line 639) enumerate ten states including "posting failed".
    - Fix: "awaiting decision, approved, posting pending, posting failed, posted, rejected, abandoned, or expired".

15. **Build-status phrasing in the Decision Register.**
    - OQ-10 (line 820): "V1 declares no Approved Bounded Context Behavior, so the oversized case always fails closed today".
    - OQ-18 (line 828): "Today a single historical message that fails the active policy blocks generation".
    - Both read as descriptions of current system behavior rather than V1 decisions; the only sanctioned build-status statements are the non-conformance sentences in FR-2/FR-9/FR-12/FR-28 and the §8 register status.
    - Fix: OQ-10 → "so in V1 the oversized case always fails closed"; OQ-18 → "In V1 a single historical message that fails the active policy blocks generation".
