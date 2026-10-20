using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Security;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Level-2 EventStore model: dispatches real public envelopes through Handle/Apply and rehydrates accepted event history.</summary>
internal sealed class AutomaticInteractionReplayFixture : IAgentCommandDispatcher, IConversationAgentStateReader, IAgentInteractionStateReader, IInteractionWorkflowScheduler,
    IAutomaticInteractionIntakeGate, IInteractionDirectoryReadiness
{
    /// <summary>The exact tenant fixture.</summary>
    internal const string Tenant = "tenant-a";
    /// <summary>The exact Conversation fixture.</summary>
    internal const string Conversation = "conversation-a";
    private readonly List<IEventPayload> _directoryEvents = [];
    private readonly List<IEventPayload> _interactionEvents = [];
    /// <summary>Gets directory state rehydrated from events.</summary>
    internal ConversationAgentStateState? Directory { get; private set; }
    /// <summary>Gets interaction state rehydrated from events.</summary>
    internal AgentInteractionState? Interaction { get; private set; }
    /// <summary>Gets authoritative external fixture outcomes.</summary>
    internal AllowAllAutomaticInteractionEffects Effects { get; } = new();
    /// <summary>Gets recorded scheduled instance ids.</summary>
    internal HashSet<string> Scheduled { get; } = [];
    /// <summary>Gets accepted command identities.</summary>
    internal List<string> AcceptedCommandIds { get; } = [];
    /// <summary>Gets dispatched envelope intents.</summary>
    internal List<AgentCommandIntent> Dispatches { get; } = [];
    /// <summary>Gets or sets one fault checkpoint.</summary>
    internal int? FaultAt { get; set; }
    /// <summary>Gets or sets the crash side of the append.</summary>
    internal bool CrashAfterAppend { get; set; }
    /// <summary>Gets or sets acknowledgement loss after runtime scheduling.</summary>
    internal bool LoseScheduleAcknowledgement { get; set; }
    /// <summary>Gets or sets fixture readiness.</summary>
    internal bool DirectoryReady { get; set; } = true;
    /// <summary>Gets or sets response mode.</summary>
    internal AgentResponseMode Mode { get; set; } = AgentResponseMode.Automatic;
    /// <summary>Gets the exact AD-29 interaction identity.</summary>
    internal string InteractionId { get; } = ConversationAgentStateIdentity.Interaction(Tenant, "hexa", Conversation, "person-a", "client-a");
    /// <summary>Gets the protected no-content request fixture.</summary>
    internal AutomaticInteractionIntakeRequest Request => new(Tenant, Tenant, "human-a", "person-a", true, "hexa", Conversation, "client-a",
        new(new(Tenant, InteractionId, "fixture-interaction-dek"), JsonSerializer.SerializeToElement(new { FixtureSealed = "request" })), new string('a', 64), "fixture-digest-v1");
    /// <summary>Gets the workflow's content-free input.</summary>
    internal WorkflowExecutionState Input => new(Tenant, Conversation, InteractionId, new(Tenant, InteractionId, 1, new string('a', 64)), Interaction?.AutomaticRevision ?? 1, WorkflowExecutionStatus.Running);
    /// <summary>Creates a fresh recovery orchestrator after a simulated process restart.</summary>
    internal InteractionOutboxRecoveryOrchestrator Recovery() => new(this, this, this, this);
    /// <summary>Creates a fresh activity step orchestrator after a simulated process restart.</summary>
    internal AutomaticInteractionStepOrchestrator Steps() => new(this, this, this, Effects);
    /// <summary>Starts the explicit call using fully injected no-content fixtures.</summary>
    internal Task<AutomaticInteractionIntakeResult> StartAsync()
    {
        var availability = Substitute.For<IAgentDependencyAvailability>();
        return new AutomaticInteractionIntakeOrchestrator(this, availability, this, this, this, Recovery()).ExecuteAsync(Request, CancellationToken.None);
    }
    /// <summary>Drives deterministic checkpoints to a terminal state with a finite test-only bound.</summary>
    internal async Task<WorkflowExecutionState> CompleteAsync()
    {
        WorkflowExecutionState input = Input;
        for (int n = 0; n < 128; n++)
        {
            input = await Steps().ExecuteAsync(input, CancellationToken.None);
            if (input.Status != WorkflowExecutionStatus.Running) { return input; }
        }
        throw new InvalidOperationException("FixtureDidNotConverge");
    }
    /// <summary>Rehydrates both owner streams from their persisted event sequence.</summary>
    internal void Restart()
    {
        Directory = null; Interaction = null;
        foreach (IEventPayload item in _directoryEvents) { Directory ??= new(); Directory.Apply((dynamic)Clone(item)); }
        foreach (IEventPayload item in _interactionEvents) { Interaction ??= new(); Interaction.Apply((dynamic)Clone(item)); }
    }
    /// <summary>Returns canonical end-state for restart comparisons.</summary>
    internal string Snapshot() => JsonSerializer.Serialize(new { Directory, Interaction });
    /// <summary>Reserves a particular phase then pauses before its commit.</summary>
    internal async Task ReserveUntilAsync(ConversationEffectKind kind)
    {
        for (int n = 0; n < 100; n++)
        {
            if (Directory?.Leases.Values.Any(l => l.Kind == kind && l.Status == ConversationEffectLeaseStatus.Reserved) == true) { return; }
            (await Steps().ExecuteAsync(Input, CancellationToken.None)).Status.ShouldBe(WorkflowExecutionStatus.Running);
        }
        throw new InvalidOperationException("FixtureReservationMissing");
    }
    /// <summary>Closes the same authoritative directory.</summary>
    internal async Task CloseAsync()
    {
        Directory.ShouldNotBeNull();
        await InteractionCommandDispatch.SendAsync(this, Tenant, ConversationAgentStateIdentity.Domain, ConversationAgentStateIdentity.Directory(Tenant, Conversation), new string('d', 64),
            new Hexalith.Agents.Contracts.ConversationAgentState.Commands.EnterConversationDeletionClosing("tenant-a", "conversation-a", new string('d', 64), Directory.Revision), CancellationToken.None);
    }
    /// <inheritdoc />
    public Task<SubmitCommandResponse> DispatchAsync(AgentCommandIntent intent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int index = Dispatches.Count; Dispatches.Add(intent);
        bool crash = FaultAt == index;
        if (crash && !CrashAfterAppend) { FaultAt = null; throw new AutomaticInteractionInjectedCrash(); }
        Type contract = typeof(AgentsSecurityIdentity).Assembly.GetTypes().Single(t => t.Name == intent.CommandType);
        object command = JsonSerializer.Deserialize(intent.Payload, contract).ShouldNotBeNull();
        CommandEnvelope envelope = new(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId, intent.CommandType, intent.Payload,
            intent.CorrelationId, intent.CausationId, intent.UserId, intent.Extensions);
        DomainResult result = intent.Domain == ConversationAgentStateIdentity.Domain
            ? ConversationAgentStateAggregate.Handle((dynamic)command, Directory, envelope)
            : AgentInteractionAggregate.Handle((dynamic)command, Interaction, envelope);
        if (result.IsRejection) { throw new InvalidOperationException("Rejected:" + JsonSerializer.Serialize(result.Events)); }
        if (result.Events.Count > 0) { AcceptedCommandIds.Add(intent.MessageId); }
        foreach (IEventPayload item in result.Events)
        {
            if (intent.Domain == ConversationAgentStateIdentity.Domain) { Directory ??= new(); Directory.Apply((dynamic)item); _directoryEvents.Add(Clone(item)); }
            else { Interaction ??= new(); Interaction.Apply((dynamic)item); _interactionEvents.Add(Clone(item)); }
        }
        if (crash) { FaultAt = null; throw new AutomaticInteractionInjectedCrash(); }
        return Task.FromResult(new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId));
    }
    /// <inheritdoc />
    Task<ConversationAgentStateState?> IConversationAgentStateReader.ReadAsync(string tenantId, string conversationId, CancellationToken ct)
        => Task.FromResult(tenantId == Tenant && conversationId == Conversation ? Directory : null);
    /// <inheritdoc />
    Task<AgentInteractionState?> IAgentInteractionStateReader.ReadAsync(string tenantId, string interactionId, CancellationToken ct)
        => Task.FromResult(tenantId == Tenant && interactionId == InteractionId ? Interaction : null);
    /// <inheritdoc />
    public Task EnsureScheduledAsync(WorkflowExecutionState input, CancellationToken ct)
    {
        WorkflowExecutionStateValidation.Validate(input); Scheduled.Add(input.AgentInteractionId);
        if (LoseScheduleAcknowledgement) { LoseScheduleAcknowledgement = false; throw new AutomaticInteractionInjectedCrash(); }
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    public Task<AutomaticInteractionGateResult> EvaluateAsync(AutomaticInteractionIntakeRequest request, CancellationToken ct)
        => Task.FromResult(new AutomaticInteractionGateResult(true, new(1, 1, Mode, 1, "provider-a", "model-a", 1, 1, "full-conversation-v1")));
    /// <inheritdoc />
    public Task<DirectoryEpochInstallation?> ReadAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<DirectoryEpochInstallation?>(DirectoryReady ? new(Tenant, "fixture-installation", "fixture-epoch", 0, "fixture-revocation", "fixture-enforcement") : null);
    private static IEventPayload Clone(IEventPayload item) => (IEventPayload)JsonSerializer.Deserialize(JsonSerializer.Serialize(item, item.GetType()), item.GetType())!;
}
