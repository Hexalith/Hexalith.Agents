using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The Content Safety Policy value an administrator or release operator defines for an Agent (<c>hexa</c>)
/// (Story 1.7 AC1; FR-26). It bundles the safe governance descriptors — prompt constraints and blocked/restricted
/// output categories — plus the safety failure-handling and audit-treatment governance choices, as one unit the
/// command, event, durable state, and configuration read path all reuse.
/// </summary>
/// <remarks>
/// <para>
/// <b>Policy content (AD-14):</b> <see cref="PromptConstraints"/>, <see cref="BlockedOutputCategories"/>, and
/// <see cref="RestrictedOutputCategories"/> are safe governance labels/descriptors (tenant/governance-defined
/// strings) — never secrets, never Party PII, never conversation-derived content. They are nonetheless policy content
/// kept off the status surface (AC2; AD-14): never echoed on a rejection, the status view, logs, telemetry, or audit
/// summaries. They live on the durable event/state and the deferred configuration read path only — treated exactly
/// like the Agent Instructions text.
/// </para>
/// <para>
/// The exact category taxonomy/filter set is deferred to a Product+Security decision (PRD OQ-9); this value object
/// fixes the <em>structure</em>, not the taxonomy. The monotonic policy <em>version</em> is deliberately NOT on this
/// value object — it is assigned by the aggregate when a change is accepted, never supplied by the caller.
/// </para>
/// </remarks>
/// <param name="PromptConstraints">Safe governance descriptors constraining prompts (kept off the status surface; AD-14).</param>
/// <param name="BlockedOutputCategories">Safe category labels for output that must be blocked (kept off the status surface; AD-14).</param>
/// <param name="RestrictedOutputCategories">Safe category labels for output that is restricted (kept off the status surface; AD-14).</param>
/// <param name="FailureHandling">The governance choice for how a safety failure is handled (enforced in Epic 2/3).</param>
/// <param name="AuditTreatment">The governance choice for how a safety failure is audited (enforced in Epic 2/4).</param>
public record AgentContentSafetyPolicy(
    IReadOnlyList<string> PromptConstraints,
    IReadOnlyList<string> BlockedOutputCategories,
    IReadOnlyList<string> RestrictedOutputCategories,
    ContentSafetyFailureHandling FailureHandling,
    ContentSafetyAuditTreatment AuditTreatment);
