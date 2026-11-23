using System.Text;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>Prepares and measures the transient Conversation payload used for context readiness and generation.</summary>
internal static class GenerationContext
{
    /// <summary>Reads the authorized visible timeline and renders the provider payload when the timeline is usable.</summary>
    internal static async Task<(string? Payload, int MessageCount, AgentInteractionContextLoadOutcome Outcome)> ReadPayloadAsync(
        IConversationContextReader reader, string tenantId, string conversationId, string callerPartyId, string actorUserId, string correlationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            // The authorized read keys on the actor security principal.
            ConversationContextReadResult read = await reader
                .ReadAsync(tenantId, conversationId, callerPartyId, actorUserId, correlationId, ct)
                .ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            if (read is not { Outcome: AgentInteractionContextLoadOutcome.Loaded })
            {
                return (null, 0, read?.Outcome ?? AgentInteractionContextLoadOutcome.Unavailable);
            }

            if (!read.IsFresh)
            {
                return (null, 0, AgentInteractionContextLoadOutcome.Stale);
            }

            if (read.Messages is not { } messages)
            {
                return (null, 0, AgentInteractionContextLoadOutcome.Unavailable);
            }

            ConversationContextMessage[] timeline = [.. messages];
            if (read.MessageCount != timeline.Length
                || timeline.Any(message => message is null
                    || string.IsNullOrWhiteSpace(message.Text)
                    || message.CreatedAt == DateTimeOffset.MinValue))
            {
                return (null, 0, AgentInteractionContextLoadOutcome.Unavailable);
            }

            var payload = new StringBuilder();
            foreach (ConversationContextMessage message in timeline.OrderBy(message => message.CreatedAt))
            {
                payload.AppendLine(message.Text);
            }

            return (payload.ToString(), timeline.Length, AgentInteractionContextLoadOutcome.Loaded);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            ct.ThrowIfCancellationRequested();
            return (null, 0, AgentInteractionContextLoadOutcome.Unavailable);
        }
    }

    /// <summary>Checks that the exact payload fits after reserving the output allowance and safety margin.</summary>
    internal static async Task<bool> FitsAsync(
        IConversationContextTokenMeasurer measurer, string payload, ProviderCatalogEntryView entry, CancellationToken ct)
    {
        ConversationContextTokenMeasurement measurement = await MeasureAsync(measurer, payload, entry, ct).ConfigureAwait(false);
        int availableTokens = AgentInteractionContextPolicy.CalculateDefaultBudget(
            entry.ContextWindowTokenLimit, entry.MaxOutputTokenLimit).AvailableTokens;
        return measurement.IsAvailable && measurement.TokenCount <= availableTokens;
    }

    /// <summary>Measures the exact serialized payload against the admitted catalog entry.</summary>
    internal static async Task<ConversationContextTokenMeasurement> MeasureAsync(
        IConversationContextTokenMeasurer measurer, string payload, ProviderCatalogEntryView entry, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            ConversationContextTokenMeasurement measurement = await measurer
                .MeasurePayloadAsync(payload, entry.ProviderId, entry.ModelId, entry.CapabilityVersion, ct)
                .ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return measurement is { IsAvailable: true, TokenCount: >= 0 }
                && (payload.Length == 0 || measurement.TokenCount > 0)
                ? measurement
                : ConversationContextTokenMeasurement.NotAvailable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            ct.ThrowIfCancellationRequested();
            return ConversationContextTokenMeasurement.NotAvailable;
        }
    }
}
