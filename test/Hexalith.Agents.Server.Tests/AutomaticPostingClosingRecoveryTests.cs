using System.Text.Json;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Server.Application.Workflows;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Automatic-mode Closing suppresses uncommitted posts and permits only a committed phase to finish and settle.</summary>
public sealed class AutomaticPostingClosingRecoveryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PostingCommitAndClosingConvergeWithoutChangingTheGeneratedVersion(bool commitFirst, bool restart)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ConversationPosting);
        string generated = JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]);
        string versionId = fixture.Interaction.PermittedCreation!.ProposalVersionId;
        int generationLookups = fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded);
        if (commitFirst) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        await fixture.CloseAsync();
        int afterClosing = fixture.Dispatches.Count; int leasesAtClosing = fixture.Directory!.Leases.Count;
        if (restart) { fixture.Restart(); }
        var result = await fixture.CompleteAsync();
        result.Status.ShouldBe(commitFirst ? WorkflowExecutionStatus.Completed : WorkflowExecutionStatus.Suppressed);
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion).ShouldBe(!commitFirst);
        if (!commitFirst)
        {
            var suppression = fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.ConversationPostingSuppressedByDeletion];
            suppression.Outcome.ShouldBe(AutomaticInteractionOutcome.Suppressed); suppression.LeaseCommitRevision.ShouldBe(0);
            suppression.DeletionRequestId.ShouldBe(fixture.Directory!.DeletionRequestId); suppression.DirectoryClosingRevision.ShouldBe(fixture.Directory.DeletionClosingRevision); suppression.DirectoryClosingRevision.ShouldBeGreaterThan(0);
        }
        fixture.Effects.Posts.Count.ShouldBe(commitFirst ? 1 : 0);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.ConversationPostingRecorded).ShouldBe(commitFirst ? 1 : 0);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generationLookups);
        JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]).ShouldBe(generated);
        fixture.Interaction.PermittedCreation!.ProposalVersionId.ShouldBe(versionId);
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.InteractionPreAcceptanceFailed).ShouldBeFalse();
        fixture.Directory!.Leases.Count.ShouldBe(leasesAtClosing);
        fixture.Directory.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Directory.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ConversationPosting).Status
            .ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Dispatches.Skip(afterClosing).Where(i => i.CommandType == nameof(AcquireConversationEffectLease) || i.CommandType == nameof(CommitConversationEffect)).ShouldBeEmpty();
        int commands = fixture.AcceptedCommandIds.Count;
        fixture.Restart(); (await fixture.CompleteAsync()).Status.ShouldBe(result.Status);
        fixture.AcceptedCommandIds.Count.ShouldBe(commands); fixture.Effects.Posts.Count.ShouldBe(commitFirst ? 1 : 0);
    }

    [Fact]
    public async Task AutomaticModeRecordsAWinningGeneratedMutationThenSuppressesPostingWithoutAcquiringANewLease()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ProposalMutation);
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);
        string versionId = fixture.Interaction!.PermittedCreation!.ProposalVersionId;
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation).Status.ShouldBe(ConversationEffectLeaseStatus.CommittedToEffect);
        await fixture.CloseAsync(); int afterClosing = fixture.Dispatches.Count; int leasesAtClosing = fixture.Directory.Leases.Count;
        fixture.Restart(); (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Suppressed);
        fixture.Interaction!.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion);
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.ConversationPostingSuppressedByDeletion].LeaseId.ShouldBeNull();
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.ConversationPostingSuppressedByDeletion].DirectoryClosingRevision.ShouldBe(fixture.Directory!.DeletionClosingRevision);
        var generated = fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded];
        generated.Outcome.ShouldBe(AutomaticInteractionOutcome.Allowed); generated.ContentDigest.ShouldBe(new string('d', 64));
        generated.Output.ShouldNotBeNull().Value.GetProperty("FixtureSealed").GetString().ShouldBe("allowed-output");
        fixture.Interaction.PermittedCreation!.ProposalVersionId.ShouldBe(versionId);
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GenerationFailed).ShouldBeFalse();
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.InteractionPreAcceptanceFailed).ShouldBeFalse();
        fixture.Directory!.Leases.Count.ShouldBe(leasesAtClosing); fixture.Directory.Leases.Values.Any(l => l.Kind == ConversationEffectKind.ConversationPosting).ShouldBeFalse();
        fixture.Directory.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Dispatches.Skip(afterClosing).Where(i => i.CommandType == nameof(AcquireConversationEffectLease) || i.CommandType == nameof(CommitConversationEffect)).ShouldBeEmpty();
        fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Effects.Observed.Contains(AutomaticInteractionStep.ConversationPostingRecorded).ShouldBeFalse();
    }

    [Fact]
    public async Task RecordedPostSuppressionSurvivesLostAcknowledgementAndRestart()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ConversationPosting);
        string generated = JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]);
        await fixture.CloseAsync(); fixture.FaultAt = fixture.Dispatches.Count; fixture.CrashAfterAppend = true;
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.CompleteAsync());
        fixture.Restart(); (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Suppressed);
        fixture.Interaction!.AutomaticFacts.Keys.Count(s => s == AutomaticInteractionStep.ConversationPostingSuppressedByDeletion).ShouldBe(1);
        JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]).ShouldBe(generated);
        fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }
}
