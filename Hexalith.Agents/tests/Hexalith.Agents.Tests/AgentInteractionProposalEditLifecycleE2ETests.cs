using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// End-to-end Confirmation-mode lifecycle tests for the Story 3.3 Proposed Agent Reply edit (AC1, AC2, AC4; FR-14, FR-15;
/// AD-3, AD-5, AD-13, AD-14). Unlike the focused <see cref="AgentInteractionProposalEditAggregateTests"/> — which drives the
/// edit command over a pre-built <c>ProposalCreated</c> state — these walk the WHOLE command chain
/// (request → gate → context → generation → proposal create → edit) through the real reflection-dispatch pipeline
/// (<c>AgentInteractionAggregate.ProcessAsync</c> + JSON round-trip + the production <c>Apply</c> handlers), so every
/// precondition is exercised by the actual handler. This proves the ONLY way to reach an edit is a full happy Confirmation
/// path through a created proposal, and that the edited version is appended alongside the preserved generated version.
/// </summary>
public sealed class AgentInteractionProposalEditLifecycleE2ETests
{
    private static RequestAgentInteraction ConfirmationRequest { get; } = new(
        AgentId,
        SourceConversationId,
        CallerPartyId,
        Prompt,
        IdempotencyKey,
        ConfirmationSnapshot,
        ClientCorrelationId);

    [Fact]
    public async Task A_confirmation_interaction_reaches_an_edited_proposal_through_the_full_command_chain()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.GeneratedVersions.ShouldNotBeNull().Count.ShouldBe(1);

        // Edit (authorized) → ProposalEdited, the edited version appended alongside the preserved generated version (AC1).
        DomainResult edited = await ProcessAndApplyAsync(aggregate, state, EditCommand(EditedProposalResult()));

        ProposedAgentReplyEdited editedEvent = edited.Events[0].ShouldBeOfType<ProposedAgentReplyEdited>();
        editedEvent.EditedVersion.VersionId.ShouldBe(EditedVersionId);
        editedEvent.EditedVersion.Kind.ShouldBe(AgentGenerationKind.Edited);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Edited);
        state.GeneratedVersions!.Count.ShouldBe(2); // generated + edited, prior preserved (AC1)
        state.GeneratedVersions[0].Kind.ShouldBe(AgentGenerationKind.Generated);
        state.GeneratedVersions[1].Kind.ShouldBe(AgentGenerationKind.Edited);

        // AD-14: the safe evidence that survived the full reflection-dispatch + JSON round-trip carries no content.
        JsonSerializer.Serialize(editedEvent.Evidence).ShouldNotContain(EditedContentText);
    }

    [Fact]
    public async Task A_terminal_proposal_cannot_be_edited_end_to_end()
    {
        // AC2 end-to-end: before a proposal is created, an edit command can only be structurally rejected — there is no
        // pending proposal to edit, so the edit never appends a version or changes state.
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        state.Status.ShouldBe(AgentInteractionStatus.Generated); // generated, but no proposal created yet

        DomainResult edited = await ProcessAndApplyAsync(aggregate, state, EditCommand(EditedProposalResult()));

        edited.IsRejection.ShouldBeTrue();
        edited.Events[0].ShouldBeOfType<ProposedAgentReplyNotEditableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotEditableReason.ProposalNotPending);
        edited.Events.OfType<ProposedAgentReplyEdited>().ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.Generated); // unchanged
        state.ProposalState.ShouldBeNull();
        state.GeneratedVersions!.Count.ShouldBe(1); // no edited version appended
    }
}
