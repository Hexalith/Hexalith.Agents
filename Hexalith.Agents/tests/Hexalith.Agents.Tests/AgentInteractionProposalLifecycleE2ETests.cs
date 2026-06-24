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
/// End-to-end Confirmation-mode lifecycle tests for the Story 3.1 Proposed Agent Reply creation (AC1–AC3; FR-13, FR-14,
/// FR-27; AD-3, AD-5, AD-6, AD-13, AD-14). Unlike the focused <see cref="AgentInteractionProposalAggregateTests"/> — which
/// drives the create command over a pre-built <c>Generated</c> state — these tests walk the WHOLE command chain
/// (request → gate → context → generation → proposal) through the real reflection-dispatch pipeline
/// (<c>AgentInteractionAggregate.ProcessAsync</c> + JSON round-trip + the production <c>Apply</c> handlers), so every
/// precondition is exercised by the actual handler, not a hand-seeded status. This proves that the ONLY way to reach a
/// proposal is the full happy Confirmation path, and that a safety-blocked generation can NEVER reach an approvable
/// proposal end-to-end (the structural enforcement of AC3, mirroring <c>ProviderCatalogLifecycleE2ETests</c>).
/// </summary>
public sealed class AgentInteractionProposalLifecycleE2ETests
{
    // A Confirmation-mode request command (the sample request is Automatic; the proposal path needs Confirmation).
    private static RequestAgentInteraction ConfirmationRequest { get; } = new(
        AgentId,
        SourceConversationId,
        CallerPartyId,
        Prompt,
        IdempotencyKey,
        ConfirmationSnapshot,
        ClientCorrelationId);

    [Fact]
    public async Task A_confirmation_interaction_reaches_a_pending_proposal_through_the_full_command_chain()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        // Request → Requested (the AD-4 Confirmation snapshot is frozen here).
        DomainResult requested = await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        requested.IsSuccess.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.Requested);
        state.Snapshot.ShouldNotBeNull().ResponseMode.ShouldBe(AgentResponseMode.Confirmation);

        // Gate (all checks satisfied) → Authorized.
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        state.Status.ShouldBe(AgentInteractionStatus.Authorized);

        // Context (fits the budget) → ContextReady.
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);

        // Generation (success) → Generated, with the approvable version on the append-only history.
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        state.GeneratedVersions.ShouldNotBeNull().Count.ShouldBe(1);

        // Proposal creation (Confirmation + Generated + Created) → ProposalCreated, Pending, safe evidence recorded (AC1).
        DomainResult proposed = await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));

        ProposedAgentReplyCreated created = proposed.Events[0].ShouldBeOfType<ProposedAgentReplyCreated>();
        created.Evidence.ProposalId.ShouldBe(SampleProposalId);
        created.Evidence.SourceConversationId.ShouldBe(SourceConversationId);
        created.Evidence.ProposedVersionId.ShouldBe(PostedVersionId);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.ProposalEvidence.ShouldNotBeNull().ProposalId.ShouldBe(SampleProposalId);
        state.ProposalCreationFailureReason.ShouldBeNull();

        // AC2/AD-14: the proposal outcome that survived the full reflection-dispatch + JSON round-trip carries no content.
        JsonSerializer.Serialize(created).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task A_safety_blocked_confirmation_interaction_can_never_reach_a_proposal_end_to_end()
    {
        // AC3 end-to-end: a generation that fails Content Safety records SafetyFailed and creates no approvable version, so a
        // subsequent create command can only be structurally rejected (OutputNotGenerated) — there is nothing to propose.
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));

        // Generation fails Content Safety → SafetyFailed; no version is appended (AD-5).
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SafetyBlockedGenerationResult()));
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        state.GeneratedVersions.ShouldBeNull();

        // The create command can never yield a proposal on a safety-failed interaction — structural rejection, no state change.
        DomainResult proposed = await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));

        proposed.IsRejection.ShouldBeTrue();
        proposed.Events[0].ShouldBeOfType<ProposedAgentReplyNotCreatableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotCreatableReason.OutputNotGenerated);
        proposed.Events.OfType<ProposedAgentReplyCreated>().ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed); // unchanged — no proposal exists
        state.ProposalState.ShouldBeNull();
        state.ProposalEvidence.ShouldBeNull();
    }
}
