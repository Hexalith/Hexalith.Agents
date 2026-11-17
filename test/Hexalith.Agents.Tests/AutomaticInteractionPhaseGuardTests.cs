using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.Security;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Isolated invalid phase facts fail at their exact guard after a publicly accepted phase prefix.</summary>
public sealed class AutomaticInteractionPhaseGuardTests
{
    [Theory]
    [InlineData("expected-revision", AutomaticInteractionRejectionReason.StaleRevision)]
    [InlineData("fact-revision", AutomaticInteractionRejectionReason.StaleRevision)]
    [InlineData("ordering", AutomaticInteractionRejectionReason.InvalidPhase)]
    [InlineData("lease-zero", AutomaticInteractionRejectionReason.LeaseCommitRequired)]
    [InlineData("lease-negative", AutomaticInteractionRejectionReason.LeaseCommitRequired)]
    [InlineData("lease-missing", AutomaticInteractionRejectionReason.LeaseCommitRequired)]
    [InlineData("lease-foreign", AutomaticInteractionRejectionReason.LeaseCommitRequired)]
    [InlineData("authority-missing", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-revision", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-digest", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-status", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-catalog-revision", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-observation", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-reason", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-metric", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-disposition", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("authority-undefined-status", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("lease-other-phase", AutomaticInteractionRejectionReason.LeaseCommitRequired)]
    [InlineData("content-digest-misplaced", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("digest-key-version-misplaced", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("authority-misplaced", AutomaticInteractionRejectionReason.InvalidAuthority)]
    [InlineData("output-missing", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("output-tenant", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("output-interaction", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("output-alias", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("output-digest", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("output-key-version", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    [InlineData("output-misplaced", AutomaticInteractionRejectionReason.ProtectedContentRequired)]
    public void InvalidFactRejectsForItsOwnReasonAndReplaysWithoutAcceptedPhaseChange(string invalid, AutomaticInteractionRejectionReason reason)
    {
        ArgumentNullException.ThrowIfNull(invalid);
        var target = invalid == "lease-other-phase" ? AutomaticInteractionStep.ProviderInvocationAuthorized : invalid.StartsWith("authority-", StringComparison.Ordinal) && invalid != "authority-misplaced"
            ? AutomaticInteractionStep.ProviderInvocationAuthorized
            : invalid.StartsWith("output-", StringComparison.Ordinal) && invalid != "output-misplaced"
                ? AutomaticInteractionStep.GeneratedVersionRecorded : invalid == "ordering"
                    ? AutomaticInteractionStep.OpenInteractionLeaseAuthorized : AutomaticInteractionStep.WorkflowStartCheckpoint;
        var (state, envelope, valid) = ConversationDirectoryTestData.PhasePrefix(target);
        var command = new RecordAutomaticInteractionStep(envelope.TenantId, state.AgentInteractionId, valid, state.AutomaticRevision);
        var fact = invalid switch
        {
            "fact-revision" => valid with { Revision = valid.Revision - 1 },
            "ordering" => valid with { Step = AutomaticInteractionStep.RateAdmissionRecorded,
                OperationId = AgentsSecurityIdentity.Derive("interaction-step", envelope.TenantId, state.AgentInteractionId, AutomaticInteractionStep.RateAdmissionRecorded.ToString()), Outcome = AutomaticInteractionOutcome.Allowed },
            "lease-zero" => valid with { LeaseCommitRevision = 0 },
            "lease-negative" => valid with { LeaseCommitRevision = -1 },
            "lease-missing" => valid with { LeaseId = null },
            "lease-foreign" => valid with { LeaseId = new string('f', 64) },
            "authority-missing" => valid with { OutputAuthority = null },
            "authority-revision" => valid with { OutputAuthority = valid.OutputAuthority! with { DecisionRevision = 0 } },
            "authority-digest" => valid with { OutputAuthority = valid.OutputAuthority! with { ContractDigest = "invalid" } },
            "authority-status" => valid with { OutputAuthority = valid.OutputAuthority! with { FailureStatus = AgentInteractionStatus.Unknown } },
            "authority-catalog-revision" => valid with { OutputAuthority = valid.OutputAuthority! with { CatalogActivationRevision = 0 } },
            "authority-observation" => valid with { OutputAuthority = valid.OutputAuthority! with { ObservationId = "invalid" } },
            "authority-reason" => valid with { OutputAuthority = valid.OutputAuthority! with { ReasonId = " " } },
            "authority-metric" => valid with { OutputAuthority = valid.OutputAuthority! with { MetricId = " " } },
            "authority-disposition" => valid with { OutputAuthority = valid.OutputAuthority! with { OpenLeaseDispositionId = " " } },
            "authority-undefined-status" => valid with { OutputAuthority = valid.OutputAuthority! with { FailureStatus = (AgentInteractionStatus)int.MaxValue } },
            "lease-other-phase" => valid with { LeaseId = state.AutomaticFacts[AutomaticInteractionStep.ContextReadRecorded].LeaseId,
                LeaseCommitRevision = state.AutomaticFacts[AutomaticInteractionStep.ContextReadRecorded].LeaseCommitRevision },
            "content-digest-misplaced" => valid with { ContentDigest = new string('a', 64) },
            "digest-key-version-misplaced" => valid with { DigestKeyVersion = "fixture-digest-v1" },
            "authority-misplaced" => valid with { OutputAuthority = new(1, new string('a', 64), AgentInteractionStatus.SafetyFailed, "reason", "metric", "disposition", 1, new string('b', 64)) },
            "output-missing" => valid with { Output = null },
            "output-tenant" => valid with { Output = valid.Output! with { Target = valid.Output.Target with { TenantId = "other-tenant" } } },
            "output-interaction" => valid with { Output = valid.Output! with { Target = valid.Output.Target with { AgentInteractionId = "other-interaction" } } },
            "output-alias" => valid with { Output = valid.Output! with { Target = valid.Output.Target with { TargetProtectionKeyAlias = "other-alias" } } },
            "output-digest" => valid with { ContentDigest = "invalid" },
            "output-key-version" => valid with { DigestKeyVersion = " " },
            "output-misplaced" => valid with { Output = state.PermittedCreation!.Permit.Request },
            _ => valid,
        };
        var submitted = command with { Fact = fact, ExpectedRevision = invalid == "expected-revision" ? command.ExpectedRevision - 1 : command.ExpectedRevision };
        string before = JsonSerializer.Serialize(state);
        var result = AgentInteractionAggregate.Handle(submitted, state, envelope);
        result.IsRejection.ShouldBeTrue();
        var rejection = result.Events.Single().ShouldBeOfType<AutomaticInteractionRejected>(); rejection.Reason.ShouldBe(reason);
        state.Apply(rejection); JsonSerializer.Serialize(state).ShouldBe(before);
        state.AutomaticFacts.ContainsKey(fact.Step).ShouldBeFalse();
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        JsonSerializer.Serialize(restored).ShouldBe(before);
        var accepted = AgentInteractionAggregate.Handle(command, restored, envelope);
        accepted.IsRejection.ShouldBeFalse(); accepted.IsNoOp.ShouldBeFalse(); accepted.Events.ShouldNotBeEmpty();
        foreach (var item in accepted.Events) { restored.Apply((dynamic)item); }
        restored.AutomaticFacts[target].ShouldBe(valid);
        restored.AutomaticRevision.ShouldBeGreaterThan(state.AutomaticRevision);
        AgentInteractionAggregate.Handle(command, restored, envelope).IsNoOp.ShouldBeTrue();
    }
}
