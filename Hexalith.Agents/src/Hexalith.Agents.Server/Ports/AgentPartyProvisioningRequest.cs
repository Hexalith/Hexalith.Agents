namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal request to provision a brand-new Agent Party identity (FR-2; AD-13). The new Party is created
/// in Parties as a <c>PartyType.Organization</c> with a minimal, non-personal label — there is no AI-agent/bot/
/// system Party type, and an Organization avoids person PII. The resulting Party id is derived deterministically
/// from <see cref="AgentId"/> so a retried provision is idempotent and never creates duplicate Parties (AD-13).
/// </summary>
/// <remarks>
/// The <see cref="OrganizationLabel"/> is the non-personal name owned by Parties (e.g. an Agent display label); it
/// is never persisted on the Agents side (AC1; AD-7). This is not a public contract — it is a Server-only port type.
/// </remarks>
/// <param name="AgentId">The Agent the Party is provisioned for (drives the deterministic Party id).</param>
/// <param name="OrganizationLabel">A minimal, non-personal organization label stored only in Parties.</param>
public sealed record AgentPartyProvisioningRequest(string AgentId, string OrganizationLabel);
