using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 1.4 Party-identity surface (AC1, AC2, AC4; AD-7, AD-14). Durable event sourcing
/// replays every new event/rejection, so each must survive System.Text.Json; the <see cref="PartyLinkValidationStatus"/>
/// verdict and the new <see cref="AgentActivationBlocker.MissingPartyIdentity"/> blocker must serialize by name and
/// fail safe to their <c>Unknown</c> sentinels; and the party events/rejections must expose only id-shaped fields —
/// never a Party display name, contact, or any personal-data member (a <c>PartyId</c> is a reference, not PII).
/// </summary>
public sealed class AgentPartyIdentityContractsTests
{
    // ===== Success events round-trip =====

    [Fact]
    public void Party_identity_linked_event_round_trips_through_system_text_json()
    {
        var linked = new AgentPartyIdentityLinked("hexa", "party-001", ConfigurationVersion: 2);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(linked);

        JsonSerializer.Deserialize<AgentPartyIdentityLinked>(bytes).ShouldBe(linked);
    }

    [Fact]
    public void Party_identity_replaced_event_round_trips_with_previous_id()
    {
        var replaced = new AgentPartyIdentityReplaced("hexa", "party-001", "party-002", ConfigurationVersion: 3);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(replaced);

        JsonSerializer.Deserialize<AgentPartyIdentityReplaced>(bytes).ShouldBe(replaced);
    }

    [Fact]
    public void Party_identity_replaced_event_round_trips_with_a_null_previous_id()
    {
        var replaced = new AgentPartyIdentityReplaced("hexa", PreviousPartyId: null, "party-002", ConfigurationVersion: 2);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(replaced);

        JsonSerializer.Deserialize<AgentPartyIdentityReplaced>(bytes).ShouldBe(replaced);
    }

    // ===== Rejection events round-trip =====

    [Fact]
    public void Party_identity_link_rejected_round_trips_with_the_enum_verdict()
    {
        var rejection = new AgentPartyIdentityLinkRejected("hexa", PartyLinkValidationStatus.Unavailable);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        AgentPartyIdentityLinkRejected? roundTripped = JsonSerializer.Deserialize<AgentPartyIdentityLinkRejected>(bytes);
        roundTripped.ShouldBe(rejection);
        roundTripped!.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
    }

    [Fact]
    public void Party_identity_already_linked_rejection_round_trips_through_system_text_json()
    {
        var rejection = new AgentPartyIdentityAlreadyLinkedRejection("hexa", "party-999");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<AgentPartyIdentityAlreadyLinkedRejection>(bytes).ShouldBe(rejection);
    }

    // ===== Verdict + blocker enums serialize by name and fail safe =====

    [Fact]
    public void Party_link_validation_status_serializes_by_name()
    {
        JsonSerializer.Serialize(PartyLinkValidationStatus.Valid).ShouldBe("\"Valid\"");
        JsonSerializer.Deserialize<PartyLinkValidationStatus>("\"Unauthorized\"").ShouldBe(PartyLinkValidationStatus.Unauthorized);
    }

    [Fact]
    public void Party_link_validation_status_is_unknown_by_default_and_for_unrecognized_input()
    {
        // AC2 fail-safe: an absent/unrecognized verdict must never deserialize to Valid.
        default(PartyLinkValidationStatus).ShouldBe(PartyLinkValidationStatus.Unknown);
        JsonSerializer.Deserialize<PartyLinkValidationStatus>("\"Unknown\"").ShouldBe(PartyLinkValidationStatus.Unknown);
    }

    [Fact]
    public void Missing_party_identity_blocker_serializes_by_name()
    {
        string json = JsonSerializer.Serialize(AgentActivationBlocker.MissingPartyIdentity);

        json.ShouldBe("\"MissingPartyIdentity\"");
        JsonSerializer.Deserialize<AgentActivationBlocker>(json).ShouldBe(AgentActivationBlocker.MissingPartyIdentity);
    }

    [Fact]
    public void Existing_activation_blocker_ordinals_are_preserved_and_party_identity_is_appended()
    {
        // Additive extensibility (AD-2): the new value is appended; existing ordinals must not shift.
        ((int)AgentActivationBlocker.Unknown).ShouldBe(0);
        ((int)AgentActivationBlocker.MissingDisplayName).ShouldBe(1);
        ((int)AgentActivationBlocker.MissingInstructions).ShouldBe(2);
        ((int)AgentActivationBlocker.InvalidInstructions).ShouldBe(3);
        ((int)AgentActivationBlocker.MissingPartyIdentity).ShouldBe(4);
    }

    // ===== Party events/rejections expose only safe, id-shaped fields (AC1; AD-7, AD-14) =====

    [Fact]
    public void Party_events_and_rejections_expose_no_personal_data_member()
    {
        Type[] partyTypes =
        [
            typeof(AgentPartyIdentityLinked),
            typeof(AgentPartyIdentityReplaced),
            typeof(AgentPartyIdentityLinkRejected),
            typeof(AgentPartyIdentityAlreadyLinkedRejection),
        ];

        string[] forbiddenPiiTokens =
        [
            "DisplayName", "SortName", "Person", "Contact", "Email", "Phone", "Address", "Personal", "Organization",
        ];

        foreach (Type type in partyTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                forbiddenPiiTokens.ShouldNotContain(
                    token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase),
                    $"{type.Name}.{property.Name} must not expose a Party PII member (AC1; AD-7).");

                // Only id-shaped strings, the int version, and the safe verdict enum may cross the public boundary.
                bool safeType = property.PropertyType == typeof(string)
                    || property.PropertyType == typeof(int)
                    || property.PropertyType == typeof(PartyLinkValidationStatus);
                safeType.ShouldBeTrue(
                    $"{type.Name}.{property.Name} exposes non-id-shaped type '{property.PropertyType.Name}' on the party surface.");
            }
        }
    }

    [Fact]
    public void Party_id_fields_are_present_and_id_shaped()
    {
        typeof(AgentPartyIdentityLinked).GetProperties().ShouldContain(p => p.Name == "PartyId" && p.PropertyType == typeof(string));
        typeof(AgentPartyIdentityReplaced).GetProperties().ShouldContain(p => p.Name == "PreviousPartyId" && p.PropertyType == typeof(string));
        typeof(AgentPartyIdentityReplaced).GetProperties().ShouldContain(p => p.Name == "PartyId" && p.PropertyType == typeof(string));
        typeof(AgentPartyIdentityAlreadyLinkedRejection).GetProperties().ShouldContain(p => p.Name == "AttemptedPartyId" && p.PropertyType == typeof(string));
    }

    // ===== Extended status view carries party presence, never the id or PII =====

    [Fact]
    public void Status_view_exposes_party_presence_not_the_party_id()
    {
        PropertyInfo[] properties = typeof(AgentStatusView).GetProperties();

        properties.ShouldContain(p => p.Name == "HasPartyIdentity" && p.PropertyType == typeof(bool));
        properties.ShouldNotContain(p => p.Name == "PartyId"); // presence only — the id is not on the readiness view
    }
}
