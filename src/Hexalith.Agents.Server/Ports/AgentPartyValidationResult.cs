using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of validating or provisioning the Party identity an Agent (<c>hexa</c>) is being linked
/// to (AC1, AC2). It carries ONLY the safe verdict and the stable <see cref="PartyId"/> reference — never a Party
/// display name, contact value, personal identifier, or any Parties personal-data object (AC1; AD-7, AD-14). This
/// is the single value allowed to cross the port boundary back into the Agents side; the Parties PII never does.
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type. <see cref="PartyId"/> is <see langword="null"/>
/// when no Party could be validated/provisioned (e.g. a missing/unavailable/unauthorized verdict).
/// </remarks>
/// <param name="Status">The fail-closed dependency verdict (only <see cref="PartyLinkValidationStatus.Valid"/> permits a link).</param>
/// <param name="PartyId">The validated/created stable Party id (a reference, not PII), or <see langword="null"/> when none.</param>
public sealed record AgentPartyValidationResult(PartyLinkValidationStatus Status, string? PartyId);
