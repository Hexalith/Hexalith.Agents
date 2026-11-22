using System.Reflection;
using System.Text.Json;
using Dapr.Workflow;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>AD-27 reflection sweep and poison-input rejection cover every workflow and activity contract.</summary>
public sealed class WorkflowExecutionStateBoundaryTests
{
    [Fact]
    public void EveryWorkflowAndActivityContractContainsOnlyIdsRevisionsDigestsSafeEnumsAndProtectedReferences()
    {
        var contracts = typeof(AutomaticInteractionWorkflow).Assembly.GetTypes().Where(t => t.BaseType?.IsGenericType == true)
            .Select(t => t.BaseType!).Where(b => b.GetGenericTypeDefinition() == typeof(Workflow<,>) || b.GetGenericTypeDefinition() == typeof(WorkflowActivity<,>))
            .SelectMany(b => b.GetGenericArguments()).Distinct().ToArray();
        contracts.ShouldBe([typeof(WorkflowExecutionState)]);
        foreach (Type contract in contracts.Append(typeof(ProtectedContentReference)))
        {
            foreach (PropertyInfo property in contract.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Type value = property.PropertyType;
                bool allowed = value.IsEnum || value == typeof(ProtectedContentReference) || value == typeof(long) && property.Name.EndsWith("Revision", StringComparison.Ordinal)
                    || value == typeof(string) && (property.Name.EndsWith("Id", StringComparison.Ordinal) || property.Name.EndsWith("Digest", StringComparison.Ordinal));
                allowed.ShouldBeTrue($"AD-27 forbidden contract member {contract.Name}.{property.Name}");
            }
        }
    }
    [Theory]
    [InlineData("a plaintext prompt goes here")]
    [InlineData("tenant\npoison")]
    [InlineData("")]
    public void NonIdentityTextIsRejectedBeforeScheduling(string poison)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        Should.Throw<ArgumentException>(() => WorkflowExecutionStateValidation.Validate(fixture.Input with { TenantId = poison }));
        fixture.Scheduled.ShouldBeEmpty();
    }
    [Fact]
    public void UnknownContentMembersAndCrossTenantReferencesCannotBeSilentlyIgnored()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        string json = JsonSerializer.Serialize(fixture.Input); json = json[..^1] + ",\"Prompt\":\"poison\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<WorkflowExecutionState>(json));
        Should.Throw<ArgumentException>(() => WorkflowExecutionStateValidation.Validate(fixture.Input with { RequestReference = fixture.Input.RequestReference with { TenantId = "tenant-b" } }));
        Should.Throw<ArgumentException>(() => WorkflowExecutionStateValidation.Validate(fixture.Input with { Status = (WorkflowExecutionStatus)999 }));
    }

    [Theory]
    [InlineData("plaintext-conversation")]
    [InlineData("non-digest-interaction")]
    [InlineData("plaintext-reference-digest")]
    [InlineData("zero-workflow-revision")]
    [InlineData("negative-workflow-revision")]
    [InlineData("zero-reference-revision")]
    [InlineData("negative-reference-revision")]
    public async Task MalformedInputIsRefusedBeforeSchedulingOrReadingOwners(string invalidField)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var client = Substitute.For<IDaprWorkflowClient>();
        var scheduler = new DaprInteractionWorkflowScheduler(client);
        var directory = Substitute.For<IConversationAgentStateReader>();
        var interaction = Substitute.For<IAgentInteractionStateReader>();
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        var effects = Substitute.For<IAutomaticInteractionStepEffects>();
        var steps = new AutomaticInteractionStepOrchestrator(directory, interaction, dispatcher, effects);
        var workflowContext = Substitute.For<WorkflowContext>();
        var input = fixture.Input;
        var malformed = invalidField switch
        {
            "plaintext-conversation" => input with { ConversationId = "a plaintext request" },
            "non-digest-interaction" => input with
            {
                AgentInteractionId = "not-a-digest",
                RequestReference = input.RequestReference with { AgentInteractionId = "not-a-digest" },
            },
            "plaintext-reference-digest" => input with
            {
                RequestReference = input.RequestReference with { ContentDigest = "a plaintext request" },
            },
            "zero-workflow-revision" => input with { EventRevision = 0 },
            "negative-workflow-revision" => input with { EventRevision = -1 },
            "zero-reference-revision" => input with { RequestReference = input.RequestReference with { EventRevision = 0 } },
            "negative-reference-revision" => input with { RequestReference = input.RequestReference with { EventRevision = -1 } },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidField)),
        };

        workflowContext.InstanceId.Returns(malformed.AgentInteractionId);

        var scheduleError = await Should.ThrowAsync<ArgumentException>(() => scheduler.EnsureScheduledAsync(malformed, CancellationToken.None));
        var stepError = await Should.ThrowAsync<ArgumentException>(() => steps.ExecuteAsync(malformed, CancellationToken.None));
        var workflowError = await Should.ThrowAsync<ArgumentException>(() => new AutomaticInteractionWorkflow().RunAsync(workflowContext, malformed));

        scheduleError.Message.ShouldStartWith("InvalidWorkflowExecutionState");
        stepError.Message.ShouldStartWith("InvalidWorkflowExecutionState");
        workflowError.Message.ShouldStartWith("InvalidWorkflowExecutionState");
        workflowContext.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == nameof(WorkflowContext.CallActivityAsync));
        client.ReceivedCalls().ShouldBeEmpty();
        directory.ReceivedCalls().ShouldBeEmpty();
        interaction.ReceivedCalls().ShouldBeEmpty();
        dispatcher.ReceivedCalls().ShouldBeEmpty();
        effects.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("digest")]
    [InlineData("revision")]
    public async Task ReferenceMustMatchThePersistedRequestBeforeExecutingAStep(string mismatchedField)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var scheduler = Substitute.For<IInteractionWorkflowScheduler>();
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, fixture, scheduler);
        var intake = new AutomaticInteractionIntakeOrchestrator(
            fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, fixture, fixture, recovery);
        (await intake.ExecuteAsync(fixture.Request, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.Restart();
        var input = scheduler.ReceivedCalls().ShouldHaveSingleItem().GetArguments()[0].ShouldBeOfType<WorkflowExecutionState>();
        input.RequestReference.ContentDigest.ShouldBe(fixture.Directory.ShouldNotBeNull().Permits[fixture.InteractionId].Permit.RequestDigest);
        input.RequestReference.EventRevision.ShouldBe(fixture.Interaction.ShouldNotBeNull().PermittedCreation.ShouldNotBeNull().Revision);
        var mismatched = input with
        {
            RequestReference = mismatchedField == "digest"
                ? input.RequestReference with { ContentDigest = new string('b', 64) }
                : input.RequestReference with { EventRevision = input.RequestReference.EventRevision + 1 },
        };
        string before = fixture.Snapshot();
        int dispatchCount = fixture.Dispatches.Count;

        var error = await Should.ThrowAsync<InvalidOperationException>(() => fixture.Steps().ExecuteAsync(mismatched, CancellationToken.None));

        error.Message.ShouldBe("InvalidAuthoritativeInteraction");
        fixture.Dispatches.Count.ShouldBe(dispatchCount);
        fixture.Snapshot().ShouldBe(before);
        fixture.Effects.Observed.ShouldBeEmpty();
        fixture.Effects.Settlements.ShouldBeEmpty();

        var next = await fixture.Steps().ExecuteAsync(input, CancellationToken.None);
        next.EventRevision.ShouldBeGreaterThan(input.EventRevision);
        fixture.Interaction.ShouldNotBeNull().AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.WorkflowStartCheckpoint);
    }
}
