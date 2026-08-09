using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;

using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Deterministic fold from an Agent event slice onto the persisted setup read model (Story 5.2). The fold is pure:
/// it reads no clock and no dependency, and it skips events at or below the stored checkpoint — so a slice
/// delivered once, twice, or as a full replay yields the same end state.
/// </summary>
public static class AgentSetupProjectionFold
{
    /// <summary>Reason reported when a delivery skips aggregate sequence numbers.</summary>
    public const string DeliverySequenceGapReason = "delivery-sequence-gap";

    /// <summary>Reason reported when a delivery carries an event this domain cannot resolve.</summary>
    public const string UnresolvedEventReason = "unresolved-event-type";

    // Instruction text is never persisted (AD-14), so replay feeds the pure policy a length-only stand-in that
    // reproduces the recorded presence/validity verdict. It is folded state, never surfaced.
    private const string _validInstructionsStandIn = "xxxxxxxxxxxxxxxx";
    private const string _invalidInstructionsStandIn = "x";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly AgentContentSafetyPolicy _contentSafetyStandIn = new(
        [],
        [],
        [],
        ContentSafetyFailureHandling.BlockAndAudit,
        ContentSafetyAuditTreatment.MetadataOnly);

    /// <summary>
    /// Returns the reason a delivery must be retried rather than folded, or <see langword="null"/> when the slice is
    /// safe to apply. A gap or an unknown event type is a transport/deployment fault: folding past it would bake a
    /// wrong end state into the read model, so the delivery fails and is retried instead.
    /// </summary>
    /// <param name="events">The delivered event slice.</param>
    /// <param name="checkpoint">The highest sequence number already folded (0 when none).</param>
    /// <returns>The retry reason, or <see langword="null"/>.</returns>
    public static string? GetDeliveryFailureReason(IReadOnlyCollection<ProjectionEventDto> events, long checkpoint)
    {
        ArgumentNullException.ThrowIfNull(events);

        ProjectionEventDto[] pending = [.. events.Where(item => item.SequenceNumber > checkpoint).OrderBy(item => item.SequenceNumber)];
        if (pending.Length == 0)
        {
            return null;
        }

        if (pending[0].SequenceNumber != checkpoint + 1)
        {
            return DeliverySequenceGapReason;
        }

        for (int index = 1; index < pending.Length; index++)
        {
            if (pending[index].SequenceNumber != pending[index - 1].SequenceNumber + 1)
            {
                return DeliverySequenceGapReason;
            }
        }

        return Array.Exists(pending, item => AgentEventTypeResolver.Resolve(item.EventTypeName) is null)
            ? UnresolvedEventReason
            : null;
    }

    /// <summary>Folds the delivered slice onto the current read model, returning the next value.</summary>
    /// <param name="request">The projection request carrying the tenant, Agent id, and event slice.</param>
    /// <param name="current">The currently persisted read model, or <see langword="null"/> when absent.</param>
    /// <returns>The next read model.</returns>
    public static AgentSetupReadModel Fold(ProjectionRequest request, AgentSetupReadModel? current)
    {
        ArgumentNullException.ThrowIfNull(request);

        long checkpoint = current?.LastSequenceNumber ?? 0;
        AgentState state = ToState(current, request.TenantId, request.AggregateId);
        DateTimeOffset? projectedAt = current?.ProjectedAt;
        long lastSequence = checkpoint;

        foreach (ProjectionEventDto @event in request.Events
                     .Where(item => item.SequenceNumber > checkpoint)
                     .OrderBy(item => item.SequenceNumber))
        {
            if (AgentEventTypeResolver.Resolve(@event.EventTypeName) is not { } eventType)
            {
                // Callers check GetDeliveryFailureReason first. Reaching here means folding cannot be completed
                // faithfully, so stop rather than advance the checkpoint past an event that was never applied.
                break;
            }

            if (Deserialize(@event.Payload, eventType) is { } payload)
            {
                Apply(state, payload);
            }

            lastSequence = Math.Max(lastSequence, @event.SequenceNumber);
            DateTimeOffset timestamp = @event.Timestamp.ToUniversalTime();
            projectedAt = projectedAt is { } known && known >= timestamp ? known : timestamp;
        }

        return ToReadModel(state, lastSequence, projectedAt);
    }

    /// <summary>Rebuilds the folded Agent state from a persisted read model, for the derived read paths.</summary>
    /// <param name="model">The persisted read model, or <see langword="null"/>.</param>
    /// <param name="tenantId">The tenant scope to seed an empty state with.</param>
    /// <param name="agentId">The Agent id to seed an empty state with.</param>
    /// <returns>The rehydrated state.</returns>
    public static AgentState ToState(AgentSetupReadModel? model, string tenantId, string agentId)
    {
        if (model is null || !model.IsCreated)
        {
            return new AgentState { AgentId = agentId, TenantId = tenantId };
        }

        return new AgentState
        {
            IsCreated = true,
            AgentId = model.AgentId,
            TenantId = model.TenantId,
            DisplayName = model.DisplayName,
            Description = model.Description,
            Instructions = (model.HasInstructions, model.InstructionsValid) switch
            {
                (false, _) => string.Empty,
                (true, true) => _validInstructionsStandIn,
                (true, false) => _invalidInstructionsStandIn,
            },
            Lifecycle = model.Lifecycle,
            ConfigurationVersion = model.ConfigurationVersion,
            InstructionsVersion = model.InstructionsVersion,
            PartyId = model.PartyId,
            ProviderId = model.ProviderId,
            ModelId = model.ModelId,
            ProviderCapabilityVersion = model.ProviderCapabilityVersion,
            ResponseMode = model.ResponseMode,
            ApproverPolicySources = model.ApproverPolicySources.Count == 0 ? null : model.ApproverPolicySources,
            ApproverPolicyDisclosure = model.ApproverPolicyDisclosure,
            ApproverPolicyVersion = model.ApproverPolicyVersion,
            ContentSafety = model.HasContentSafetyPolicy
                ? new AgentContentSafetyConfiguration(
                    _contentSafetyStandIn,
                    model.HasAutomaticContentSafetyOverride ? _contentSafetyStandIn : null,
                    model.HasConfirmationContentSafetyOverride ? _contentSafetyStandIn : null)
                : null,
            ContentSafetyPolicyVersion = model.ContentSafetyPolicyVersion,
            LaunchReadiness = model.LaunchReadiness,
            LaunchReadinessVersion = model.LaunchReadinessVersion,
            ProductionLikeGenerationEnabled = model.ProductionLikeGenerationEnabled,
        };
    }

    private static AgentSetupReadModel ToReadModel(AgentState state, long lastSequence, DateTimeOffset? projectedAt)
    {
        AgentInspectionResult inspection = AgentInspection.GetStatus(state, isAgentsAdmin: true);
        AgentStatusView? view = inspection.Agent;

        return new AgentSetupReadModel
        {
            IsCreated = state.IsCreated,
            AgentId = state.AgentId,
            TenantId = state.TenantId,
            DisplayName = state.DisplayName,
            Description = state.Description,
            HasInstructions = view?.HasInstructions ?? false,
            InstructionsValid = view?.InstructionsValid ?? false,
            InstructionsVersion = state.InstructionsVersion,
            Lifecycle = state.Lifecycle,
            ConfigurationVersion = state.ConfigurationVersion,
            PartyId = state.PartyId,
            ProviderId = state.ProviderId,
            ModelId = state.ModelId,
            ProviderCapabilityVersion = state.ProviderCapabilityVersion,
            ResponseMode = state.ResponseMode,
            ApproverPolicySources = state.ApproverPolicySources ?? [],
            ApproverPolicyDisclosure = state.ApproverPolicyDisclosure,
            ApproverPolicyVersion = state.ApproverPolicyVersion,
            HasContentSafetyPolicy = state.ContentSafety is not null,
            HasAutomaticContentSafetyOverride = state.ContentSafety?.AutomaticModePolicy is not null,
            HasConfirmationContentSafetyOverride = state.ContentSafety?.ConfirmationModePolicy is not null,
            ContentSafetyPolicyVersion = state.ContentSafetyPolicyVersion,
            LaunchReadiness = state.LaunchReadiness,
            LaunchReadinessVersion = state.LaunchReadinessVersion,
            ProductionLikeGenerationEnabled = state.ProductionLikeGenerationEnabled,
            LastSequenceNumber = lastSequence,
            ProjectedAt = projectedAt,
            ProjectionVersion = lastSequence <= 0
                ? null
                : lastSequence.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static object? Deserialize(byte[] payload, Type eventType)
    {
        try
        {
            return payload is null or { Length: 0 }
                ? null
                : JsonSerializer.Deserialize(payload, eventType, _jsonOptions);
        }
        catch (JsonException)
        {
            // A corrupt payload can never be repaired by a retry, so the checkpoint still advances past it and
            // state is left untouched rather than wedging every later delivery for this Agent.
            return null;
        }
    }

    private static void Apply(AgentState state, object payload)
    {
        switch (payload)
        {
            case AgentCreated e: state.Apply(e); break;
            case AgentConfigurationUpdated e: state.Apply(e); break;
            case AgentActivated e: state.Apply(e); break;
            case AgentDisabled e: state.Apply(e); break;
            case AgentPartyIdentityLinked e: state.Apply(e); break;
            case AgentPartyIdentityReplaced e: state.Apply(e); break;
            case AgentProviderModelSelected e: state.Apply(e); break;
            case AgentResponseModeConfigured e: state.Apply(e); break;
            case AgentApproverPolicyConfigured e: state.Apply(e); break;
            case AgentContentSafetyPolicyConfigured e: state.Apply(e); break;
            case AgentLaunchReadinessRecorded e: state.Apply(e); break;
            case AgentProductionLikeGenerationEnabled e: state.Apply(e); break;
            default: break; // Rejection events carry no state change; replay stays total.
        }
    }
}
