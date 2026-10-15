using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Unit-only materialization for tests that explicitly run a pure aggregate after capturing an intent.</summary>
internal static class FixtureCommandIntentExtensions
{
    /// <summary>Creates a target envelope in a unit fixture; it proves no signature, replay admission or live authority.</summary>
    internal static CommandEnvelope ToFixtureEnvelope(this AgentCommandIntent intent)
        => new(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId, intent.CommandType,
            intent.Payload, intent.CorrelationId, intent.CausationId, intent.UserId, intent.Extensions);

    /// <summary>Copies a captured unit intent to simulate a different logical writer.</summary>
    internal static AgentCommandIntent WithFixtureMessageId(this AgentCommandIntent intent, string messageId)
        => new(messageId, intent.TenantId, intent.Domain, intent.AggregateId, intent.CommandType,
            intent.Payload, intent.CorrelationId, intent.CausationId, intent.UserId, intent.Extensions);
}
