using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The AD-4 configuration snapshot frozen onto an Agent Call at request time (AC1; FR-8, FR-9). It captures the
/// Agent's governed configuration the instant the call was made so a later admin edit to the Agent or
/// ProviderCatalog affects only <em>future</em> interactions, never this one. Every field is a safe scalar or
/// reference — no secret value, provider SDK type, prompt, or Conversation content (AD-9, AD-14).
/// </summary>
/// <remarks>
/// <para>
/// The snapshot is <b>server-assembled</b> by the request orchestration from the trusted Agent read (the pure
/// aggregate cannot read the <c>Agent</c> aggregate — AD-3) and is then recorded verbatim on
/// <see cref="Events.InteractionRequested"/>. All fields are declared <b>non-nullable</b> because the snapshot
/// reader returns a populated snapshot ONLY for an Agent that has passed activation (AC1's "active in setup
/// readiness" precondition) — by which point Provider/Model identity and capability version are guaranteed present.
/// The pre-activation case is the reader's not-available result, which surfaces as a structural
/// <see cref="AgentInteractionRequestValidationStatus.MissingAgentSnapshot"/> rejection.
/// </para>
/// <para>
/// <see cref="ContentSafetyPolicyVersion"/> is an <em>additive extension</em> beyond AD-4's enumerated floor
/// (config/instructions/response-mode/approver-policy versions, Provider/Model identity, capability version,
/// caller/source ids, and context-build policy) — it is snapshotted here to anticipate Story 2.4's safety check.
/// <see cref="ContextPolicyReference"/> is the V1 default reference (<see cref="DefaultContextPolicyReference"/>);
/// the concrete Conversation Context Policy is bound in Story 2.3 without a contract break (the field stays additive).
/// </para>
/// </remarks>
/// <param name="ConfigurationVersion">The Agent's monotonic configuration version at request time.</param>
/// <param name="InstructionsVersion">The Agent Instructions version at request time.</param>
/// <param name="ResponseMode">The Response Mode frozen at request time so a later mode change cannot affect this interaction (AD-4).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version at request time.</param>
/// <param name="ProviderId">The selected stable safe provider identifier (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The selected stable safe model identifier (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The captured provider capability version at request time.</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version at request time (additive beyond AD-4; anticipates Story 2.4).</param>
/// <param name="ContextPolicyReference">An opaque reference to the Conversation Context Policy in force (V1 default until Story 2.3 binds a concrete policy).</param>
public record AgentInteractionSnapshot(
    int ConfigurationVersion,
    int InstructionsVersion,
    AgentResponseMode ResponseMode,
    int ApproverPolicyVersion,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    string ContextPolicyReference)
{
    /// <summary>
    /// The stable V1 default Conversation Context Policy reference (FR-9 "the Conversation Context Policy version or
    /// equivalent identifier"). Defined once here so the request orchestration and its tests cannot drift; Story 2.3
    /// replaces it with the concrete policy. Treated as an opaque reference, never parsed.
    /// </summary>
    public const string DefaultContextPolicyReference = "full-conversation-v1";
}
