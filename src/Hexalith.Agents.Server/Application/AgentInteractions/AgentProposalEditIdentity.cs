using System;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Pure, deterministic derivation of the edited-version id for a Proposed-Agent-Reply edit (Story 3.3; AD-13). Editing
/// the same proposal version with the same edit attempt — same <c>(interaction, source version, edit attempt)</c> triple —
/// yields the same edited <c>VersionId</c>, so a retried edit command reuses the same identity and the aggregate's
/// terminal no-op dedupes it, never appending a duplicate edited version (AC4). Lives here in the Server orchestration,
/// never in the pure aggregate (AD-3). Mirrors <see cref="AgentProposalIdentity"/>.
/// </summary>
/// <remarks>
/// The id is the lowercase hex of a SHA-256 digest of the length-prefixed components plus a <b>distinct</b> per-purpose
/// tag (<c>proposal-edit-version-id</c>) so the edited version id never collides with the proposal id
/// (<c>proposal-id</c>), the posting message id/idempotency key, or a generated version id derived from the same pair:
/// length framing makes the composite unambiguous, and hashing neutralizes any characters in the opaque values that
/// would be illegal in an id. A 64-char lowercase-hex digest is non-empty, colon-free, and deterministic. ULIDs/
/// <c>Guid.NewGuid</c> are NOT used here — they are non-deterministic and would defeat idempotency (AD-13).
/// </remarks>
internal static class AgentProposalEditIdentity
{
    private const char UnitSeparator = '\u001F';
    private const string EditedVersionIdTag = "proposal-edit-version-id";

    /// <summary>Derives the deterministic edited version id from the interaction + source version + edit attempt (AD-13).</summary>
    /// <param name="agentInteractionId">The Agent Call interaction id (the aggregate id).</param>
    /// <param name="sourceVersionId">The id of the version being edited from.</param>
    /// <param name="editAttemptId">The deterministic edit attempt id carried on the request so retries do not fork.</param>
    /// <returns>A deterministic, non-empty, colon-free edited version id (64 lowercase hex chars).</returns>
    internal static string DeriveEditedVersionId(string agentInteractionId, string sourceVersionId, string editAttemptId)
    {
        var builder = new StringBuilder();
        AppendComponent(builder, EditedVersionIdTag);
        AppendComponent(builder, agentInteractionId);
        AppendComponent(builder, sourceVersionId);
        AppendComponent(builder, editAttemptId);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    private static void AppendComponent(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        builder.Append(value.Length).Append(':').Append(value).Append(UnitSeparator);
    }
}
