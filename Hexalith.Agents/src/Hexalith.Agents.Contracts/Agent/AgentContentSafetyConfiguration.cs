namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The cohesive Content Safety configuration an administrator or release operator defines for an Agent (<c>hexa</c>)
/// (Story 1.7 AC1, AC3; FR-26). It bundles the <see cref="ActivePolicy"/> both modes use by default with the optional
/// stricter per-mode overrides, as one unit the command, event, durable state, and configuration read path all reuse.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mode-specific policy (AC3):</b> <see cref="ActivePolicy"/> is the policy both Automatic and Confirmation modes
/// use by default. <see cref="AutomaticModePolicy"/> / <see cref="ConfirmationModePolicy"/> are <em>optional</em>
/// stricter overrides — a <see langword="null"/> override means "this mode uses the active policy."
/// </para>
/// <para>
/// V1 records and surfaces a mode-specific policy as the effective policy for that mode; machine-validating that an
/// override is genuinely <em>stricter</em> than the active policy is deferred (consistent with the deferred
/// safety-filter provider — OQ-9), so an override is a governance declaration the configurer is responsible for
/// keeping at least as restrictive. The policy content carried here is kept off the status surface (AC2; AD-14); the
/// status view surfaces only which modes carry an override, never the override content.
/// </para>
/// </remarks>
/// <param name="ActivePolicy">The Content Safety Policy both modes use unless a stricter mode-specific override is configured.</param>
/// <param name="AutomaticModePolicy">The optional stricter Automatic-mode override (<see langword="null"/> = use the active policy).</param>
/// <param name="ConfirmationModePolicy">The optional stricter Confirmation-mode override (<see langword="null"/> = use the active policy).</param>
public record AgentContentSafetyConfiguration(
    AgentContentSafetyPolicy ActivePolicy,
    AgentContentSafetyPolicy? AutomaticModePolicy,
    AgentContentSafetyPolicy? ConfirmationModePolicy);
