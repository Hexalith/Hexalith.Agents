using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

using Hexalith.EventStore.Contracts.Serialization;

namespace Hexalith.Agents.EventStore;

/// <summary>Normalizes server-stamped governance clocks and server-derived terms out of exact retry identity.</summary>
internal static class ProviderGovernanceCommandIntent
{
    public static byte[] Normalize(string commandType, byte[] payload)
    {
        object command = commandType switch
        {
            nameof(CreateProviderModelEntry) => NormalizeCreate(Read<CreateProviderModelEntry>(payload)),
            nameof(UpdateProviderModelEntry) => NormalizeUpdate(Read<UpdateProviderModelEntry>(payload)),
            nameof(EnableProviderModelEntry) => Read<EnableProviderModelEntry>(payload),
            nameof(DisableProviderModelEntry) => Read<DisableProviderModelEntry>(payload),
            // The server re-reads the current platform terms on every attempt; they are not caller intent.
            nameof(SetTenantProviderModelEnablement) => Read<SetTenantProviderModelEnablement>(payload) with { CurrentTerms = null },
            nameof(DecideProviderDataHandling) => Read<DecideProviderDataHandling>(payload) with { DecidedAt = default },
            _ => throw new InvalidOperationException("The governance command is unsupported."),
        };
        return JsonSerializer.SerializeToUtf8Bytes(command, command.GetType(), EventStorePayloadSerialization.Options);
    }

    private static CreateProviderModelEntry NormalizeCreate(CreateProviderModelEntry command)
        => command with { DataHandling = NormalizeTerms(command.DataHandling) };

    private static UpdateProviderModelEntry NormalizeUpdate(UpdateProviderModelEntry command)
        => command with { DataHandling = NormalizeTerms(command.DataHandling) };

    private static ProviderDataHandlingRecord? NormalizeTerms(ProviderDataHandlingRecord? terms)
        => terms is null ? null : terms with
        {
            EffectiveAt = null,
            TighteningDeclaration = terms.TighteningDeclaration is { } declaration
                ? declaration with { DeclaredAt = default }
                : null,
        };

    /// <summary>Binds governance commands with the same unambiguous casing rules at every policy entry point.</summary>
    internal static T Read<T>(byte[] payload)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("The governance command payload must be an object.");
            }

            VerifyUniqueProperties(document.RootElement);
            if (typeof(T) == typeof(SetTenantProviderModelEnablement)
                && document.RootElement.EnumerateObject()
                    .FirstOrDefault(property => string.Equals(property.Name, "Enabled", StringComparison.OrdinalIgnoreCase))
                    .Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                throw new ArgumentException("Tenant provider enablement requires an explicit boolean Enabled property.", nameof(payload));
            }
            return JsonSerializer.Deserialize<T>(payload, EventStorePayloadSerialization.Options)
                ?? throw new ArgumentException("The governance command payload is invalid.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The governance command payload is invalid.", nameof(payload), exception);
        }
    }

    private static void VerifyUniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new ArgumentException("The governance command payload has ambiguous properties.");
                }

                VerifyUniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                VerifyUniqueProperties(item);
            }
        }
    }
}
