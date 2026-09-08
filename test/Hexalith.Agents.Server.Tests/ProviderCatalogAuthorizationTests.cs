using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>
/// Story 5.3 authorization and no-disclosure: unauthorized and cross-tenant catalog reads fail closed before
/// lookup-dependent disclosure, including selection through <see cref="IProviderCatalogReader"/>.
/// </summary>
public sealed class ProviderCatalogAuthorizationTests
{
    private const string TenantId = "acme";
    private const string OtherTenant = "other-tenant";
    private const string StoreName = "statestore";

    private readonly FakeReadModelStore _store = new();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();

    public ProviderCatalogAuthorizationTests()
        => SeedCatalog();

    [Fact]
    public async Task CrossTenantSelectionIsDenied()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "admin-user", IsAgentsAdmin: true));
        ProjectedProviderCatalogReader reader = Reader();

        ProviderCatalogEntryReadResult result = await reader.GetEntryAsync(
            OtherTenant,
            "openai",
            "gpt-4o",
            CancellationToken.None);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entry.ShouldBeNull();
        JsonSerializer.Serialize(result).ShouldNotContain("openai");
        JsonSerializer.Serialize(result).ShouldNotContain(OtherTenant);
        _store.Snapshot<ProviderCatalogReadModel>(StoreName, ProviderCatalogReadModelAddresses.Detail(OtherTenant))
            .ShouldBeNull();
    }

    [Fact]
    public async Task An_unauthorized_caller_learns_nothing_about_catalog_rows()
    {
        _contextProvider.GetContext().Returns(AgentAdministrationContext.Anonymous);
        EventStoreProviderCatalogOperations operations = Operations();

        ProviderCatalogInspectionResult result = (await operations.ListEntriesAsync(includeDisabled: true))
            .Value
            .ShouldNotBeNull();

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entries.ShouldBeEmpty();
        JsonSerializer.Serialize(result).ShouldNotContain("openai");
        JsonSerializer.Serialize(result).ShouldNotContain("cfg-openai-gpt4o");
    }

    [Fact]
    public async Task An_unauthorized_reader_does_not_address_the_store()
    {
        _contextProvider.GetContext().Returns(AgentAdministrationContext.Anonymous);
        int getsBefore = _store.GetCount;

        ProviderCatalogEntryReadResult result = await Reader().GetEntryAsync(
            TenantId,
            "openai",
            "gpt-4o",
            CancellationToken.None);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entry.ShouldBeNull();
        _store.GetCount.ShouldBe(getsBefore);
        JsonSerializer.Serialize(result).ShouldNotContain("openai");
    }

    private ProjectedProviderCatalogReader Reader()
        => new(
            _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
            _contextProvider);

    private EventStoreProviderCatalogOperations Operations()
        => new(
            _contextProvider,
            new ProviderCatalogAdministrationOrchestrator(Substitute.For<IAgentCommandDispatcher>()),
            _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory());

    private void SeedCatalog()
        => _store.Seed(
            StoreName,
            ProviderCatalogReadModelAddresses.Detail(TenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = TenantId,
                TenantId = TenantId,
                Entries =
                [
                    new ProviderCatalogEntryView(
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
                        1,
                        new ProviderModelPricing("USD", 0.002m, 0.008m, 1)),
                ],
                LastSequenceNumber = 1,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = "1",
            });
}
