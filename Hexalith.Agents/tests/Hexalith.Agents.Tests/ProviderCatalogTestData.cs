using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Shared fixtures for the ProviderCatalog aggregate, state-replay, and inspection tests: a valid command
/// envelope builder (with the trusted provider-admin extension), a valid create command, and helpers to
/// pre-build catalog state from applied creation events.
/// </summary>
internal static class ProviderCatalogTestData
{
    internal const string CatalogId = "acme";
    internal const string ProviderAdminExtensionKey = "actor:agentsProviderAdmin";

    internal static CommandEnvelope Envelope<T>(
        T command,
        bool isProviderAdmin = true,
        string catalogId = CatalogId,
        string actorUserId = "admin-user")
        where T : notnull
        => new(
            "msg-" + typeof(T).Name,
            "acme",
            "provider-catalog",
            catalogId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            isProviderAdmin
                ? new Dictionary<string, string> { [ProviderAdminExtensionKey] = "true" }
                : null);

    internal static CreateProviderModelEntry ValidCreate(
        bool enabled = true,
        string providerId = "openai",
        string modelId = "gpt-4o",
        string displayLabel = "OpenAI GPT-4o",
        string? configurationReferenceId = "cfg-openai-gpt4o")
        => new(
            providerId,
            modelId,
            displayLabel,
            enabled,
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 128_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming | ProviderModelCapabilityFlags.ToolCalling,
            configurationReferenceId);

    internal static ProviderModelEntryCreated CreatedEvent(CreateProviderModelEntry create, string catalogId = CatalogId)
        => new(
            catalogId,
            create.ProviderId,
            create.ModelId,
            create.DisplayLabel,
            create.Enabled,
            create.SupportsTextGeneration,
            create.ContextWindowTokenLimit,
            create.MaxOutputTokenLimit,
            create.TimeoutPolicy,
            create.SafeCapabilityFlags,
            string.IsNullOrWhiteSpace(create.ConfigurationReferenceId)
                ? ProviderConfigurationState.NotConfigured
                : ProviderConfigurationState.Configured,
            create.ConfigurationReferenceId);

    internal static ProviderCatalogState StateWith(params CreateProviderModelEntry[] creates)
    {
        var state = new ProviderCatalogState();
        foreach (CreateProviderModelEntry create in creates)
        {
            state.Apply(CreatedEvent(create));
        }

        return state;
    }

    /// <summary>
    /// Applies every event of a <see cref="DomainResult"/> to the supplied state through the aggregate's typed
    /// <c>Apply</c> methods — the same production replay handlers the EventStore state-store invokes. Lets the
    /// end-to-end lifecycle tests thread evolving state across successive commands exactly as a real stream replay
    /// would. Success events advance state; rejection events are replay-safe no-ops (they must not mutate state).
    /// </summary>
    /// <param name="state">The catalog state to advance in place.</param>
    /// <param name="result">The domain result whose events are applied in order.</param>
    internal static void ApplyAll(ProviderCatalogState state, DomainResult result)
    {
        foreach (IEventPayload payload in result.Events)
        {
            switch (payload)
            {
                case ProviderModelEntryCreated e: state.Apply(e); break;
                case ProviderModelEntryMetadataUpdated e: state.Apply(e); break;
                case ProviderModelEntryEnabled e: state.Apply(e); break;
                case ProviderModelEntryDisabled e: state.Apply(e); break;
                case ProviderCatalogAdministrationDeniedRejection e: state.Apply(e); break;
                case ProviderModelEntryAlreadyExistsRejection e: state.Apply(e); break;
                case ProviderModelEntryNotFoundRejection e: state.Apply(e); break;
                case ProviderModelEntryLifecycleStateAlreadySetRejection e: state.Apply(e); break;
                case InvalidProviderModelMetadataRejection e: state.Apply(e); break;
                case UnsafeProviderConfigurationInputRejection e: state.Apply(e); break;
                default: throw new InvalidOperationException($"Unhandled event type '{payload.GetType().Name}' in test apply dispatch.");
            }
        }
    }

    /// <summary>
    /// Drives one command end-to-end through the real aggregate pipeline — JSON-serialized command envelope →
    /// reflection dispatch in <see cref="ProviderCatalogAggregate.ProcessAsync"/> → typed handler → events — then
    /// applies the resulting events to <paramref name="state"/> so the next command sees the evolved state.
    /// </summary>
    /// <typeparam name="TCommand">The command type (drives the dispatch lookup and payload round-trip).</typeparam>
    /// <param name="aggregate">The aggregate under test.</param>
    /// <param name="state">The threaded catalog state, advanced in place.</param>
    /// <param name="command">The command to process.</param>
    /// <param name="isProviderAdmin">Whether the trusted provider-admin extension is present (AC3).</param>
    /// <returns>The domain result of processing the command.</returns>
    internal static async Task<DomainResult> ProcessAndApplyAsync<TCommand>(
        ProviderCatalogAggregate aggregate,
        ProviderCatalogState state,
        TCommand command,
        bool isProviderAdmin = true)
        where TCommand : notnull
    {
        DomainResult result = await aggregate.ProcessAsync(Envelope(command, isProviderAdmin), state);
        ApplyAll(state, result);
        return result;
    }
}
