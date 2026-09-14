using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Verifies the bounded command result evidence used to correlate setup writes with their projected version.
/// </summary>
public sealed class AgentSetupDomainResultTests
{
    [Fact]
    public void EveryEventfulSetupHandlerCarriesAppliedEffectAndItsEmittedVersion()
    {
        foreach ((string commandName, DomainResult result, int expectedVersion) in EventfulCases())
        {
            result.IsSuccess.ShouldBeTrue(commandName);
            IEventPayload emitted = result.Events.Single();
            int emittedVersion = ConfigurationVersionOf(emitted);
            emittedVersion.ShouldBe(expectedVersion, commandName);
            AssertPayload(result, "Applied", emittedVersion, commandName);
        }
    }

    [Fact]
    public void EveryNoOpCapableSetupHandlerCarriesAlreadyAppliedEffectAndItsUnchangedVersion()
    {
        foreach ((string commandName, DomainResult result, int unchangedVersion) in NoOpCases())
        {
            result.IsNoOp.ShouldBeTrue(commandName);
            AssertPayload(result, "AlreadyApplied", unchangedVersion, commandName);
        }
    }

    [Fact]
    public void DomainRejectionCarriesNoResultPayload()
    {
        var command = new UpdateAgentConfiguration("hexa", null, ValidInstructions);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.ResultPayload.ShouldBeNull();
    }

    private static IEnumerable<(string CommandName, DomainResult Result, int ExpectedVersion)> EventfulCases()
    {
        CreateAgent create = ValidCreate();
        yield return (nameof(CreateAgent), AgentAggregate.Handle(create, state: null, Envelope(create)), 1);

        AgentState updateState = StateWith(create);
        var update = new UpdateAgentConfiguration(
            "renamed hexa",
            null,
            "You are hexa with updated, careful instructions.");
        yield return (
            nameof(UpdateAgentConfiguration),
            AgentAggregate.Handle(update, updateState, Envelope(update)),
            updateState.ConfigurationVersion + 1);

        AgentState activationState = StateWithSelectedProvider(create);
        var activate = new ActivateAgent();
        yield return (
            nameof(ActivateAgent),
            AgentAggregate.Handle(activate, activationState, ActivateEnvelope()),
            activationState.ConfigurationVersion + 1);

        AgentState disableState = StateWith(create);
        var disable = new DisableAgent();
        yield return (
            nameof(DisableAgent),
            AgentAggregate.Handle(disable, disableState, Envelope(disable)),
            disableState.ConfigurationVersion + 1);

        AgentState linkState = StateWith(create);
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        yield return (
            nameof(LinkAgentPartyIdentity),
            AgentAggregate.Handle(link, linkState, LinkEnvelope(link)),
            linkState.ConfigurationVersion + 1);

        AgentState replaceState = StateWithLinkedParty(create);
        var replace = new ReplaceAgentPartyIdentity("party-002");
        yield return (
            nameof(ReplaceAgentPartyIdentity),
            AgentAggregate.Handle(replace, replaceState, LinkEnvelope(replace)),
            replaceState.ConfigurationVersion + 1);

        AgentState providerState = StateWith(create);
        var provider = new SelectAgentProviderModel(SelectedProviderId, SelectedModelId, SelectedCapabilityVersion);
        yield return (
            nameof(SelectAgentProviderModel),
            AgentAggregate.Handle(provider, providerState, SelectEnvelope(provider)),
            providerState.ConfigurationVersion + 1);

        AgentState responseModeState = StateWith(create);
        var responseMode = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);
        yield return (
            nameof(ConfigureAgentResponseMode),
            AgentAggregate.Handle(responseMode, responseModeState, Envelope(responseMode)),
            responseModeState.ConfigurationVersion + 1);

        AgentState approverState = StateWith(create);
        var approver = new ConfigureAgentApproverPolicy(SampleApproverPolicy);
        yield return (
            nameof(ConfigureAgentApproverPolicy),
            AgentAggregate.Handle(approver, approverState, Envelope(approver)),
            approverState.ConfigurationVersion + 1);

        AgentState contentSafetyState = StateWith(create);
        var contentSafety = new ConfigureAgentContentSafetyPolicy(SampleContentSafetyConfiguration);
        yield return (
            nameof(ConfigureAgentContentSafetyPolicy),
            AgentAggregate.Handle(contentSafety, contentSafetyState, Envelope(contentSafety)),
            contentSafetyState.ConfigurationVersion + 1);

        AgentState readinessState = StateWith(create);
        var readiness = new RecordAgentLaunchReadiness(SampleLaunchReadiness);
        yield return (
            nameof(RecordAgentLaunchReadiness),
            AgentAggregate.Handle(readiness, readinessState, Envelope(readiness)),
            readinessState.ConfigurationVersion + 1);

        AgentState enableState = StateLaunchReady(create);
        var enable = new EnableProductionLikeGeneration();
        yield return (
            nameof(EnableProductionLikeGeneration),
            AgentAggregate.Handle(enable, enableState, EnableEnvelope()),
            enableState.ConfigurationVersion + 1);
    }

    private static IEnumerable<(string CommandName, DomainResult Result, int UnchangedVersion)> NoOpCases()
    {
        CreateAgent create = ValidCreate();
        AgentState createState = StateWith(create);
        yield return (nameof(CreateAgent), AgentAggregate.Handle(create, createState, Envelope(create)), createState.ConfigurationVersion);

        AgentState updateState = StateWith(create);
        var update = new UpdateAgentConfiguration(create.DisplayName, create.Description, create.Instructions);
        yield return (
            nameof(UpdateAgentConfiguration),
            AgentAggregate.Handle(update, updateState, Envelope(update)),
            updateState.ConfigurationVersion);

        AgentState linkState = StateWithLinkedParty(create);
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        yield return (
            nameof(LinkAgentPartyIdentity),
            AgentAggregate.Handle(link, linkState, LinkEnvelope(link)),
            linkState.ConfigurationVersion);

        AgentState replaceState = StateWithLinkedParty(create);
        var replace = new ReplaceAgentPartyIdentity(LinkedPartyId);
        yield return (
            nameof(ReplaceAgentPartyIdentity),
            AgentAggregate.Handle(replace, replaceState, LinkEnvelope(replace)),
            replaceState.ConfigurationVersion);

        AgentState providerState = StateWithSelectedProvider(create);
        var provider = new SelectAgentProviderModel(SelectedProviderId, SelectedModelId, SelectedCapabilityVersion);
        yield return (
            nameof(SelectAgentProviderModel),
            AgentAggregate.Handle(provider, providerState, SelectEnvelope(provider)),
            providerState.ConfigurationVersion);

        AgentState responseModeState = StateWithResponseMode(create);
        var responseMode = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);
        yield return (
            nameof(ConfigureAgentResponseMode),
            AgentAggregate.Handle(responseMode, responseModeState, Envelope(responseMode)),
            responseModeState.ConfigurationVersion);

        AgentState approverState = StateWithApproverPolicy(create);
        var approver = new ConfigureAgentApproverPolicy(SampleApproverPolicy);
        yield return (
            nameof(ConfigureAgentApproverPolicy),
            AgentAggregate.Handle(approver, approverState, Envelope(approver)),
            approverState.ConfigurationVersion);

        AgentState contentSafetyState = StateWithContentSafety(create);
        var contentSafety = new ConfigureAgentContentSafetyPolicy(SampleContentSafetyConfiguration);
        yield return (
            nameof(ConfigureAgentContentSafetyPolicy),
            AgentAggregate.Handle(contentSafety, contentSafetyState, Envelope(contentSafety)),
            contentSafetyState.ConfigurationVersion);

        AgentState readinessState = StateWithLaunchReadiness(create);
        var readiness = new RecordAgentLaunchReadiness(SampleLaunchReadiness);
        yield return (
            nameof(RecordAgentLaunchReadiness),
            AgentAggregate.Handle(readiness, readinessState, Envelope(readiness)),
            readinessState.ConfigurationVersion);

        AgentState enableState = StateLaunchReady(create);
        enableState.Apply(new AgentProductionLikeGenerationEnabled(AgentId, enableState.ConfigurationVersion + 1));
        var enable = new EnableProductionLikeGeneration();
        yield return (
            nameof(EnableProductionLikeGeneration),
            AgentAggregate.Handle(enable, enableState, EnableEnvelope()),
            enableState.ConfigurationVersion);
    }

    private static int ConfigurationVersionOf(IEventPayload payload)
        => payload switch
        {
            AgentCreated value => value.ConfigurationVersion,
            AgentConfigurationUpdated value => value.ConfigurationVersion,
            AgentActivated value => value.ConfigurationVersion,
            AgentDisabled value => value.ConfigurationVersion,
            AgentPartyIdentityLinked value => value.ConfigurationVersion,
            AgentPartyIdentityReplaced value => value.ConfigurationVersion,
            AgentProviderModelSelected value => value.ConfigurationVersion,
            AgentResponseModeConfigured value => value.ConfigurationVersion,
            AgentApproverPolicyConfigured value => value.ConfigurationVersion,
            AgentContentSafetyPolicyConfigured value => value.ConfigurationVersion,
            AgentLaunchReadinessRecorded value => value.ConfigurationVersion,
            AgentProductionLikeGenerationEnabled value => value.ConfigurationVersion,
            _ => throw new InvalidOperationException($"Unexpected setup event '{payload.GetType().Name}'."),
        };

    private static void AssertPayload(
        DomainResult result,
        string expectedEffect,
        int expectedVersion,
        string commandName)
    {
        using JsonDocument payload = JsonDocument.Parse(result.ResultPayload.ShouldNotBeNull(commandName));
        payload.RootElement.EnumerateObject().Count().ShouldBe(2, commandName);
        payload.RootElement.GetProperty("effect").GetString().ShouldBe(expectedEffect, commandName);
        payload.RootElement.GetProperty("configurationVersion").GetInt32().ShouldBe(expectedVersion, commandName);
    }
}
