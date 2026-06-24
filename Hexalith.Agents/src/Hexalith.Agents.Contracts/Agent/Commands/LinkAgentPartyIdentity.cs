namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Links an existing Agent (<c>hexa</c>) to its single active Party identity (AC1, AC3; FR-2). The command payload
/// carries only the stable <see cref="PartyId"/> reference — never a Party display name, contact value, personal
/// identifier, or any Parties personal-data object (AC1; AD-7). The Agent identifier comes from the command
/// envelope.
/// </summary>
/// <remarks>
/// The trusted Parties-validation verdict is <em>not</em> on this payload: it is supplied to the aggregate through
/// the server-populated, client-stripped <c>party:linkValidation</c> envelope extension (AD-3, AD-12), patterned
/// after the <c>actor:agentsAdmin</c> trust model. The aggregate links only on a <c>Valid</c> verdict and rejects
/// any other/absent verdict, so a direct client command that bypassed Parties validation is failed closed (AC2). A
/// second <em>distinct</em> link while one is already active is rejected — changing identity requires the explicit
/// <see cref="ReplaceAgentPartyIdentity"/> (AC3); re-asserting the same id is a deterministic no-op (AD-13).
/// </remarks>
/// <param name="PartyId">The stable Parties-owned identifier to link (a reference, not PII — AD-7).</param>
public record LinkAgentPartyIdentity(string PartyId);
