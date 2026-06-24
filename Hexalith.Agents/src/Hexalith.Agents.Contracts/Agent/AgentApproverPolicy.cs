using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The cohesive Approver Policy value an administrator configures for a Confirmation-mode Agent (<c>hexa</c>)
/// (AC2, AC4). It bundles the ordered approver <see cref="Sources"/> and the FR-7 <see cref="DisclosureCategory"/>
/// as one unit so the command, event, durable state, and status surface all reuse the same shape rather than
/// spreading the fields across multiple parameters. It carries only safe references and enums — no Party PII, no
/// secret, no content (AD-7, AD-14).
/// </summary>
/// <remarks>
/// The monotonic policy <em>version</em> is deliberately NOT on this value object — it is assigned by the aggregate
/// when a policy change is accepted (exactly like the instructions/configuration versions), never supplied by the
/// caller. An empty <see cref="Sources"/> list is structurally storable but does not satisfy Confirmation-mode
/// activation readiness (it evaluates to <see cref="ApproverPolicyValidationStatus.Incomplete"/>).
/// </remarks>
/// <param name="Sources">The ordered declared approver sources (may be empty).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing later policy-basis reporting.</param>
public record AgentApproverPolicy(
    IReadOnlyList<ApproverPolicySource> Sources,
    ApproverPolicyBasisDisclosure DisclosureCategory);
