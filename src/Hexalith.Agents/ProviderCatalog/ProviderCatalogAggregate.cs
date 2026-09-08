using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.ProviderCatalog;

/// <summary>
/// Pure, replay-safe aggregate for the tenant-scoped governed provider/model catalog (AD-2, AD-3, AD-9, AD-10).
/// Static <c>Handle(command, state, envelope) -&gt; DomainResult</c> methods (discovered by the EventStore
/// client by convention) decide success events, typed rejection events, or a deterministic no-op. The aggregate
/// performs no I/O, no provider/secret-store/Dapr access, and no wall-clock reads.
/// </summary>
/// <remarks>
/// Authorization is transitional: until the full Agents authorization story lands, provider administration is
/// gated by a trusted, server-populated command-envelope extension (<c>actor:agentsProviderAdmin</c>),
/// patterned after the Tenants <c>actor:globalAdmin</c> extension. Client-provided reserved extensions must be
/// stripped by the command entry point and never trusted here. Replace this gate when the Agents authorization
/// model exists.
/// </remarks>
[EventStoreDomain("provider-catalog")]
public class ProviderCatalogAggregate : EventStoreAggregate<ProviderCatalogState>
{
    /// <summary>Maximum allowed per-request timeout (10 minutes), in milliseconds.</summary>
    internal const int MaxRequestTimeoutMilliseconds = 600_000;

    /// <summary>Maximum allowed retry count for a single invocation.</summary>
    internal const int MaxRetryCount = 10;

    /// <summary>Maximum length of a safe configuration reference identifier.</summary>
    internal const int MaxConfigurationReferenceLength = 128;

    /// <summary>Maximum length of a provider/model identifier.</summary>
    internal const int MaxIdentifierLength = 128;

    /// <summary>Maximum length of the safe display label.</summary>
    internal const int MaxDisplayLabelLength = 256;

    /// <summary>The kebab-case EventStore domain this aggregate owns.</summary>
    public const string Domain = "provider-catalog";

    // SECURITY: server-populated only (patterned after Tenants' "actor:globalAdmin"). The command entry point
    // strips client-provided reserved extensions and repopulates this key from trusted claims only.
    /// <summary>The server-populated provider-catalog administration extension key (client-stripped).</summary>
    public const string ProviderAdminExtensionKey = "actor:agentsProviderAdmin";

    private static readonly Regex _configurationReferenceRegex =
        new("^[A-Za-z0-9._:-]+$", RegexOptions.Compiled);

    private static readonly Regex _currencyRegex =
        new("^[A-Za-z]{3}$", RegexOptions.Compiled);

    /// <summary>Handles creation (or idempotent re-creation) of a provider/model catalog entry.</summary>
    /// <param name="command">The create command.</param>
    /// <param name="state">The current catalog state (null before the first entry exists).</param>
    /// <param name="envelope">The command envelope (carries the catalog id and trusted authorization extension).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(CreateProviderModelEntry command, ProviderCatalogState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string catalogId = envelope.AggregateId;

        if (!IsProviderAdmin(envelope))
        {
            return Denied(catalogId, envelope, nameof(CreateProviderModelEntry));
        }

        if (TryGetIdentifierRejection(catalogId, command.ProviderId, command.ModelId, out DomainResult? idRejection))
        {
            return idRejection;
        }

        if (TryGetConfigurationRejection(catalogId, command.ProviderId, command.ModelId, command.ConfigurationReferenceId, out DomainResult? cfgRejection))
        {
            return cfgRejection;
        }

        if (TryGetMetadataRejection(
                catalogId,
                command.ProviderId,
                command.ModelId,
                command.DisplayLabel,
                command.ContextWindowTokenLimit,
                command.MaxOutputTokenLimit,
                command.TimeoutPolicy,
                out DomainResult? metaRejection))
        {
            return metaRejection;
        }

        if (TryGetPricingRejection(catalogId, command.ProviderId, command.ModelId, command.Pricing, current: null, out DomainResult? pricingRejection))
        {
            return pricingRejection;
        }

        ProviderConfigurationState configurationState = ResolveConfigurationState(command.ConfigurationReferenceId);
        ProviderModelPricing pricing = AssignPricing(command.Pricing, current: null);
        ProviderModelEntryState? existing = FindEntry(state, command.ProviderId, command.ModelId);
        if (existing is not null)
        {
            // AC4: exact-duplicate create is a deterministic no-op; a conflicting payload is rejected and never
            // mutates state silently.
            return CreateMatchesExisting(existing, command, configurationState, pricing)
                ? DomainResult.NoOp()
                : DomainResult.Rejection([new ProviderModelEntryAlreadyExistsRejection(catalogId, command.ProviderId, command.ModelId)]);
        }

        return DomainResult.Success([
            new ProviderModelEntryCreated(
                catalogId,
                command.ProviderId,
                command.ModelId,
                command.DisplayLabel,
                command.Enabled,
                command.SupportsTextGeneration,
                command.ContextWindowTokenLimit,
                command.MaxOutputTokenLimit,
                command.TimeoutPolicy,
                command.SafeCapabilityFlags,
                configurationState,
                command.ConfigurationReferenceId,
                pricing,
                CapabilityVersion: 1),
        ]);
    }

    /// <summary>Handles a safe-metadata or pricing update of an existing provider/model catalog entry.</summary>
    /// <param name="command">The update command.</param>
    /// <param name="state">The current catalog state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(UpdateProviderModelEntry command, ProviderCatalogState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string catalogId = envelope.AggregateId;

        if (!IsProviderAdmin(envelope))
        {
            return Denied(catalogId, envelope, nameof(UpdateProviderModelEntry));
        }

        if (TryGetConfigurationRejection(catalogId, command.ProviderId, command.ModelId, command.ConfigurationReferenceId, out DomainResult? cfgRejection))
        {
            return cfgRejection;
        }

        if (TryGetMetadataRejection(
                catalogId,
                command.ProviderId,
                command.ModelId,
                command.DisplayLabel,
                command.ContextWindowTokenLimit,
                command.MaxOutputTokenLimit,
                command.TimeoutPolicy,
                out DomainResult? metaRejection))
        {
            return metaRejection;
        }

        ProviderModelEntryState? existing = FindEntry(state, command.ProviderId, command.ModelId);
        if (existing is null)
        {
            return DomainResult.Rejection([new ProviderModelEntryNotFoundRejection(catalogId, command.ProviderId, command.ModelId)]);
        }

        if (TryGetCapabilityVersionRejection(catalogId, command.ProviderId, command.ModelId, command.ExpectedCapabilityVersion, existing.CapabilityVersion, out DomainResult? versionRejection))
        {
            return versionRejection;
        }

        if (TryGetPricingRejection(catalogId, command.ProviderId, command.ModelId, command.Pricing, existing.Pricing, out DomainResult? pricingRejection))
        {
            return pricingRejection;
        }

        ProviderConfigurationState configurationState = ResolveConfigurationState(command.ConfigurationReferenceId);
        ProviderModelPricing pricing = AssignPricing(command.Pricing, existing.Pricing);

        // AC4: an update that changes nothing is a deterministic no-op.
        if (UpdateMatchesExisting(existing, command, configurationState, pricing))
        {
            return DomainResult.NoOp();
        }

        int nextCapabilityVersion = existing.CapabilityVersion + 1;
        return DomainResult.Success([
            new ProviderModelEntryMetadataUpdated(
                catalogId,
                command.ProviderId,
                command.ModelId,
                command.DisplayLabel,
                command.SupportsTextGeneration,
                command.ContextWindowTokenLimit,
                command.MaxOutputTokenLimit,
                command.TimeoutPolicy,
                command.SafeCapabilityFlags,
                configurationState,
                command.ConfigurationReferenceId,
                pricing,
                nextCapabilityVersion),
        ]);
    }

    /// <summary>Handles enabling an existing provider/model catalog entry.</summary>
    /// <param name="command">The enable command.</param>
    /// <param name="state">The current catalog state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(EnableProviderModelEntry command, ProviderCatalogState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string catalogId = envelope.AggregateId;

        if (!IsProviderAdmin(envelope))
        {
            return Denied(catalogId, envelope, nameof(EnableProviderModelEntry));
        }

        ProviderModelEntryState? existing = FindEntry(state, command.ProviderId, command.ModelId);
        return existing switch
        {
            null => DomainResult.Rejection([new ProviderModelEntryNotFoundRejection(catalogId, command.ProviderId, command.ModelId)]),
            { IsEnabled: true } => DomainResult.Rejection([
                new ProviderModelEntryLifecycleStateAlreadySetRejection(
                    catalogId,
                    command.ProviderId,
                    command.ModelId,
                    ProviderModelStatus.Enabled,
                    ProviderModelStatus.Enabled,
                    nameof(EnableProviderModelEntry)),
            ]),
            _ => DomainResult.Success([new ProviderModelEntryEnabled(catalogId, command.ProviderId, command.ModelId)]),
        };
    }

    /// <summary>Handles disabling an existing provider/model catalog entry (history preserved).</summary>
    /// <param name="command">The disable command.</param>
    /// <param name="state">The current catalog state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(DisableProviderModelEntry command, ProviderCatalogState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string catalogId = envelope.AggregateId;

        if (!IsProviderAdmin(envelope))
        {
            return Denied(catalogId, envelope, nameof(DisableProviderModelEntry));
        }

        ProviderModelEntryState? existing = FindEntry(state, command.ProviderId, command.ModelId);
        return existing switch
        {
            null => DomainResult.Rejection([new ProviderModelEntryNotFoundRejection(catalogId, command.ProviderId, command.ModelId)]),
            { IsEnabled: false } => DomainResult.Rejection([
                new ProviderModelEntryLifecycleStateAlreadySetRejection(
                    catalogId,
                    command.ProviderId,
                    command.ModelId,
                    ProviderModelStatus.Disabled,
                    ProviderModelStatus.Disabled,
                    nameof(DisableProviderModelEntry)),
            ]),
            _ => DomainResult.Success([new ProviderModelEntryDisabled(catalogId, command.ProviderId, command.ModelId)]),
        };
    }

    /// <summary>
    /// Computes the safe configured-state of an entry from its configuration reference: a non-blank reference is
    /// <see cref="ProviderConfigurationState.Configured"/>; otherwise <see cref="ProviderConfigurationState.NotConfigured"/>.
    /// </summary>
    /// <param name="configurationReferenceId">The optional safe configuration reference identifier.</param>
    /// <returns>The resolved configuration state.</returns>
    internal static ProviderConfigurationState ResolveConfigurationState(string? configurationReferenceId)
        => string.IsNullOrWhiteSpace(configurationReferenceId)
            ? ProviderConfigurationState.NotConfigured
            : ProviderConfigurationState.Configured;

    /// <summary>Returns whether the administrator-supplied pricing is present and valid.</summary>
    /// <param name="pricing">The pricing to inspect.</param>
    /// <returns><see langword="true"/> when currency and unit prices are valid.</returns>
    internal static bool HasValidPricing(ProviderModelPricing? pricing)
        => ValidatePricing(pricing, current: null) is null;

    private static bool IsProviderAdmin(CommandEnvelope envelope)
        => envelope.Extensions?.TryGetValue(ProviderAdminExtensionKey, out string? value) == true
            && string.Equals(value, "true", StringComparison.Ordinal);

    private static DomainResult Denied(string catalogId, CommandEnvelope envelope, string commandName)
        => DomainResult.Rejection([new ProviderCatalogAdministrationDeniedRejection(catalogId, envelope.UserId, commandName)]);

    private static ProviderModelEntryState? FindEntry(ProviderCatalogState? state, string providerId, string modelId)
        => state is not null && state.Entries.TryGetValue(ProviderCatalogState.EntryKey(providerId, modelId), out ProviderModelEntryState? entry)
            ? entry
            : null;

    private static bool TryGetIdentifierRejection(string catalogId, string providerId, string modelId, [NotNullWhen(true)] out DomainResult? rejection)
    {
        string? reason = null;
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(modelId))
        {
            reason = "ProviderId and ModelId are required.";
        }
        else if (providerId.Length > MaxIdentifierLength || modelId.Length > MaxIdentifierLength)
        {
            reason = $"ProviderId and ModelId must not exceed {MaxIdentifierLength} characters.";
        }

        rejection = reason is null
            ? null
            : DomainResult.Rejection([new InvalidProviderModelMetadataRejection(catalogId, providerId, modelId, reason)]);
        return rejection is not null;
    }

    private static bool TryGetConfigurationRejection(
        string catalogId,
        string providerId,
        string modelId,
        string? configurationReferenceId,
        [NotNullWhen(true)] out DomainResult? rejection)
    {
        string? reason = null;
        if (!string.IsNullOrWhiteSpace(configurationReferenceId))
        {
            if (configurationReferenceId.Length > MaxConfigurationReferenceLength)
            {
                reason = $"ConfigurationReferenceId must not exceed {MaxConfigurationReferenceLength} characters.";
            }
            else if (!_configurationReferenceRegex.IsMatch(configurationReferenceId))
            {
                reason = "ConfigurationReferenceId must be a safe opaque reference (letters, digits, '.', '_', ':', '-').";
            }
        }

        rejection = reason is null
            ? null
            : DomainResult.Rejection([new UnsafeProviderConfigurationInputRejection(catalogId, providerId, modelId, reason)]);
        return rejection is not null;
    }

    private static bool TryGetMetadataRejection(
        string catalogId,
        string providerId,
        string modelId,
        string displayLabel,
        int contextWindowTokenLimit,
        int maxOutputTokenLimit,
        ProviderModelTimeoutPolicy timeoutPolicy,
        [NotNullWhen(true)] out DomainResult? rejection)
    {
        string? reason = ValidateMetadata(displayLabel, contextWindowTokenLimit, maxOutputTokenLimit, timeoutPolicy);
        rejection = reason is null
            ? null
            : DomainResult.Rejection([new InvalidProviderModelMetadataRejection(catalogId, providerId, modelId, reason)]);
        return rejection is not null;
    }

    private static bool TryGetPricingRejection(
        string catalogId,
        string providerId,
        string modelId,
        ProviderModelPricing? pricing,
        ProviderModelPricing? current,
        [NotNullWhen(true)] out DomainResult? rejection)
    {
        string? reason = ValidatePricing(pricing, current);
        rejection = reason is null
            ? null
            : DomainResult.Rejection([new InvalidProviderModelPricingRejection(catalogId, providerId, modelId, reason)]);
        return rejection is not null;
    }

    private static bool TryGetCapabilityVersionRejection(
        string catalogId,
        string providerId,
        string modelId,
        int? expectedCapabilityVersion,
        int currentCapabilityVersion,
        [NotNullWhen(true)] out DomainResult? rejection)
    {
        if (expectedCapabilityVersion is null)
        {
            rejection = null;
            return false;
        }

        if (expectedCapabilityVersion.Value < currentCapabilityVersion)
        {
            rejection = DomainResult.Rejection([
                new ProviderModelCapabilityVersionRegressedRejection(
                    catalogId,
                    providerId,
                    modelId,
                    expectedCapabilityVersion.Value,
                    currentCapabilityVersion),
            ]);
            return true;
        }

        if (expectedCapabilityVersion.Value > currentCapabilityVersion)
        {
            rejection = DomainResult.Rejection([
                new ProviderModelEntryStaleRevisionRejection(
                    catalogId,
                    providerId,
                    modelId,
                    expectedCapabilityVersion.Value,
                    currentCapabilityVersion),
            ]);
            return true;
        }

        rejection = null;
        return false;
    }

    private static string? ValidateMetadata(
        string displayLabel,
        int contextWindowTokenLimit,
        int maxOutputTokenLimit,
        ProviderModelTimeoutPolicy timeoutPolicy)
    {
        if (string.IsNullOrWhiteSpace(displayLabel) || displayLabel.Length > MaxDisplayLabelLength)
        {
            return $"DisplayLabel is required and must not exceed {MaxDisplayLabelLength} characters.";
        }

        if (contextWindowTokenLimit <= 0)
        {
            return "ContextWindowTokenLimit must be positive.";
        }

        if (maxOutputTokenLimit <= 0)
        {
            return "MaxOutputTokenLimit must be positive.";
        }

        if (maxOutputTokenLimit > contextWindowTokenLimit)
        {
            return "MaxOutputTokenLimit must not exceed ContextWindowTokenLimit.";
        }

        if (timeoutPolicy is null)
        {
            return "TimeoutPolicy is required.";
        }

        if (timeoutPolicy.RequestTimeoutMilliseconds <= 0 || timeoutPolicy.RequestTimeoutMilliseconds > MaxRequestTimeoutMilliseconds)
        {
            return $"TimeoutPolicy.RequestTimeoutMilliseconds must be between 1 and {MaxRequestTimeoutMilliseconds}.";
        }

        return timeoutPolicy.MaxRetries is < 0 or > MaxRetryCount
            ? $"TimeoutPolicy.MaxRetries must be between 0 and {MaxRetryCount}."
            : null;
    }

    private static string? ValidatePricing(ProviderModelPricing? pricing, ProviderModelPricing? current)
    {
        if (pricing is null || string.IsNullOrWhiteSpace(pricing.Currency))
        {
            return "Pricing units and currency are required.";
        }

        if (!_currencyRegex.IsMatch(pricing.Currency))
        {
            return "Pricing.Currency must be a three-letter ISO 4217 code.";
        }

        if (pricing.InputTokenUnitPrice < 0 || pricing.OutputTokenUnitPrice < 0)
        {
            return "Pricing unit prices must be non-negative.";
        }

        if (pricing.PricingVersion < 0)
        {
            return "Pricing.PricingVersion must not be negative.";
        }

        if (current is null && pricing.PricingVersion is not 0 and not 1)
        {
            return "Pricing.PricingVersion must be 1 on create.";
        }

        if (current is not null && pricing.PricingVersion > 0 && pricing.PricingVersion < current.PricingVersion)
        {
            return "Pricing.PricingVersion must not decrease or be reused.";
        }

        return null;
    }

    private static ProviderModelPricing AssignPricing(ProviderModelPricing pricing, ProviderModelPricing? current)
    {
        string currency = pricing.Currency.ToUpperInvariant();
        bool unitsChanged = current is null
            || !string.Equals(current.Currency, currency, StringComparison.Ordinal)
            || current.InputTokenUnitPrice != pricing.InputTokenUnitPrice
            || current.OutputTokenUnitPrice != pricing.OutputTokenUnitPrice;

        int version = current is null
            ? 1
            : unitsChanged ? current.PricingVersion + 1 : current.PricingVersion;

        return new ProviderModelPricing(currency, pricing.InputTokenUnitPrice, pricing.OutputTokenUnitPrice, version);
    }

    private static bool CreateMatchesExisting(
        ProviderModelEntryState existing,
        CreateProviderModelEntry command,
        ProviderConfigurationState configurationState,
        ProviderModelPricing pricing)
        => existing.IsEnabled == command.Enabled
            && existing.CapabilityVersion == 1
            && SafeMetadataMatches(
                existing,
                command.DisplayLabel,
                command.SupportsTextGeneration,
                command.ContextWindowTokenLimit,
                command.MaxOutputTokenLimit,
                command.TimeoutPolicy,
                command.SafeCapabilityFlags,
                configurationState,
                command.ConfigurationReferenceId,
                pricing);

    private static bool UpdateMatchesExisting(
        ProviderModelEntryState existing,
        UpdateProviderModelEntry command,
        ProviderConfigurationState configurationState,
        ProviderModelPricing pricing)
        => SafeMetadataMatches(
            existing,
            command.DisplayLabel,
            command.SupportsTextGeneration,
            command.ContextWindowTokenLimit,
            command.MaxOutputTokenLimit,
            command.TimeoutPolicy,
            command.SafeCapabilityFlags,
            configurationState,
            command.ConfigurationReferenceId,
            pricing);

    private static bool SafeMetadataMatches(
        ProviderModelEntryState existing,
        string displayLabel,
        bool supportsTextGeneration,
        int contextWindowTokenLimit,
        int maxOutputTokenLimit,
        ProviderModelTimeoutPolicy timeoutPolicy,
        ProviderModelCapabilityFlags safeCapabilityFlags,
        ProviderConfigurationState configurationState,
        string? configurationReferenceId,
        ProviderModelPricing pricing)
        => existing.DisplayLabel == displayLabel
            && existing.SupportsTextGeneration == supportsTextGeneration
            && existing.ContextWindowTokenLimit == contextWindowTokenLimit
            && existing.MaxOutputTokenLimit == maxOutputTokenLimit
            && existing.TimeoutPolicy == timeoutPolicy
            && existing.SafeCapabilityFlags == safeCapabilityFlags
            && existing.ConfigurationState == configurationState
            && existing.ConfigurationReferenceId == configurationReferenceId
            && PricingUnitsMatch(existing.Pricing, pricing);

    private static bool PricingUnitsMatch(ProviderModelPricing? existing, ProviderModelPricing proposed)
        => existing is not null
            && string.Equals(existing.Currency, proposed.Currency, StringComparison.Ordinal)
            && existing.InputTokenUnitPrice == proposed.InputTokenUnitPrice
            && existing.OutputTokenUnitPrice == proposed.OutputTokenUnitPrice;
}
