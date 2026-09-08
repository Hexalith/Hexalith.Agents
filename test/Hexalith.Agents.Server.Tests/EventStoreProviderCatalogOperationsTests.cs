namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Live public catalog surface (Story 5.3): accepted writes return a structured identity, reads serve the
/// projected catalog, and unauthorized callers learn nothing.
/// </summary>
public sealed class EventStoreProviderCatalogOperationsTests
{
    private const string TenantId = "acme";
    private const string StoreName = "statestore";

    private readonly FakeReadModelStore _store = new();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();

    public EventStoreProviderCatalogOperationsTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "admin-user", IsAgentsAdmin: true));
        _ = _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("corr-1", null, "msg-1"));
    }

    [Fact]
    public async Task An_accepted_create_answers_with_a_structured_identity_and_the_submitted_stage()
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeTrue();
        ProviderCatalogCommandAcceptance acceptance = result.Value.ShouldNotBeNull();
        acceptance.ProviderId.ShouldBe("openai");
        acceptance.ModelId.ShouldBe("gpt-4o");
        acceptance.MessageId.ShouldNotBeNullOrWhiteSpace();
        acceptance.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        acceptance.TruthState.ShouldBe(AgentSetupTruthState.Submitted);
    }

    [Fact]
    public async Task An_acceptance_carries_no_eventstore_or_secret_internals()
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        string serialized = JsonSerializer.Serialize(result.Value);
        serialized.ShouldNotContain("revision", Case.Insensitive);
        serialized.ShouldNotContain("stream", Case.Insensitive);
        serialized.ShouldNotContain("aggregate", Case.Insensitive);
        serialized.ShouldNotContain("cfg-openai-gpt4o");
        serialized.ShouldNotContain("sk-");
    }

    [Fact]
    public async Task A_write_then_projection_then_read_reaches_projection_confirmed()
    {
        _ = await Operations().CreateEntryAsync(CreateCommand());

        ProviderCatalogInspectionResult before = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 1))
            .Value
            .ShouldNotBeNull();
        before.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        before.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        before.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        before.Entries.ShouldBeEmpty();

        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult after = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 1))
            .Value
            .ShouldNotBeNull();
        after.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        after.Entries.ShouldHaveSingleItem().Pricing.ShouldNotBeNull().Currency.ShouldBe("USD");
        after.Entries[0].IsSelectableForNewActiveUse.ShouldBeTrue();
    }

    [Fact]
    public async Task A_read_that_outruns_the_projection_is_authoritative_pending()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 2))
            .Value
            .ShouldNotBeNull();

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem().CapabilityVersion.ShouldBe(1);
    }

    [Fact]
    public async Task A_list_waiting_on_a_newer_projection_version_is_authoritative_pending()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true, expectedProjectionVersion: "9"))
            .Value
            .ShouldNotBeNull();

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_unparseable_expected_projection_version_is_treated_as_behind()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true, expectedProjectionVersion: "not-a-number"))
            .Value
            .ShouldNotBeNull();

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task An_unauthorized_write_does_not_dispatch()
    {
        _contextProvider.GetContext().Returns(AgentAdministrationContext.Anonymous);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task Select_provider_model_stays_out_of_scope_on_the_administration_surface()
    {
        IAgentAdministrationOperations administration = new EventStoreAgentAdministrationOperations(
            _contextProvider,
            new AgentAdministrationOrchestrator(new EventStoreAgentCommandDispatcher(_gateway)),
            new AgentResponseModeOrchestrator(new EventStoreAgentCommandDispatcher(_gateway)),
            new AgentActivationProviderRevalidation(
                Substitute.For<IProviderCatalogReader>(),
                Substitute.For<IApproverPolicyResolver>(),
                new EventStoreAgentCommandDispatcher(_gateway)),
            _store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory());

        AgentOperationResult result = await administration.SelectProviderModelAsync(
            new Hexalith.Agents.Contracts.Agent.Commands.SelectAgentProviderModel("openai", "gpt-4o", 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Unavailable);
    }

    private IProviderCatalogOperations Operations()
    {
        var dispatcher = new EventStoreAgentCommandDispatcher(_gateway);
        return new EventStoreProviderCatalogOperations(
            _contextProvider,
            new ProviderCatalogAdministrationOrchestrator(dispatcher),
            _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory());
    }

    private void SeedProjectedEntry(int capabilityVersion)
        => _store.Seed(
            StoreName,
            ProviderCatalogReadModelAddresses.Detail(TenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = TenantId,
                TenantId = TenantId,
                Entries = [SelectableEntry(capabilityVersion)],
                LastSequenceNumber = capabilityVersion,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = capabilityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private static CreateProviderModelEntry CreateCommand()
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            Enabled: true,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            "cfg-openai-gpt4o",
            new ProviderModelPricing("USD", 0.002m, 0.008m, 0));

    private static ProviderCatalogEntryView SelectableEntry(int capabilityVersion)
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            ProviderModelStatus.Enabled,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: true,
            capabilityVersion,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 1));
}
