using System;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Pure, deterministic derivation of the Conversation <c>MessageId</c> and idempotency key for automatic posting (AD-13).
/// Re-posting the same Agent Call response — same <c>(interaction, selected version)</c> pair — yields the same
/// <c>MessageId</c>/key, so Conversations dedupes the append and a retry is a safe no-op. Lives here in the Server
/// orchestration, never in the pure aggregate (AD-3). Mirrors <see cref="AgentInteractionIdentity"/>.
/// </summary>
/// <remarks>
/// Each id is the lowercase hex of a SHA-256 digest of the length-prefixed components (plus a per-purpose tag so the
/// message id and the idempotency key never collide): length framing makes the composite unambiguous, and hashing
/// neutralizes any characters in the opaque values that would be illegal in an id. A 64-char lowercase-hex digest is
/// non-empty (satisfying the Conversations <c>MessageId</c> validation), colon-free, and deterministic. ULIDs/
/// <c>Guid.NewGuid</c> are NOT used here — they are non-deterministic and would defeat idempotency (AD-13).
/// </remarks>
internal static class AgentResponsePostingIdentity
{
    private const char UnitSeparator = '\u001F';
    private const string MessageIdTag = "message-id";
    private const string IdempotencyKeyTag = "idempotency-key";

    /// <summary>Derives the deterministic Conversation Message id from the interaction + selected version (AD-13).</summary>
    /// <param name="agentInteractionId">The Agent Call interaction id (the aggregate id).</param>
    /// <param name="versionId">The selected generated version id being posted.</param>
    /// <returns>A deterministic, non-empty, colon-free message id (64 lowercase hex chars).</returns>
    internal static string DeriveMessageId(string agentInteractionId, string versionId)
        => Derive(MessageIdTag, agentInteractionId, versionId);

    /// <summary>Derives the deterministic posting idempotency key from the interaction + selected version (AD-13).</summary>
    /// <param name="agentInteractionId">The Agent Call interaction id (the aggregate id).</param>
    /// <param name="versionId">The selected generated version id being posted.</param>
    /// <returns>A deterministic idempotency key (64 lowercase hex chars), distinct from the message id.</returns>
    internal static string DeriveIdempotencyKey(string agentInteractionId, string versionId)
        => Derive(IdempotencyKeyTag, agentInteractionId, versionId);

    private static string Derive(string purposeTag, string agentInteractionId, string versionId)
    {
        var builder = new StringBuilder();
        AppendComponent(builder, purposeTag);
        AppendComponent(builder, agentInteractionId);
        AppendComponent(builder, versionId);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    private static void AppendComponent(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        builder.Append(value.Length).Append(':').Append(value).Append(UnitSeparator);
    }
}
