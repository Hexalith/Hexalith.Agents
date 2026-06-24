using System;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Pure, deterministic derivation of the attempt id and regenerated-version id for a Proposed-Agent-Reply regeneration
/// (Story 3.4; AD-13). Regenerating the same proposal with the same regeneration attempt — same
/// <c>(interaction, source conversation, regeneration attempt)</c> triple — yields the same ids, so a retried regenerate
/// command reuses the same identity and the aggregate's terminal no-op dedupes it, never appending a duplicate regenerated
/// version (AC2); a distinct second regeneration (a different regeneration-attempt seed) derives different ids and appends
/// another version. Lives here in the Server orchestration, never in the pure aggregate (AD-3). Mirrors
/// <see cref="AgentProposalEditIdentity"/> and <see cref="AgentInteractionIdentity"/>.
/// </summary>
/// <remarks>
/// Each id is the lowercase hex of a SHA-256 digest of the length-prefixed components plus a <b>distinct</b> per-purpose tag
/// (<c>proposal-regeneration-attempt-id</c> / <c>proposal-regeneration-version-id</c>) so the regenerated version id never
/// collides with the attempt id, the proposal id (<c>proposal-id</c>), the edited version id (<c>proposal-edit-version-id</c>),
/// the posting message id/idempotency key, or a Story 2.4 generated version id (<c>attempt-…</c>/<c>version-…</c>): length
/// framing makes the composite unambiguous, and hashing neutralizes any characters in the opaque values that would be illegal
/// in an id. A 64-char lowercase-hex digest is non-empty, colon-free, and deterministic. ULIDs/<c>Guid.NewGuid</c> are NOT
/// used here — they are non-deterministic and would defeat idempotency (AD-13).
/// </remarks>
internal static class AgentProposalRegenerationIdentity
{
    private const char UnitSeparator = '';
    private const string AttemptIdTag = "proposal-regeneration-attempt-id";
    private const string VersionIdTag = "proposal-regeneration-version-id";

    /// <summary>Derives the deterministic regeneration attempt id from the interaction + source conversation + regeneration attempt seed (AD-13).</summary>
    /// <param name="agentInteractionId">The Agent Call interaction id (the aggregate id).</param>
    /// <param name="sourceConversationId">The snapshot-recorded source Conversation reference the regeneration re-reads.</param>
    /// <param name="regenerationAttemptId">The deterministic regeneration attempt seed carried on the request so retries do not fork.</param>
    /// <returns>A deterministic, non-empty, colon-free regeneration attempt id (64 lowercase hex chars).</returns>
    internal static string DeriveAttemptId(string agentInteractionId, string sourceConversationId, string regenerationAttemptId)
        => Derive(AttemptIdTag, agentInteractionId, sourceConversationId, regenerationAttemptId);

    /// <summary>Derives the deterministic regenerated-version id from the interaction + source conversation + regeneration attempt seed (AD-13).</summary>
    /// <param name="agentInteractionId">The Agent Call interaction id (the aggregate id).</param>
    /// <param name="sourceConversationId">The snapshot-recorded source Conversation reference the regeneration re-reads.</param>
    /// <param name="regenerationAttemptId">The deterministic regeneration attempt seed carried on the request so retries do not fork.</param>
    /// <returns>A deterministic, non-empty, colon-free regenerated version id (64 lowercase hex chars), distinct from the attempt id.</returns>
    internal static string DeriveVersionId(string agentInteractionId, string sourceConversationId, string regenerationAttemptId)
        => Derive(VersionIdTag, agentInteractionId, sourceConversationId, regenerationAttemptId);

    private static string Derive(string purposeTag, string agentInteractionId, string sourceConversationId, string regenerationAttemptId)
    {
        var builder = new StringBuilder();
        AppendComponent(builder, purposeTag);
        AppendComponent(builder, agentInteractionId);
        AppendComponent(builder, sourceConversationId);
        AppendComponent(builder, regenerationAttemptId);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    private static void AppendComponent(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        builder.Append(value.Length).Append(':').Append(value).Append(UnitSeparator);
    }
}
