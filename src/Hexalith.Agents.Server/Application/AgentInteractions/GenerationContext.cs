using System.Text;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>Prepares and checks the transient Conversation payload shared by generation and regeneration.</summary>
internal static class GenerationContext
{
    /// <summary>Reads the authorized visible timeline and renders the provider payload when the timeline is usable.</summary>
    internal static async Task<string?> ReadPayloadAsync(
        IConversationContextReader reader, string tenantId, string conversationId, string actorUserId, string correlationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            // The authorized read keys on the actor security principal.
            ConversationContextReadResult read = await reader
                .ReadAsync(tenantId, conversationId, string.Empty, actorUserId, correlationId, ct)
                .ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            if (read is not { Outcome: AgentInteractionContextLoadOutcome.Loaded, IsFresh: true, Messages: { } messages })
            {
                return null;
            }

            ConversationContextMessage[] timeline = [.. messages];
            if (read.MessageCount != timeline.Length
                || timeline.Any(message => message is null
                    || string.IsNullOrWhiteSpace(message.Text)
                    || message.CreatedAt == DateTimeOffset.MinValue))
            {
                return null;
            }

            var payload = new StringBuilder();
            foreach (ConversationContextMessage message in timeline.OrderBy(message => message.CreatedAt))
            {
                payload.AppendLine(message.Text);
            }

            return payload.ToString();
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            ct.ThrowIfCancellationRequested();
            return null;
        }
    }

    /// <summary>Checks that the exact payload fits after reserving the admitted model's output allowance.</summary>
    internal static async Task<bool> FitsAsync(
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
                && measurement.TokenCount <= entry.ContextWindowTokenLimit - entry.MaxOutputTokenLimit;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            ct.ThrowIfCancellationRequested();
            return false;
        }
    }
}
