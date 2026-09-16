using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.EventStore;

internal abstract class AgentSetupIdempotencyIntentAdapter(
    string commandType,
    string operationId,
    params string[] semanticExtensionKeys) : IIdempotencyIntentAdapter
{
    private const string AgentDomain = "agent";

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
            command.Payload,
            semanticOptions,
            PolicyVersion: "1",
            DelegatedTaskScope: null,
            CredentialScope: null);
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
