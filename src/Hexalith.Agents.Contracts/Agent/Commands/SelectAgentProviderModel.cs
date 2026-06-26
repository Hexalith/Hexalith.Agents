namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Selects an enabled Provider/model from the governed catalog for an Agent (<c>hexa</c>) (AC1; FR-5). The command
/// payload carries only the safe <see cref="ProviderId"/>/<see cref="ModelId"/> identifiers (Agents/Provider-owned
/// references, never secrets — the Identity convention) plus the captured <see cref="ProviderCapabilityVersion"/>.
/// It never carries a secret value, configuration reference, capability-metadata blob, or provider SDK type
/// (AD-9, AD-14). The Agent identifier comes from the command envelope.
/// </summary>
/// <remarks>
/// The trusted provider-readiness verdict is <em>not</em> on this payload: it is supplied to the aggregate through
/// the server-populated, client-stripped <c>provider:selectionValidation</c> envelope extension (AD-3, AD-12),
/// patterned after the <c>actor:agentsAdmin</c> (1.3) and <c>party:linkValidation</c> (1.4) trust models. The
/// aggregate records the selection only on a <c>Valid</c> verdict and rejects any other/absent verdict, so a direct
/// client command that bypassed catalog validation is failed closed with no provider SDK call or credential access
/// (AC2). Re-asserting the same Provider/model/version is a deterministic no-op (AD-13).
/// </remarks>
/// <param name="ProviderId">The stable safe provider identifier to select (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The stable safe model identifier to select (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version captured from the catalog at selection time (AC1).</param>
public record SelectAgentProviderModel(
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion);
