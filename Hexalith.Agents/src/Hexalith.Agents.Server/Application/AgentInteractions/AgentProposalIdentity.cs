using System;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Pure, deterministic derivation of the Proposed-Agent-Reply <c>ProposalId</c> for Confirmation-mode creation (AD-13).
/// Re-creating the same proposal — same <c>(interaction, selected version)</c> pair — yields the same <c>ProposalId</c>, so
/// the aggregate's terminal no-op (and a future read-model dedupe) make a retry a safe no-op that never creates a duplicate
/// proposal/version (AC4). Lives here in the Server orchestration, never in the pure aggregate (AD-3). Mirrors
/// <see cref="AgentResponsePostingIdentity"/>.
/// </summary>
/// <remarks>
/// The id is the lowercase hex of a SHA-256 digest of the length-prefixed components (plus a per-purpose tag so it never
/// collides with the posting message id / idempotency key for the same pair): length framing makes the composite
/// unambiguous, and hashing neutralizes any characters in the opaque values that would be illegal in an id. A 64-char
/// lowercase-hex digest is non-empty, colon-free, and deterministic. <c>ExpiresAt</c> is intentionally EXCLUDED from the
/// identity (only interaction + version derive the id), so a differing expiry across retries cannot fork the proposal.
/// ULIDs/<c>Guid.NewGuid</c> are NOT used here — they are non-deterministic and would defeat idempotency (AD-13).
/// </remarks>
internal static class AgentProposalIdentity
{
    private const char UnitSeparator = '\u001F';
    private const string ProposalIdTag = "proposal-id";

    /// <summary>Derives the deterministic proposal id from the interaction + selected version (AD-13).</summary>
    /// <param name="agentInteractionId">The Agent Call interaction id (the aggregate id).</param>
    /// <param name="versionId">The selected generated version id held in the proposal.</param>
    /// <returns>A deterministic, non-empty, colon-free proposal id (64 lowercase hex chars).</returns>
    internal static string DeriveProposalId(string agentInteractionId, string versionId)
        => Derive(ProposalIdTag, agentInteractionId, versionId);

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
