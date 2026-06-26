namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Explicitly replaces an Agent's (<c>hexa</c>) currently linked Party identity with a different one (AC3; FR-2).
/// Replacement is the sanctioned way to change identity once one is linked — a plain <see cref="LinkAgentPartyIdentity"/>
/// against a different id is rejected (AC3). The command payload carries only the stable <see cref="PartyId"/>
/// reference — never a Party display name, contact value, personal identifier, or any Parties personal-data object
/// (AC1; AD-7). The Agent identifier comes from the command envelope.
/// </summary>
/// <remarks>
/// Like <see cref="LinkAgentPartyIdentity"/>, the trusted Parties-validation verdict is supplied through the
/// server-populated, client-stripped <c>party:linkValidation</c> envelope extension (AD-3, AD-12), not this
/// payload; the aggregate replaces only on a <c>Valid</c> verdict and otherwise fails closed (AC2). Replacement
/// deterministically sets the single active identity, so the Agent never holds more than one Party identity (AC3).
/// Re-asserting the already-linked id is a deterministic no-op (AD-13).
/// </remarks>
/// <param name="PartyId">The stable Parties-owned identifier to link in place of the current one (a reference, not PII — AD-7).</param>
public record ReplaceAgentPartyIdentity(string PartyId);
