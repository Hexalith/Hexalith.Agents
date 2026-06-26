namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that the governed Agent record <c>hexa</c> was created (AC1; FR-1). The new Agent is in
/// <see cref="AgentLifecycleStatus.Draft"/> and not callable until a gated activation succeeds.
/// </summary>
/// <remarks>
/// This is a success event and is therefore the sanctioned durable home for the sensitive Agent Instructions
/// text (AD-14): <see cref="Instructions"/> lives only here and on <see cref="AgentConfigurationUpdated"/> and in
/// durable state — never on a rejection, status view, log, or audit summary. Content-bearing Agents events must
/// adopt EventStore payload-protection/redaction before production use (tracked by AD-14); plaintext here is for
/// local/dev only and is not a bespoke encryption scheme. No wall-clock timestamp is carried — occurrence time is
/// supplied by EventStore event metadata so the aggregate stays pure (AD-3).
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="TenantId">Tenant scope captured at create.</param>
/// <param name="DisplayName">Safe display name (may be empty for an incomplete draft).</param>
/// <param name="Description">Optional safe description.</param>
/// <param name="Instructions">Agent Instructions text (sensitive; durable here only — AD-14).</param>
/// <param name="ConfigurationVersion">Initial configuration version (1).</param>
/// <param name="InstructionsVersion">Initial instructions version (1 when instructions are present, else 0).</param>
public record AgentCreated(
    string AgentId,
    string TenantId,
    string DisplayName,
    string? Description,
    string Instructions,
    int ConfigurationVersion,
    int InstructionsVersion) : IEventPayload;
