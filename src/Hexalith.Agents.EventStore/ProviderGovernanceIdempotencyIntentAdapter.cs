using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.EventStore;

/// <summary>Canonicalizes one governed catalog or tenant command for EventStore admission.</summary>
internal sealed class ProviderGovernanceIdempotencyIntentAdapter<TCommand> : IIdempotencyIntentAdapter
    where TCommand : class
{
    private static readonly bool _platform = typeof(TCommand) == typeof(CreateProviderModelEntry)
        || typeof(TCommand) == typeof(UpdateProviderModelEntry)
        || typeof(TCommand) == typeof(EnableProviderModelEntry)
        || typeof(TCommand) == typeof(DisableProviderModelEntry);

    public string CommandType => typeof(TCommand).Name;

    public string AdapterId => $"Hexalith.Agents.EventStore.{CommandType}.v1";

    public string OperationId => _platform ? $"agents.catalog.{CommandType}" : $"agents.tenant-catalog.{CommandType}";

    public int DescriptorVersion => 1;

    public IdempotencyReplayRetentionTier RetentionTier => IdempotencyReplayRetentionTier.Mutation;

    public IdempotencyCanonicalIntent CreateIntent(IdempotencyIntentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string domain = _platform ? "provider-catalog" : "tenant-provider-enablement";
        if (!string.Equals(command.Domain, domain, StringComparison.Ordinal)
            || !string.Equals(command.CommandType, CommandType, StringComparison.Ordinal))
        {
            throw new ArgumentException("The governance command does not match its intent adapter.", nameof(command));
        }

        string authorityKey = _platform ? "actor:agentsProviderAdmin"
            : typeof(TCommand) == typeof(DecideProviderDataHandling)
                ? "actor:tenantAgentAdministrator" : "actor:platformOperator";
        var semanticOptions = new Dictionary<string, string>(StringComparer.Ordinal);
        if (command.Extensions?.TryGetValue(authorityKey, out string? value) == true)
        {
            semanticOptions[authorityKey] = value;
        }

        return new IdempotencyCanonicalIntent(
            JsonSerializer.Serialize(new[] { command.Tenant, command.Domain, command.AggregateId }),
            ProviderGovernanceCommandIntent.Normalize(CommandType, command.Payload),
            semanticOptions, PolicyVersion: "1", DelegatedTaskScope: null, CredentialScope: null);
    }
}
