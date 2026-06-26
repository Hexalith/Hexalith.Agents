using System;
using System.Linq;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 2.6 gateway-result wrappers (AC1, AC2, AC3; AD-12, AD-14). The four wrappers
/// (<see cref="AgentInteractionInspectionResult"/>/<see cref="AgentInteractionInspectionStatus"/>,
/// <see cref="AgentCallRequestResult"/>/<see cref="AgentCallRequestStatus"/>) cross the future live
/// <c>Hexalith.Agents.Client</c> → BFF/API boundary, so each must survive System.Text.Json; the status enums must
/// serialize by name and fail safe to <c>Unknown</c>; the data (view/reference) is non-null only off the success path;
/// and the wrappers carry only safe ids/status — never a prompt, generated content, stream name, provider detail, or
/// secret. The assembly-wide <see cref="ContractsSecretNonDisclosureTests"/> additionally auto-covers the new types.
/// </summary>
public sealed class AgentCallGatewayWrappersContractsTests
{
    private static AgentInteractionStatusView View { get; } = new(
        "interaction-001",
        AgentInteractionStatus.Generated,
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

    // ===== Round-trips =====

    [Fact]
    public void Inspection_result_round_trips_on_success_with_its_view()
    {
        AgentInteractionInspectionResult result = AgentInteractionInspectionResult.Success(View);
        AgentInteractionInspectionResult roundTripped = RoundTrip(result);
        roundTripped.ShouldBe(result);
        roundTripped.View.ShouldBe(View);
    }

    [Fact]
    public void Inspection_result_round_trips_off_success_with_a_null_view()
    {
        RoundTrip(AgentInteractionInspectionResult.NotAuthorized()).ShouldBe(AgentInteractionInspectionResult.NotAuthorized());
        RoundTrip(AgentInteractionInspectionResult.NotFound()).ShouldBe(AgentInteractionInspectionResult.NotFound());
    }

    [Fact]
    public void Request_result_round_trips_on_accept_with_its_reference()
    {
        AgentInteractionReference reference = new("interaction-001", AgentInteractionStatus.Requested);
        AgentCallRequestResult result = AgentCallRequestResult.Accepted(reference);
        AgentCallRequestResult roundTripped = RoundTrip(result);
        roundTripped.ShouldBe(result);
        roundTripped.Reference.ShouldBe(reference);
    }

    [Fact]
    public void Request_result_round_trips_off_accept_with_a_null_reference()
    {
        RoundTrip(AgentCallRequestResult.NotAuthorized()).ShouldBe(AgentCallRequestResult.NotAuthorized());
        RoundTrip(AgentCallRequestResult.Rejected()).ShouldBe(AgentCallRequestResult.Rejected());
    }

    // ===== Factories: data is present only on the success path (AD-12) =====

    [Fact]
    public void Inspection_factories_carry_a_view_only_on_success()
    {
        AgentInteractionInspectionResult.Success(View).Status.ShouldBe(AgentInteractionInspectionStatus.Success);
        AgentInteractionInspectionResult.Success(View).View.ShouldBe(View);
        AgentInteractionInspectionResult.NotAuthorized().View.ShouldBeNull();
        AgentInteractionInspectionResult.NotFound().View.ShouldBeNull();
    }

    [Fact]
    public void Request_factories_carry_a_reference_only_on_accept()
    {
        AgentInteractionReference reference = new("interaction-001", AgentInteractionStatus.Requested);
        AgentCallRequestResult.Accepted(reference).Status.ShouldBe(AgentCallRequestStatus.Accepted);
        AgentCallRequestResult.Accepted(reference).Reference.ShouldBe(reference);
        AgentCallRequestResult.NotAuthorized().Reference.ShouldBeNull();
        AgentCallRequestResult.Rejected().Reference.ShouldBeNull();
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Inspection_status_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionInspectionStatus.NotAuthorized).ShouldBe("\"NotAuthorized\"");
        default(AgentInteractionInspectionStatus).ShouldBe(AgentInteractionInspectionStatus.Unknown);
        JsonSerializer.Deserialize<AgentInteractionInspectionStatus>("\"Unknown\"").ShouldBe(AgentInteractionInspectionStatus.Unknown);
    }

    [Fact]
    public void Request_status_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentCallRequestStatus.Accepted).ShouldBe("\"Accepted\"");
        default(AgentCallRequestStatus).ShouldBe(AgentCallRequestStatus.Unknown);
        JsonSerializer.Deserialize<AgentCallRequestStatus>("\"Unknown\"").ShouldBe(AgentCallRequestStatus.Unknown);
    }

    // ===== No content/prompt leak (AC2, AC3; AD-14) =====

    [Fact]
    public void Wrappers_expose_no_prompt_or_content_member()
    {
        HasNoSensitiveMember(typeof(AgentInteractionInspectionResult));
        HasNoSensitiveMember(typeof(AgentCallRequestResult));
    }

    [Fact]
    public void Serialized_wrappers_carry_only_safe_references_and_never_content()
    {
        // The wrappers have no place to hold a prompt or generated content; a poison token never inserted into any safe
        // field must never appear in the serialized form (AD-14).
        const string poison = "secret-prompt-or-generated-content";

        string inspection = JsonSerializer.Serialize(AgentInteractionInspectionResult.Success(View));
        inspection.ShouldContain("interaction-001"); // safe id is carried
        inspection.ShouldNotContain(poison);

        string request = JsonSerializer.Serialize(
            AgentCallRequestResult.Accepted(new AgentInteractionReference("interaction-001", AgentInteractionStatus.Requested)));
        request.ShouldContain("interaction-001");
        request.ShouldNotContain(poison);
    }

    [Fact]
    public void Wrappers_carry_no_hexalith_domain_attribute()
    {
        // Plain contracts, like AgentInspectionResult — no event-payload marker or domain attribute. Scoped to the
        // Hexalith namespace so compiler-generated Nullable* attributes (emitted for the nullable members) are ignored
        // (Story 2.1 learning).
        AssertNoHexalithAttribute(typeof(AgentInteractionInspectionResult));
        AssertNoHexalithAttribute(typeof(AgentCallRequestResult));
    }

    private static void AssertNoHexalithAttribute(Type type)
        => type.GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));

    private static void HasNoSensitiveMember(Type type)
    {
        string[] forbidden = ["Prompt", "Content", "Instructions", "Secret", "Message"];
        foreach (var property in type.GetProperties())
        {
            forbidden.ShouldNotContain(
                token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase),
                $"{type.Name}.{property.Name} must not expose sensitive content (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
