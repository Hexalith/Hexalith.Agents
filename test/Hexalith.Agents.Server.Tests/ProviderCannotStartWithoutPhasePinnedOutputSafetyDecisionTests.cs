using System.Text.Json;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Malformed exact authority follows the same negative Provider branch as missing/Open authority.</summary>
public sealed class ProviderCannotStartWithoutPhasePinnedOutputSafetyDecisionTests
{
    [Theory]
    [InlineData(0, "valid", AgentInteractionStatus.GenerationFailed)]
    [InlineData(1, "malformed", AgentInteractionStatus.GenerationFailed)]
    [InlineData(1, "valid", AgentInteractionStatus.Unknown)]
    [InlineData(1, "valid", AgentInteractionStatus.Posted)]
    public async Task MalformedAuthorityNeverInvokesProvider(long revision, string digest, AgentInteractionStatus status)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        fixture.Effects.AuthorityOverride = new(revision, digest == "valid" ? new string('b', 64) : digest, status, "reason", "metric", "open", 1, new string('c', 64));
        await fixture.StartAsync(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationNotAuthorized]
            .OutputAuthorityRefusal.ShouldBe(ProviderOutputAuthorityRefusal.Malformed);
        fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(AutomaticInteractionOutcome.Authorized)]
    [InlineData(AutomaticInteractionOutcome.Posted)]
    public async Task InconsistentAuthorityEnvelopeRecordsAMalformedRefusal(AutomaticInteractionOutcome outcome)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        fixture.Effects.AuthorityResultOverride = new AutomaticInteractionEffectResult(outcome, "owner-record",
            Authority: new(1, new string('b', 64), AgentInteractionStatus.GenerationFailed, "reason", "metric", "open", 1, new string('c', 64)))
        {
            OutputAuthorityRefusal = ProviderOutputAuthorityRefusal.Open,
        };
        await fixture.StartAsync();
        await fixture.CompleteAsync();

        var refusal = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationNotAuthorized];
        refusal.OutputAuthorityRefusal.ShouldBe(ProviderOutputAuthorityRefusal.Malformed);
        refusal.OutputAuthority.ShouldBeNull();
        fixture.Effects.Attempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClosingAfterLeaseCommitRecordsClosingProvenanceWithoutReadingAuthority()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        await fixture.StartAsync();
        await fixture.ReserveUntilAsync(ConversationEffectKind.ProviderInvocation);
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);
        await fixture.CloseAsync();
        await fixture.CompleteAsync();

        var refusal = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationNotAuthorized];
        refusal.OutputAuthorityRefusal.ShouldBeNull();
        refusal.DeletionRequestId.ShouldBe(fixture.Directory!.DeletionRequestId);
        refusal.DirectoryClosingRevision.ShouldBe(fixture.Directory.DeletionClosingRevision);
        fixture.Effects.Observed.ShouldNotContain(AutomaticInteractionStep.ProviderInvocationAuthorized);
        fixture.Effects.Attempts.ShouldBeEmpty();
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided]
            .CapacityAdmissionId.ShouldBe(fixture.Interaction.PermittedCreation!.AdmissionId);
    }

    [Fact]
    public async Task AggregateRefusesInvalidProvenanceAndPreservesExactBranchReplay()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        await fixture.StartAsync();
        await fixture.ReserveUntilAsync(ConversationEffectKind.ProviderInvocation);
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);

        const AutomaticInteractionStep step = AutomaticInteractionStep.ProviderInvocationNotAuthorized;
        var lease = fixture.Directory!.Leases.Values.Single(item => item.Kind == ConversationEffectKind.ProviderInvocation);
        long revision = fixture.Interaction!.AutomaticRevision;
        string operation = AgentsSecurityIdentity.Derive("interaction-step", AutomaticInteractionReplayFixture.Tenant, fixture.InteractionId, step.ToString());
        var bare = new AutomaticInteractionFact(step, operation, AutomaticInteractionOutcome.Blocked, revision + 1, lease.LeaseId, lease.CommitRevision);
        var closing = bare with { DeletionRequestId = new string('d', 64), DirectoryClosingRevision = 5 };
        foreach (var invalid in new[]
        {
            bare,
            closing with { OutputAuthorityRefusal = ProviderOutputAuthorityRefusal.Unknown },
            closing with { OutputAuthorityRefusal = ProviderOutputAuthorityRefusal.Unavailable },
            bare with { OutputAuthorityRefusal = ProviderOutputAuthorityRefusal.Open },
        })
        {
            Reject(invalid).ShouldBe(AutomaticInteractionRejectionReason.InvalidAuthority);
            fixture.Interaction.AutomaticRevision.ShouldBe(revision);
        }

        var valid = bare with { OutputAuthorityRefusal = ProviderOutputAuthorityRefusal.Missing };
        await SubmitAsync(valid);
        await SubmitAsync(valid);
        fixture.Interaction.AutomaticFacts[step].ShouldBe(valid);
        Reject(valid with { OutputAuthorityRefusal = ProviderOutputAuthorityRefusal.Unavailable })
            .ShouldBe(AutomaticInteractionRejectionReason.DivergentDuplicate);

        Task SubmitAsync(AutomaticInteractionFact fact) => InteractionCommandDispatch.SendAsync(fixture, AutomaticInteractionReplayFixture.Tenant,
            "agent-interaction", fixture.InteractionId, fact.OperationId,
            new RecordAutomaticInteractionStep(AutomaticInteractionReplayFixture.Tenant, fixture.InteractionId, fact, revision), CancellationToken.None);

        AutomaticInteractionRejectionReason Reject(AutomaticInteractionFact fact)
        {
            const string tenant = AutomaticInteractionReplayFixture.Tenant;
            var command = new RecordAutomaticInteractionStep(tenant, fixture.InteractionId, fact, revision);
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(command);
            var envelope = new CommandEnvelope(operation, tenant, "agent-interaction", fixture.InteractionId, nameof(RecordAutomaticInteractionStep),
                payload, operation, null, "workflow", new Dictionary<string, string> { [ConversationAgentStateIdentity.WorkflowAuthority] = "true" });
            return AgentInteractionAggregate.Handle(JsonSerializer.Deserialize<RecordAutomaticInteractionStep>(payload)!, fixture.Interaction, envelope)
                .Events.ShouldHaveSingleItem().ShouldBeOfType<AutomaticInteractionRejected>().Reason;
        }
    }
}
