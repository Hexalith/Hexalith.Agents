using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2/AC3 — the pure readiness mapping never collapses active lifecycle with callability, and binds each state to
/// the correct Fluent semantic role. Success is used ONLY for a proven-callable Agent / enabled+configured
/// provider.
/// </summary>
public sealed class AgentReadinessMappingTests
{
    public static IEnumerable<object[]> ReadinessCases()
    {
        yield return [AgentUiTestData.Status(AgentLifecycleStatus.Active), AgentReadinessState.Callable, BadgeColor.Success];
        yield return [AgentUiTestData.Status(AgentLifecycleStatus.Disabled), AgentReadinessState.Disabled, BadgeColor.Severe];
        yield return [AgentUiTestData.Status(blockers: [AgentActivationBlocker.MissingPartyIdentity]), AgentReadinessState.MissingPartyIdentity, BadgeColor.Severe];
        yield return [AgentUiTestData.Status(blockers: [AgentActivationBlocker.MissingProviderSelection]), AgentReadinessState.ProviderUnavailable, BadgeColor.Severe];
        yield return [AgentUiTestData.Status(blockers: [AgentActivationBlocker.ProviderUnavailable]), AgentReadinessState.ProviderUnavailable, BadgeColor.Severe];
        yield return [AgentUiTestData.Status(blockers: [AgentActivationBlocker.MissingInstructions]), AgentReadinessState.InvalidConfiguration, BadgeColor.Severe];
        yield return [AgentUiTestData.Status(blockers: [AgentActivationBlocker.MissingContentSafetyPolicy]), AgentReadinessState.InvalidConfiguration, BadgeColor.Severe];
        yield return [AgentUiTestData.Status(blockers: [AgentActivationBlocker.ApproverPolicyUnresolvable]), AgentReadinessState.Unresolved, BadgeColor.Important];
    }

    [Theory]
    [MemberData(nameof(ReadinessCases))]
    public void MapState_and_ColorFor_bind_each_view_to_its_canonical_state_and_role(
        AgentStatusView view, AgentReadinessState expectedState, BadgeColor expectedColor)
    {
        AgentReadiness.MapState(view).ShouldBe(expectedState);
        AgentReadiness.ColorFor(AgentReadiness.MapState(view)).ShouldBe(expectedColor);
    }

    [Fact]
    public void Active_agent_with_blockers_is_never_callable_and_never_success()
    {
        AgentStatusView view = AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            blockers: [AgentActivationBlocker.MissingContentSafetyPolicy]);

        AgentReadiness.IsCallable(view).ShouldBeFalse();
        AgentReadiness.MapState(view).ShouldNotBe(AgentReadinessState.Callable);
        AgentReadiness.ColorFor(AgentReadiness.MapState(view)).ShouldNotBe(BadgeColor.Success);
    }

    [Fact]
    public void Callable_requires_active_lifecycle_and_no_blockers()
    {
        AgentReadiness.IsCallable(AgentUiTestData.Status(AgentLifecycleStatus.Active)).ShouldBeTrue();
        AgentReadiness.IsCallable(AgentUiTestData.Status(AgentLifecycleStatus.Draft)).ShouldBeFalse();
    }

    public static IEnumerable<object[]> ProviderCases()
    {
        yield return [AgentUiTestData.Entry(status: ProviderModelStatus.Enabled, configurationState: ProviderConfigurationState.Configured), ProviderReadinessState.Enabled, BadgeColor.Success];
        yield return [AgentUiTestData.Entry(status: ProviderModelStatus.Enabled, configurationState: ProviderConfigurationState.NotConfigured), ProviderReadinessState.NotConfigured, BadgeColor.Severe];
        yield return [AgentUiTestData.Entry(status: ProviderModelStatus.Disabled, configurationState: ProviderConfigurationState.NotConfigured), ProviderReadinessState.Disabled, BadgeColor.Severe];
        yield return [AgentUiTestData.Entry(status: ProviderModelStatus.Disabled, configurationState: ProviderConfigurationState.Configured), ProviderReadinessState.HistoricalSelection, BadgeColor.Subtle];
        yield return [AgentUiTestData.Entry(status: ProviderModelStatus.Degraded, configurationState: ProviderConfigurationState.Configured), ProviderReadinessState.Degraded, BadgeColor.Warning];
        yield return [AgentUiTestData.Entry(status: ProviderModelStatus.Failed, configurationState: ProviderConfigurationState.Configured), ProviderReadinessState.Failed, BadgeColor.Danger];
    }

    [Theory]
    [MemberData(nameof(ProviderCases))]
    public void MapProviderState_and_ColorFor_bind_each_entry_to_its_canonical_state_and_role(
        ProviderCatalogEntryView entry, ProviderReadinessState expectedState, BadgeColor expectedColor)
    {
        AgentReadiness.MapProviderState(entry).ShouldBe(expectedState);
        AgentReadiness.ColorFor(AgentReadiness.MapProviderState(entry)).ShouldBe(expectedColor);
    }
}
