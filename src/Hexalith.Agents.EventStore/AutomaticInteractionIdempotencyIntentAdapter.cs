using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.Agents.Contracts.Security;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.EventStore;

/// <summary>Declared-contract canonical intent for the directory and automatic interaction protocol.</summary>
internal sealed class AutomaticInteractionIdempotencyIntentAdapter<TCommand> : IIdempotencyIntentAdapter where TCommand : class
{
    private static readonly string _domain = typeof(TCommand).Namespace!.Contains("ConversationAgentState", StringComparison.Ordinal) ? ConversationAgentStateIdentity.Domain : "agent-interaction";
    /// <inheritdoc />
    public string CommandType => typeof(TCommand).Name;
    /// <inheritdoc />
    public string AdapterId => $"Hexalith.Agents.EventStore.{CommandType}.v1";
    /// <inheritdoc />
    public string OperationId => $"agents.{_domain}.{CommandType}";
    /// <inheritdoc />
    public int DescriptorVersion => 1;
    /// <inheritdoc />
    public IdempotencyReplayRetentionTier RetentionTier => IdempotencyReplayRetentionTier.Mutation;
    /// <inheritdoc />
    public IdempotencyCanonicalIntent CreateIntent(IdempotencyIntentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Domain != _domain || command.CommandType != CommandType) { throw new ArgumentException("CommandIntentMismatch", nameof(command)); }
        TCommand declared = JsonSerializer.Deserialize<TCommand>(command.Payload) ?? throw new ArgumentException("InvalidCommandPayload", nameof(command));
        JsonObject normalized = JsonSerializer.SerializeToNode(declared)!.AsObject();
        normalized.Remove("ExpectedRevision");
        if (normalized["Fact"] is JsonObject fact) { fact.Remove("Revision"); }
        var semantic = new Dictionary<string, string>(StringComparer.Ordinal);
        if (command.Extensions?.TryGetValue(ConversationAgentStateIdentity.WorkflowAuthority, out string? value) == true) { semantic[ConversationAgentStateIdentity.WorkflowAuthority] = value; }
        if (command.Extensions?.TryGetValue(ConversationAgentStateIdentity.DeletionAuthority, out string? deletion) == true) { semantic[ConversationAgentStateIdentity.DeletionAuthority] = deletion; }
        return new IdempotencyCanonicalIntent(JsonSerializer.Serialize(new[] { command.Tenant, command.Domain, command.AggregateId }),
            JsonSerializer.SerializeToUtf8Bytes(normalized), semantic, PolicyVersion: "1", DelegatedTaskScope: null, CredentialScope: null);
    }
}
