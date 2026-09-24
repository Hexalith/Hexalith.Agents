using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Server.Commands;

namespace Hexalith.Agents.EventStore;

/// <summary>Coordinates each platform model stream with its tenant enablement and terms decisions.</summary>
public sealed class AgentsProviderCatalogCoordinationPolicy : ICoordinatedCommandPolicy
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <inheritdoc />
    public bool Claims(string domain, string commandType)
        => (string.Equals(domain, ProviderCatalogIdentity.Domain, StringComparison.Ordinal)
            && commandType is (nameof(CreateProviderModelEntry) or nameof(UpdateProviderModelEntry)
                or nameof(EnableProviderModelEntry) or nameof(DisableProviderModelEntry)))
            || (string.Equals(domain, "tenant-provider-enablement", StringComparison.Ordinal)
                && commandType is (nameof(SetTenantProviderModelEnablement) or nameof(DecideProviderDataHandling)));

    /// <inheritdoc />
    public CoordinatedCommandScope GetScope(CommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Claims(command.Domain, command.CommandType))
        {
            throw new InvalidOperationException("This command is not owned by the provider coordinator.");
        }

        (string providerId, string modelId, bool enablementEnabled) = ReadIdentity(command);
        string entryId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        bool platform = string.Equals(command.Domain, ProviderCatalogIdentity.Domain, StringComparison.Ordinal);
        if (platform
            ? !string.Equals(command.TenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal)
                || !string.Equals(command.AggregateId, entryId, StringComparison.Ordinal)
            : string.Equals(command.TenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal)
                || !string.Equals(command.AggregateId, command.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The coordinated command has an invalid aggregate scope.");
        }

        var source = new AggregateIdentity(ProviderCatalogIdentity.PlatformTenantId,
            ProviderCatalogIdentity.Domain, entryId);
        return new CoordinatedCommandScope(source,
            !platform && (command.CommandType == nameof(DecideProviderDataHandling) || enablementEnabled));
    }

    /// <inheritdoc />
    public string GetCommandDigest(CommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);
        byte[] semanticPayload = ProviderGovernanceCommandIntent.Normalize(command.CommandType, command.Payload);
        string[] extensions = command.Extensions is null ? []
            : [.. command.Extensions
                .Where(item => !string.Equals(item.Key, "traceparent", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(item.Key, "tracestate", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key.Length}:{item.Key}{item.Value.Length}:{item.Value}")];
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            command.MessageId,
            command.TenantId,
            command.Domain,
            command.AggregateId,
            command.CommandType,
            command.UserId,
            Payload = Convert.ToBase64String(semanticPayload),
            Extensions = extensions,
        });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    /// <inheritdoc />
    public bool Validate(CommandEnvelope command, IReadOnlyList<ProjectionEventDto> sourceEvents)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(sourceEvents);
        if (string.Equals(command.Domain, ProviderCatalogIdentity.Domain, StringComparison.Ordinal)
            || command.CommandType == nameof(SetTenantProviderModelEnablement)
                && Deserialize<SetTenantProviderModelEnablement>(command.Payload).Enabled is false)
        {
            return true;
        }

        (string providerId, string modelId, _) = ReadIdentity(command);
        string expectedEntryId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        bool exists = false;
        bool enabled = false;
        ProviderDataHandlingRecord? currentTerms = null;
        long sequence = 0;
        foreach (ProjectionEventDto item in sourceEvents.OrderBy(item => item.SequenceNumber))
        {
            if (item.SequenceNumber != sequence + 1)
            {
                throw new InvalidOperationException("The authoritative platform stream has a sequence gap.");
            }

            sequence = item.SequenceNumber;
            switch (item.EventTypeName.Split('.').Last())
            {
                case nameof(ProviderModelEntryCreated):
                    ProviderModelEntryCreated created = Deserialize<ProviderModelEntryCreated>(item.Payload);
                    VerifySourceIdentity(created.CatalogId, created.ProviderId, created.ModelId,
                        expectedEntryId, providerId, modelId);
                    if (exists)
                    {
                        throw new InvalidOperationException("The authoritative platform stream creates an entry twice.");
                    }

                    exists = true;
                    enabled = created.Enabled;
                    currentTerms = created.DataHandling;
                    break;
                case nameof(ProviderModelEntryMetadataUpdated):
                    ProviderModelEntryMetadataUpdated updated = Deserialize<ProviderModelEntryMetadataUpdated>(item.Payload);
                    VerifySourceIdentity(updated.CatalogId, updated.ProviderId, updated.ModelId,
                        expectedEntryId, providerId, modelId);
                    if (!exists)
                    {
                        throw new InvalidOperationException("The authoritative platform stream updates an absent entry.");
                    }

                    currentTerms = updated.DataHandling;
                    break;
                case nameof(ProviderModelEntryEnabled):
                    ProviderModelEntryEnabled enabledEvent = Deserialize<ProviderModelEntryEnabled>(item.Payload);
                    VerifySourceIdentity(enabledEvent.CatalogId, enabledEvent.ProviderId, enabledEvent.ModelId,
                        expectedEntryId, providerId, modelId);
                    if (!exists)
                    {
                        throw new InvalidOperationException("The authoritative platform stream enables an absent entry.");
                    }

                    enabled = true;
                    break;
                case nameof(ProviderModelEntryDisabled):
                    ProviderModelEntryDisabled disabledEvent = Deserialize<ProviderModelEntryDisabled>(item.Payload);
                    VerifySourceIdentity(disabledEvent.CatalogId, disabledEvent.ProviderId, disabledEvent.ModelId,
                        expectedEntryId, providerId, modelId);
                    if (!exists)
                    {
                        throw new InvalidOperationException("The authoritative platform stream disables an absent entry.");
                    }

                    enabled = false;
                    break;
                case string rejected when rejected.EndsWith("Rejection", StringComparison.Ordinal):
                    break;
                default:
                    throw new InvalidOperationException("The authoritative platform stream has an unknown event.");
            }
        }

        if (!exists || !enabled || currentTerms is null)
        {
            return false;
        }

        var confirmed = command.CommandType == nameof(DecideProviderDataHandling)
            ? Deserialize<DecideProviderDataHandling>(command.Payload).ConfirmedTerms
            : Deserialize<SetTenantProviderModelEnablement>(command.Payload).CurrentTerms;
        return confirmed is not null
            && confirmed.DataHandlingVersion == currentTerms.DataHandlingVersion
            && SameRenderedTermsSnapshot(confirmed, currentTerms);
    }

    private static bool SameRenderedTermsSnapshot(ProviderDataHandlingRecord confirmed, ProviderDataHandlingRecord current)
        // Serializing the strongly typed records gives a structural comparison of every rendered field,
        // including region order, EffectiveAt, and the full tightening declaration. A future field fails
        // closed until both the platform source and tenant confirmation carry the same value.
        => JsonSerializer.SerializeToUtf8Bytes(confirmed, _json).AsSpan()
            .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(current, _json));

    private static void VerifySourceIdentity(string entryId, string eventProviderId, string eventModelId,
        string expectedEntryId, string providerId, string modelId)
    {
        if (!string.Equals(entryId, expectedEntryId, StringComparison.Ordinal)
            || !string.Equals(eventProviderId, providerId, StringComparison.Ordinal)
            || !string.Equals(eventModelId, modelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The authoritative platform stream has an unexpected entry identity.");
        }
    }

    private static (string ProviderId, string ModelId, bool EnablementEnabled) ReadIdentity(CommandEnvelope command)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(command.Payload);
            JsonElement root = document.RootElement;
            string providerId = ReadString(root, "ProviderId");
            string modelId = ReadString(root, "ModelId");
            bool enabled = command.CommandType == nameof(SetTenantProviderModelEnablement)
                && ReadProperty(root, "Enabled").GetBoolean();
            return (providerId, modelId, enabled);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new InvalidOperationException("The coordinated command payload is invalid.", exception);
        }
    }

    private static string ReadString(JsonElement root, string name)
    {
        string? value = ReadProperty(root, name).GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidOperationException("The coordinated command identity is missing.");
    }

    private static JsonElement ReadProperty(JsonElement root, string name)
        => root.TryGetProperty(name, out JsonElement value) || root.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(name), out value)
            ? value : throw new KeyNotFoundException();

    private static T Deserialize<T>(byte[] payload)
        => JsonSerializer.Deserialize<T>(payload, _json)
            ?? throw new InvalidOperationException("A coordinated command or source event is invalid.");
}
