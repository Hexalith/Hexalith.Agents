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

    private static T Read<T>(byte[] payload)
        => JsonSerializer.Deserialize<T>(payload, EventStorePayloadSerialization.Options)
            ?? throw new InvalidOperationException("The governance command payload is invalid.");
}
