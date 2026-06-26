using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of resolving a Confirmation-mode Agent's Approver Policy sources against their
/// dependencies (Story 1.6 AC3). It carries ONLY safe per-source <see cref="Sources"/> outcomes — never a Party
/// display name, contact value, tenant-membership PII, conversation detail, secret, or provider SDK type
/// (AD-7, AD-9, AD-14). This is the single value allowed to cross the resolver boundary back into the Agents side.
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>AgentPartyValidationResult</c> /
/// <c>ProviderCatalogEntryReadResult</c>). The aggregated <see cref="ApproverPolicyValidationStatus"/> verdict is
/// computed from these outcomes by <c>ApproverPolicyVerdict.Evaluate</c>.
/// </remarks>
/// <param name="Sources">The per-source resolution outcomes (one per configured approver source).</param>
public sealed record ApproverPolicyResolutionResult(IReadOnlyList<ApproverSourceResolution> Sources);

/// <summary>
/// The safe per-source resolution outcome for one configured approver source (Story 1.6 AC3). Carries only the safe
/// source <see cref="Kind"/> and the resolution <see cref="Outcome"/> — no PII and no secret.
/// </summary>
/// <param name="Kind">The approver source kind that was resolved.</param>
/// <param name="Outcome">The fail-closed resolution outcome for that source.</param>
public sealed record ApproverSourceResolution(ApproverPolicySourceKind Kind, ApproverSourceOutcome Outcome);

/// <summary>
/// The fail-closed outcome of resolving a single approver source against its dependency (Story 1.6 AC3). It is safe
/// by construction — it classifies the resolution state and carries no PII or secret. <see cref="Unknown"/>
/// (ordinal 0) is the fail-safe sentinel.
/// </summary>
public enum ApproverSourceOutcome
{
    /// <summary>Absent/unrecognized outcome sentinel — treated as a degraded read and fails closed.</summary>
    Unknown = 0,

    /// <summary>The source resolved cleanly and is usable as an approver.</summary>
    Resolved,

    /// <summary>The source resolved to nothing (e.g. no such role / no such Party).</summary>
    Missing,

    /// <summary>The source exists but is deactivated, erased, or restricted.</summary>
    Disabled,

    /// <summary>The source reference resolved to more than one candidate (ambiguous authority).</summary>
    Ambiguous,

    /// <summary>The source could not be read freshly (stale/degraded projection or resolver unavailable) — fail closed.</summary>
    Unavailable,

    /// <summary>The source is outside the Agent's tenant scope or the caller is not authorized to read it.</summary>
    Unauthorized,
}
