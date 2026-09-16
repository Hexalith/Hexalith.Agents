namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Defines the reserved EventStore extension identifiers populated by the trusted Agents application path.
/// </summary>
/// <remarks>
/// These identifiers describe an internal service-to-service contract. Public callers are never authorized to
/// assert their values.
/// </remarks>
public static class AgentSetupTrustedExtensions
{
    /// <summary>Gets the Agents-administrator authorization extension key.</summary>
    public const string AgentAdministrator = "actor:agentsAdmin";

    /// <summary>Gets the activation provider-selection validation extension key.</summary>
    public const string ProviderSelectionValidation = "provider:selectionValidation";

    /// <summary>Gets the activation approver-policy validation extension key.</summary>
    public const string ApproverPolicyValidation = "approver:policyValidation";

    /// <summary>Gets the activation expected-configuration-version extension key.</summary>
    public const string ActivationExpectedConfigurationVersion = "agent:activationExpectedConfigurationVersion";
}
