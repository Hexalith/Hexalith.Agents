using System.Collections.Generic;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>Whether the orchestration links an already-existing Party or provisions a brand-new Agent Party.</summary>
public enum AgentPartyLinkSource
{
    /// <summary>Validate and link an existing Party identity supplied by the administrator.</summary>
    ExistingParty,

    /// <summary>Provision a new Agent Party (Organization, deterministic id) and link it (AD-13).</summary>
    ProvisionNewParty,
}

/// <summary>Whether the orchestration issues a first link or an explicit replacement of the linked identity.</summary>
public enum AgentPartyLinkOperation
{
    /// <summary>First link — rejected by the aggregate if a different identity is already linked (AC3).</summary>
    Link,

    /// <summary>Explicit replacement of the currently linked identity (AC3).</summary>
    Replace,
}

/// <summary>
/// Server-internal request driving the Party-identity link/replace orchestration (AC1, AC2, AC3). It carries the
/// trusted, already-resolved <see cref="IsAgentsAdmin"/> authorization decision (from claims) and any
/// <see cref="ClientSuppliedExtensions"/> the orchestration must sanitize — the reserved <c>actor:agentsAdmin</c>
/// and <c>party:linkValidation</c> keys are stripped and repopulated from trusted sources only.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope.</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="Source">Link an existing Party or provision a new one.</param>
/// <param name="Operation">First link or explicit replacement.</param>
/// <param name="PartyId">The existing Party id to link (required when <see cref="Source"/> is <see cref="AgentPartyLinkSource.ExistingParty"/>).</param>
/// <param name="OrganizationLabel">A minimal non-personal label for a provisioned Party (optional; defaulted from the Agent id).</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentPartyLinkRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    AgentPartyLinkSource Source,
    AgentPartyLinkOperation Operation,
    string? PartyId = null,
    string? OrganizationLabel = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);
