using System;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Pure, deterministic derivation of the <c>AgentInteraction</c> aggregate id (AD-13). Re-issuing the same Agent
/// Call — same <c>(tenant, agent, source conversation, caller, idempotency key)</c> tuple — yields the same id, so
/// the aggregate deduplicates the re-issue to a no-op. Lives here in the Server orchestration, never in the pure
/// aggregate (AD-3).
/// </summary>
/// <remarks>
/// The id is the lowercase hex of a SHA-256 digest of the length-prefixed components: length framing makes the
/// composite unambiguous (two different tuples cannot collide by concatenation), and hashing neutralizes any
/// characters in the opaque caller-supplied values that would be illegal in an id. A 64-char lowercase-hex digest
/// satisfies the EventStore <c>AggregateId</c> regex <c>^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$</c>, is well
/// under 256 chars, and contains no colons (structural tenant disjointness). ULIDs/<c>Guid.NewGuid</c> are NOT used
/// here — they are non-deterministic and would defeat idempotency.
/// </remarks>
internal static class AgentInteractionIdentity
{
    private const char UnitSeparator = '\u001F';

    /// <summary>Derives the deterministic interaction id from the stable identity components (AD-13).</summary>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="agentId">The target Agent id.</param>
    /// <param name="sourceConversationId">The source Conversation reference.</param>
    /// <param name="callerPartyId">The caller Party reference.</param>
    /// <param name="idempotencyKey">The caller idempotency metadata.</param>
    /// <returns>A deterministic, regex-valid, colon-free aggregate id (64 lowercase hex chars).</returns>
    internal static string Derive(
        string tenantId,
        string agentId,
        string sourceConversationId,
        string callerPartyId,
        string idempotencyKey)
    {
        var builder = new StringBuilder();
        AppendComponent(builder, tenantId);
        AppendComponent(builder, agentId);
        AppendComponent(builder, sourceConversationId);
        AppendComponent(builder, callerPartyId);
        AppendComponent(builder, idempotencyKey);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    private static void AppendComponent(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        builder.Append(value.Length).Append(':').Append(value).Append(UnitSeparator);
    }
}
