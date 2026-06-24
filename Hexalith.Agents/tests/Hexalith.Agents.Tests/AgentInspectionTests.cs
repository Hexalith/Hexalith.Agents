using System.Text.Json;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Tests for the pure inspection read path (<see cref="AgentInspection"/>): authorized status read showing
/// lifecycle and blockers without the Agent Instructions text (AC1, AC3; AD-14), disabled-state visibility (AC3),
/// and fail-closed structured results for unauthorized / missing reads (AC4).
/// </summary>
public sealed class AgentInspectionTests
{
    [Fact]
    public void GetStatus_authorized_active_agent_returns_view_without_instructions_text()
    {
        // A realistic active agent is linked to a Party (1.4 AC4) and has a selected Provider/model (1.5 AC1)
        // before it can activate.
        AgentState state = StateWithSelectedProvider(ValidCreate());
        state.Apply(new AgentActivated(AgentId));

        AgentInspectionResult result = AgentInspection.GetStatus(state, isAgentsAdmin: true);

        result.Status.ShouldBe(AgentInspectionStatus.Success);
        AgentStatusView view = result.Agent.ShouldNotBeNull();
        view.AgentId.ShouldBe(AgentId);
        view.TenantId.ShouldBe(TenantId);
        view.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
        view.HasInstructions.ShouldBeTrue();
        view.InstructionsValid.ShouldBeTrue();
        view.InstructionsVersion.ShouldBe(1);
        view.HasPartyIdentity.ShouldBeTrue(); // AC4: presence surfaced without the Party id or any PII
        view.HasProviderSelection.ShouldBeTrue(); // 1.5 AC1: selection presence surfaced...
        view.SelectedProviderId.ShouldBe(SelectedProviderId); // ...with the safe id (a reference, never a secret)
        view.SelectedModelId.ShouldBe(SelectedModelId);
        view.ActivationBlockers.ShouldBeEmpty();

        // AD-14: the raw instructions text must never appear anywhere on the serialized status view.
        string json = JsonSerializer.Serialize(view);
        json.ShouldNotContain(ValidInstructions);
    }

    [Fact]
    public void GetStatus_draft_with_missing_fields_reports_blockers()
    {
        AgentState state = StateWith(ValidCreate(displayName: "", instructions: ""));

        AgentInspectionResult result = AgentInspection.GetStatus(state, isAgentsAdmin: true);

        result.Status.ShouldBe(AgentInspectionStatus.Success);
        AgentStatusView view = result.Agent.ShouldNotBeNull();
        view.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
        view.HasInstructions.ShouldBeFalse();
        view.ActivationBlockers.ShouldContain(AgentActivationBlocker.MissingDisplayName);
        view.ActivationBlockers.ShouldContain(AgentActivationBlocker.MissingInstructions);
    }

    [Fact]
    public void GetStatus_disabled_agent_is_visible_through_public_status_path()
    {
        AgentState state = DisabledStateWith(ValidCreate());

        AgentInspectionResult result = AgentInspection.GetStatus(state, isAgentsAdmin: true);

        result.Status.ShouldBe(AgentInspectionStatus.Success);
        result.Agent.ShouldNotBeNull().Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled); // AC3
    }

    [Fact]
    public void GetStatus_unauthorized_returns_not_authorized_with_no_data()
    {
        AgentState state = ActiveStateWith(ValidCreate());

        AgentInspectionResult result = AgentInspection.GetStatus(state, isAgentsAdmin: false);

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Agent.ShouldBeNull(); // AC4: no fingerprinting of whether hexa exists
    }

    [Fact]
    public void GetStatus_missing_agent_returns_structured_not_found()
    {
        AgentInspectionResult result = AgentInspection.GetStatus(state: null, isAgentsAdmin: true);

        result.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
        result.Agent.ShouldBeNull();
    }

    [Fact]
    public void GetStatus_uncreated_state_returns_structured_not_found()
    {
        // A rehydrated-but-never-created state (only a pre-create rejection in the stream) must read as not found.
        var state = new AgentState();

        AgentInspectionResult result = AgentInspection.GetStatus(state, isAgentsAdmin: true);

        result.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
        result.Agent.ShouldBeNull();
    }
}
