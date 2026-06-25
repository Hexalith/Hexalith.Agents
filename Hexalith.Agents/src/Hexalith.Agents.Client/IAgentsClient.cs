namespace Hexalith.Agents.Client;

/// <summary>
/// Public entry point for governed Agents operations.
/// </summary>
public interface IAgentsClient
{
    /// <summary>Gets provider-catalog administration operations.</summary>
    IProviderCatalogOperations ProviderCatalog { get; }

    /// <summary>Gets Agent administration operations.</summary>
    IAgentAdministrationOperations AgentAdministration { get; }

    /// <summary>Gets Agent invocation operations.</summary>
    IAgentInteractionOperations AgentInteractions { get; }

    /// <summary>Gets proposal workflow operations.</summary>
    IProposalWorkflowOperations ProposalWorkflow { get; }

    /// <summary>Gets operational status inspection operations.</summary>
    IAgentStatusOperations Status { get; }

    /// <summary>Gets support-safe audit inspection operations.</summary>
    IAgentAuditOperations Audit { get; }
}
