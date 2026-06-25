namespace Hexalith.Agents.Client;

/// <summary>
/// Concrete public Agents client facade composed from operation groups.
/// </summary>
public sealed class AgentsClient : IAgentsClient
{
    /// <summary>Initializes a new instance of the <see cref="AgentsClient"/> class.</summary>
    public AgentsClient(
        IProviderCatalogOperations providerCatalog,
        IAgentAdministrationOperations agentAdministration,
        IAgentInteractionOperations agentInteractions,
        IProposalWorkflowOperations proposalWorkflow,
        IAgentStatusOperations status,
        IAgentAuditOperations audit)
    {
        ArgumentNullException.ThrowIfNull(providerCatalog);
        ArgumentNullException.ThrowIfNull(agentAdministration);
        ArgumentNullException.ThrowIfNull(agentInteractions);
        ArgumentNullException.ThrowIfNull(proposalWorkflow);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(audit);

        ProviderCatalog = providerCatalog;
        AgentAdministration = agentAdministration;
        AgentInteractions = agentInteractions;
        ProposalWorkflow = proposalWorkflow;
        Status = status;
        Audit = audit;
    }

    /// <inheritdoc />
    public IProviderCatalogOperations ProviderCatalog { get; }

    /// <inheritdoc />
    public IAgentAdministrationOperations AgentAdministration { get; }

    /// <inheritdoc />
    public IAgentInteractionOperations AgentInteractions { get; }

    /// <inheritdoc />
    public IProposalWorkflowOperations ProposalWorkflow { get; }

    /// <inheritdoc />
    public IAgentStatusOperations Status { get; }

    /// <inheritdoc />
    public IAgentAuditOperations Audit { get; }

    /// <summary>
    /// Creates a fail-closed client facade for hosts whose live bindings are not configured yet.
    /// </summary>
    /// <returns>A client that returns structured unavailable results for every operation.</returns>
    public static IAgentsClient Unavailable()
        => new AgentsClient(
            new UnavailableProviderCatalogOperations(),
            new UnavailableAgentAdministrationOperations(),
            new UnavailableAgentInteractionOperations(),
            new UnavailableProposalWorkflowOperations(),
            new UnavailableAgentStatusOperations(),
            new UnavailableAgentAuditOperations());
}
