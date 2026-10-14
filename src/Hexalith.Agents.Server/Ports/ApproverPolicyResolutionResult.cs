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
public sealed record ApproverPolicyResolutionResult(IReadOnlyList<ApproverSourceResolution> Sources)
{
    /// <summary>Gets the unavailable complete external record.</summary>
    public Hexalith.Agents.Contracts.Security.DependencyNotAvailable? Dependency { get; init; }
}
