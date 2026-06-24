using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Components.Pages;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC3 — the Provider catalog grid surfaces capability/readiness/configured state but never a secret, the
/// configuration reference value, a raw payload, or an SDK detail. It also routes the fail-closed surface states.
/// </summary>
public sealed class ProviderCatalogTests : AgentsTestContext
{
    private const string SentinelReference = "SECRET-REF-DO-NOT-RENDER-9f3a";

    [Fact]
    public void Grid_renders_entries_with_status_badges_and_configured_state()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success(
            [
                AgentUiTestData.Entry("openai", "gpt-x", ProviderModelStatus.Enabled, ProviderConfigurationState.Configured, SentinelReference),
                AgentUiTestData.Entry("anthropic", "claude-x", ProviderModelStatus.Disabled, ProviderConfigurationState.NotConfigured),
            ])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-grid']");
            cut.Markup.ShouldContain("openai");
            cut.Markup.ShouldContain("anthropic");
            cut.FindComponents<Hexalith.Agents.UI.Components.Shared.ProviderStatusBadge>().Count.ShouldBe(2);
            cut.Markup.ShouldContain("Agents.ProviderCatalog.ConfigurationState.Configured");
            cut.Markup.ShouldContain("Agents.ProviderCatalog.ConfigurationState.NotConfigured");
        });
    }

    [Fact]
    public void Grid_never_renders_the_configuration_reference_value_or_any_secret()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success(
            [
                AgentUiTestData.Entry("openai", "gpt-x", ProviderModelStatus.Enabled, ProviderConfigurationState.Configured, SentinelReference),
            ])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-grid']");
            cut.Markup.ShouldNotContain(SentinelReference);
            cut.VisibleText().ShouldNotContain(SentinelReference);
        });
    }

    [Fact]
    public void Not_authorized_result_renders_the_permission_denied_surface()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.NotAuthorized()));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-state']").GetAttribute("data-testid").ShouldBe("agents-provider-catalog-state");
            cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
        });
    }

    [Fact]
    public void Empty_success_result_renders_the_empty_surface_without_leaking_records()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.Empty.Title"));
    }

    [Fact]
    public void Grid_renders_only_safe_capability_labels_and_token_limits()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success(
            [
                AgentUiTestData.Entry(
                    "openai",
                    "gpt-x",
                    capabilities: ProviderModelCapabilityFlags.Vision | ProviderModelCapabilityFlags.ToolCalling),
            ])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-grid']");
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Capability.TextGeneration");
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Capability.Vision");
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Capability.ToolCalling");
            // Streaming was not set, so its whole-string label must not appear.
            cut.Markup.ShouldNotContain("Agents.ProviderCatalog.Capability.Streaming");
            // Safe normalized token limits are surfaced as localized whole strings (AD-10 floor).
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Tokens.Value");
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Timeout.Value");
        });
    }

    [Fact]
    public void Entry_without_any_capability_renders_the_none_label()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success(
            [
                AgentUiTestData.Entry(
                    "openai",
                    "gpt-x",
                    capabilities: ProviderModelCapabilityFlags.None,
                    supportsTextGeneration: false),
            ])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProviderCatalog.Capability.None"));
    }

    [Fact]
    public void List_entries_is_requested_with_include_disabled_so_inspection_is_complete()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([AgentUiTestData.Entry()])));

        _ = RenderPage<ProviderCatalog>();

        CatalogGateway.Received().ListEntriesAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Selectable_only_filter_yields_a_filtered_empty_surface_that_can_be_reset()
    {
        // Every entry is non-selectable: turning on the selectable-only filter must produce the distinct
        // filtered-empty state (offering a reset), never the no-records empty state (UX-DR30, AC6).
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success(
            [
                AgentUiTestData.Entry("openai", "gpt-x", isSelectable: false),
                AgentUiTestData.Entry("anthropic", "claude-x", isSelectable: false),
            ])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-grid']"));

        cut.Find("[data-testid='agents-provider-catalog-selectable-filter']").Change(true);

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-state']");
            cut.Markup.ShouldContain("Agents.Surface.FilteredEmpty.Title");
            cut.FindAll("[data-testid='agents-provider-catalog-grid']").ShouldBeEmpty();
        });

        cut.Find("[data-testid='agents-provider-catalog-state-reset']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-grid']"));
    }
}
