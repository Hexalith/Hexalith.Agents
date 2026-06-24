namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// Linking (or replacing) the Party identity was rejected because the trusted Parties-validation verdict was not
/// <see cref="PartyLinkValidationStatus.Valid"/> — the dependency was missing, disabled, ambiguous, unavailable,
/// unauthorized, or absent (failing closed) (AC2; FR-21). The Agent's linked Party identity is unchanged and
/// <c>hexa</c> remains not callable for posting-dependent workflows.
/// </summary>
/// <remarks>
/// <see cref="Status"/> classifies <em>which</em> dependency state blocked the link; it carries no Party display
/// name, contact value, personal identifier, or any Parties personal-data object (AC1; AD-7, AD-14). Recording the
/// verdict gives an auditable trail of the fail-closed decision without exposing personal Party data.
/// </remarks>
/// <param name="AgentId">The Agent aggregate identifier the link targeted.</param>
/// <param name="Status">The non-<see cref="PartyLinkValidationStatus.Valid"/> verdict that caused the rejection.</param>
public record AgentPartyIdentityLinkRejected(
    string AgentId,
    PartyLinkValidationStatus Status) : IRejectionEvent;
