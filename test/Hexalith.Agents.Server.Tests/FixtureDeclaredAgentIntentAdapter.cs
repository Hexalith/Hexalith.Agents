using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Declares finite local protocol routes while delegating the real adapter's unchanged canonical intent.</summary>
/// <remarks>This fixture metadata supplies no production declaration, activation or external availability.</remarks>
internal sealed class FixtureDeclaredAgentIntentAdapter : IDeclaredIdempotencyIntentAdapter
{
    private readonly IIdempotencyIntentAdapter _inner;

    /// <summary>Wraps an existing registered adapter with an explicit test catalog declaration.</summary>
    internal FixtureDeclaredAgentIntentAdapter(IIdempotencyIntentAdapter inner)
    {
        _inner = inner;
        string domain = inner.CommandType switch
        {
            nameof(CreateAgent) or nameof(UpdateAgentConfiguration) or nameof(ConfigureAgentResponseMode)
                or nameof(ActivateAgent) or nameof(DisableAgent) => "agent",
            nameof(CreateProviderModelEntry) or nameof(UpdateProviderModelEntry) or nameof(EnableProviderModelEntry)
                or nameof(DisableProviderModelEntry) => "provider-catalog",
            nameof(SetTenantProviderModelEnablement) or nameof(DecideProviderDataHandling) => "tenant-provider-enablement",
            nameof(RegisterInteractionPermit) or nameof(AcknowledgeInteractionCreation) or nameof(AcquireConversationEffectLease)
                or nameof(CommitConversationEffect) or nameof(SettleConversationEffectLease) or nameof(CancelConversationEffectLease)
                or nameof(EnterConversationDeletionClosing) => "conversation-agent-state",
            nameof(CreatePermittedAgentInteraction) or nameof(RecordAutomaticInteractionStep) => "agent-interaction",
            _ => throw new ArgumentException("No local protocol route is declared for this adapter.", nameof(inner)),
        };
        string contractNamespace = domain switch
        {
            "agent" => typeof(CreateAgent).Namespace!,
            "provider-catalog" or "tenant-provider-enablement" => typeof(CreateProviderModelEntry).Namespace!,
            "conversation-agent-state" => typeof(RegisterInteractionPermit).Namespace!,
            _ => typeof(CreatePermittedAgentInteraction).Namespace!,
        };
        Type contract = typeof(CreateAgent).Assembly.GetType(contractNamespace + "." + inner.CommandType, throwOnError: true)!;
        Declaration = new(new("agents-fixture-" + inner.CommandType.ToLowerInvariant(), "command", domain, inner.CommandType,
            null, "v1", 1, "workload", EventStoreOperationIdentifiers.DomainServiceProcess, null, [],
            new(contract.FullName!, "exact", [inner.CommandType], "domain-service", null, [])),
            inner.AdapterId, inner.OperationId, "1",
            new("agents-fixture-" + inner.CommandType.ToLowerInvariant() + "-intent", inner.DescriptorVersion,
                [new("target", "string"), new("semantic-payload", "string"), new("semantic-options", "string")]),
            inner.RetentionTier, false);
    }

    /// <inheritdoc />
    public CommandIntentDeclaration Declaration { get; }
    /// <inheritdoc />
    public string CommandType => _inner.CommandType;
    /// <inheritdoc />
    public string AdapterId => _inner.AdapterId;
    /// <inheritdoc />
    public string OperationId => _inner.OperationId;
    /// <inheritdoc />
    public int DescriptorVersion => _inner.DescriptorVersion;
    /// <inheritdoc />
    public IdempotencyReplayRetentionTier RetentionTier => _inner.RetentionTier;
    /// <inheritdoc />
    public IdempotencyCanonicalIntent CreateIntent(IdempotencyIntentCommand command) => _inner.CreateIntent(command);
}
