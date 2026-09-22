using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Serialization;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.EventStore;

internal abstract class AgentSetupIdempotencyIntentAdapter(
    string commandType,
    string operationId,
    Type commandContract,
    params string[] semanticExtensionKeys) : IIdempotencyIntentAdapter
{
    private const string AgentDomain = "agent";

    private readonly Type _commandContract = commandContract;
    private readonly string[] _semanticExtensionKeys = semanticExtensionKeys;

    public string CommandType { get; } = commandType;

    public string AdapterId { get; } = $"Hexalith.Agents.EventStore.{commandType}.v1";

    public string OperationId { get; } = operationId;

    public int DescriptorVersion => 1;

    public IdempotencyReplayRetentionTier RetentionTier => IdempotencyReplayRetentionTier.Mutation;

    public IdempotencyCanonicalIntent CreateIntent(IdempotencyIntentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!string.Equals(command.CommandType, CommandType, StringComparison.Ordinal)
            || !string.Equals(command.Domain, AgentDomain, StringComparison.Ordinal))
        {
            throw new ArgumentException("The command is not supported by this Agents intent adapter.", nameof(command));
        }

        var semanticOptions = new Dictionary<string, string>(StringComparer.Ordinal);
        if (command.Extensions is not null)
        {
            foreach (string key in _semanticExtensionKeys)
            {
                if (command.Extensions.TryGetValue(key, out string? value))
                {
                    semanticOptions[key] = value;
                }
            }
        }

        return new IdempotencyCanonicalIntent(
            JsonSerializer.Serialize(new[] { command.Tenant, command.Domain, command.AggregateId }),
            NormalizeDeclaredPayload(command),
            semanticOptions,
            PolicyVersion: "1",
            DelegatedTaskScope: null,
            CredentialScope: null);
    }

    private byte[] NormalizeDeclaredPayload(IdempotencyIntentCommand command)
    {
        object? declared;
        try
        {
            declared = JsonSerializer.Deserialize(command.Payload, _commandContract, EventStorePayloadSerialization.Options);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "The command payload is not the declared Agents setup command.",
                nameof(command),
                exception);
        }

        if (declared is null || declared.GetType() != _commandContract)
        {
            throw new ArgumentException(
                "The command payload is not the declared Agents setup command.",
                nameof(command));
        }

        // Declared-contract normalization only. Key order and other JSON syntax stay with
        // CanonicalIdempotencyIntentEncoder.
        return JsonSerializer.SerializeToUtf8Bytes(declared, _commandContract, EventStorePayloadSerialization.Options);
    }

    protected static string[] StandardSemanticExtensionKeys()
        => [AgentSetupTrustedExtensions.AgentAdministrator];

    protected static string[] ActivationSemanticExtensionKeys()
        =>
        [
            AgentSetupTrustedExtensions.AgentAdministrator,
            AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion,
        ];
}
