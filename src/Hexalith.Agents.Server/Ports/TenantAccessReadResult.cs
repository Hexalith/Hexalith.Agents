using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of reading the caller's tenant access for the invocation gate (Story 2.2; AC1, AC3; AD-12).
/// It carries a fail-closed verdict and optional transient verified actor evidence, never a raw claim, token or
/// tenant-membership PII. Actor evidence stays within Server and cannot disclose cross-tenant existence (AC3; AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>AgentPartyValidationResult</c>). Per
/// AD-12 tenant access comes from Agents' own local Tenants projection; <see cref="IsFresh"/> feeds the separate
/// <see cref="AgentInteractionGateCheck.DependencyFreshness"/> check (a behind-threshold projection is
/// <see cref="AgentInteractionGateOutcome.Stale"/>). The live projection-version comparison is deferred.
/// </remarks>
/// <param name="Outcome">The fail-closed tenant-access outcome (only <see cref="AgentInteractionGateOutcome.Satisfied"/> permits the call).</param>
/// <param name="IsFresh">Whether the consulted Tenants projection is within its freshness threshold.</param>
public sealed record TenantAccessReadResult(AgentInteractionGateOutcome Outcome, bool IsFresh)
{
    /// <summary>Gets the transient lifetime of a verified current tenant authorization; it grants no access by itself.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public AgentAuthorityValidity? Authority { get; init; }

    /// <summary>Gets current actor evidence after successful completion-time authorization, bounded by the earlier actor or tenant deadline.</summary>
    public CurrentHumanActorEvidence? CurrentActor { get; init; }

    /// <summary>Gets the unavailable external record, without any claimed resource information.</summary>
    public Hexalith.Agents.Contracts.Security.DependencyNotAvailable? Dependency { get; init; }
    /// <summary>Gets the fail-closed not-available result (the deferred default) — unavailable and not fresh.</summary>
    public static TenantAccessReadResult Unavailable { get; } = new(AgentInteractionGateOutcome.Unavailable, false);
}
