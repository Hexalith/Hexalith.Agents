using System.Linq;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 2.1 Agent Call request surface (AC1, AC2, AC4; AD-13, AD-14). Durable event
/// sourcing replays every new event/rejection, so each must survive System.Text.Json; the status/validation enums
/// must serialize by name and fail safe to their <c>Unknown</c> sentinel; and the sensitive <c>Prompt</c> must be
/// absent from the status view, the status reference, and every rejection (AD-14). The assembly-wide
/// <see cref="ContractsSecretNonDisclosureTests"/> additionally auto-covers the new types for secret/provider leaks.
/// </summary>
public sealed class AgentInteractionContractsTests
{
    private static AgentInteractionSnapshot Snapshot { get; } = new(
        ConfigurationVersion: 3,
        InstructionsVersion: 2,
        AgentResponseMode.Confirmation,
        ApproverPolicyVersion: 1,
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 4,
        ContentSafetyPolicyVersion: 2,
        AgentInteractionSnapshot.DefaultContextPolicyReference);

    private static RequestAgentInteraction Request { get; } = new(
        "hexa",
        "conversation-001",
        "party-001",
        "Summarize the latest decisions.",
        "idem-001",
        Snapshot,
        "client-corr-001");

    private static InteractionRequested Requested { get; } = new(
        "interaction-001",
        "hexa",
        "party-001",
        "conversation-001",
        Snapshot,
        "Summarize the latest decisions.",
        "idem-001");

    // ===== Marker interfaces =====

    [Fact]
    public void Interaction_requested_event_implements_IEventPayload()
        => typeof(IEventPayload).IsAssignableFrom(typeof(InteractionRequested)).ShouldBeTrue();

    [Fact]
    public void Request_rejections_implement_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(InvalidAgentInteractionRequestRejection)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionAlreadyRequestedRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(InvalidAgentInteractionRequestRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionAlreadyRequestedRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Request_command_has_no_marker_interface_or_domain_attribute()
    {
        // Mirrors LinkAgentPartyIdentity: a plain caller command with no event-payload marker and no domain/EventStore
        // attribute (compiler-generated nullable attributes are ignored).
        typeof(IEventPayload).IsAssignableFrom(typeof(RequestAgentInteraction)).ShouldBeFalse();
        typeof(RequestAgentInteraction)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", System.StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Request_command_round_trips_with_its_snapshot()
        => RoundTrip(Request).ShouldBe(Request);

    [Fact]
    public void Request_command_round_trips_with_a_null_snapshot_and_correlation()
    {
        RequestAgentInteraction request = Request with { Snapshot = null, ClientCorrelationId = null };
        RoundTrip(request).ShouldBe(request);
    }

    [Fact]
    public void Interaction_requested_event_round_trips_with_its_nested_snapshot()
    {
        InteractionRequested roundTripped = RoundTrip(Requested);
        roundTripped.ShouldBe(Requested);
        roundTripped.Snapshot.ShouldBe(Snapshot);
        roundTripped.Prompt.ShouldBe(Requested.Prompt);
    }

    [Fact]
    public void Invalid_request_rejection_round_trips_with_the_enum_status()
    {
        var rejection = new InvalidAgentInteractionRequestRejection("interaction-001", AgentInteractionRequestValidationStatus.MissingPrompt);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    [Fact]
    public void Already_requested_rejection_round_trips()
    {
        var rejection = new AgentInteractionAlreadyRequestedRejection("interaction-001");
        RoundTrip(rejection).ShouldBe(rejection);
    }

    [Fact]
    public void Status_query_round_trips()
        => RoundTrip(new GetAgentInteractionStatusQuery()).ShouldBe(new GetAgentInteractionStatusQuery());

    [Fact]
    public void Status_reference_round_trips()
    {
        var reference = new AgentInteractionReference("interaction-001", AgentInteractionStatus.Requested);
        RoundTrip(reference).ShouldBe(reference);
    }

    [Fact]
    public void Status_view_round_trips_without_any_prompt_field()
    {
        var view = new AgentInteractionStatusView(
            "interaction-001",
            AgentInteractionStatus.Requested,
            "hexa",
            "party-001",
            "conversation-001",
            AgentResponseMode.Automatic,
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            ApproverPolicyVersion: 1,
            ProviderId: "openai",
            ModelId: "gpt-4o",
            ProviderCapabilityVersion: 4,
            ContentSafetyPolicyVersion: 2);

        RoundTrip(view).ShouldBe(view);
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Interaction_status_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.Requested).ShouldBe("\"Requested\"");
        default(AgentInteractionStatus).ShouldBe(AgentInteractionStatus.Unknown);
        JsonSerializer.Deserialize<AgentInteractionStatus>("\"Unknown\"").ShouldBe(AgentInteractionStatus.Unknown);
    }

    [Fact]
    public void Request_validation_status_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionRequestValidationStatus.MissingAgentSnapshot).ShouldBe("\"MissingAgentSnapshot\"");
        default(AgentInteractionRequestValidationStatus).ShouldBe(AgentInteractionRequestValidationStatus.Unknown);
        JsonSerializer.Deserialize<AgentInteractionRequestValidationStatus>("\"Unknown\"").ShouldBe(AgentInteractionRequestValidationStatus.Unknown);
    }

    // ===== Sensitive prompt non-disclosure (AC4; AD-14) =====

    [Fact]
    public void Prompt_is_absent_from_the_status_view_reference_and_every_rejection()
    {
        HasNoPromptMember(typeof(AgentInteractionStatusView));
        HasNoPromptMember(typeof(AgentInteractionReference));
        HasNoPromptMember(typeof(InvalidAgentInteractionRequestRejection));
        HasNoPromptMember(typeof(AgentInteractionAlreadyRequestedRejection));
    }

    [Fact]
    public void Prompt_is_present_only_on_the_durable_event_and_command()
    {
        // The prompt's only sanctioned homes are the durable success event and the caller command (AD-14).
        typeof(InteractionRequested).GetProperties().ShouldContain(p => p.Name == "Prompt");
        typeof(RequestAgentInteraction).GetProperties().ShouldContain(p => p.Name == "Prompt");
    }

    private static void HasNoPromptMember(System.Type type)
        => type.GetProperties().ShouldNotContain(
            p => p.Name.Contains("Prompt", System.StringComparison.OrdinalIgnoreCase),
            $"{type.Name} must not expose the sensitive prompt (AD-14).");

    // ===== No wall-clock field (AD-3) =====

    [Fact]
    public void Request_surface_carries_no_wall_clock_field()
    {
        // AD-3 / module no-wall-clock rule: request time is the EventStore event-metadata timestamp, server-stamped
        // at persist. No member may re-introduce a request timestamp onto the durable event or the caller command.
        HasNoWallClockMember(typeof(InteractionRequested));
        HasNoWallClockMember(typeof(RequestAgentInteraction));
    }

    private static void HasNoWallClockMember(System.Type type)
    {
        string[] forbidden = ["Timestamp", "DateTime", "CreatedAt", "RequestedAt", "OccurredAt", "OccurredOn", "Utc", "WallClock"];
        foreach (var property in type.GetProperties())
        {
            forbidden.ShouldNotContain(
                token => property.Name.Contains(token, System.StringComparison.OrdinalIgnoreCase),
                $"{type.Name}.{property.Name} re-introduces a wall-clock field — request time is event metadata (AD-3).");
        }
    }

    // ===== V1 default context-policy reference is pinned (FR-9) =====

    [Fact]
    public void Default_context_policy_reference_is_the_pinned_v1_value()
        // Pinned so Story 2.3 binding a concrete policy is a deliberate, reviewed change — not an accidental drift.
        => AgentInteractionSnapshot.DefaultContextPolicyReference.ShouldBe("full-conversation-v1");

    // ===== Standalone snapshot round-trip =====

    [Fact]
    public void Snapshot_round_trips_and_serializes_its_response_mode_by_name()
    {
        RoundTrip(Snapshot).ShouldBe(Snapshot);
        string json = JsonSerializer.Serialize(Snapshot);
        json.ShouldContain("\"Confirmation\""); // the nested enum is serialized by name, not by ordinal
        json.ShouldNotContain("\"ResponseMode\":1");
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
