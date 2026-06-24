namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Server.Application.AgentInteractions;

using Shouldly;

/// <summary>
/// Tests for the deterministic <see cref="AgentProposalIdentity"/> derivation (Story 3.1; AC1, AC4; AD-13). A proposal id
/// derived from the same <c>(interaction, selected version)</c> pair must be identical across calls (so a retried creation
/// reuses the same id and the aggregate's terminal no-op dedupes it), distinct for different versions, and distinct from the
/// posting message id / idempotency key for the SAME pair (the purpose tag differs so the two id families never collide).
/// </summary>
public sealed class AgentProposalIdentityTests
{
    private const string InteractionId = "interaction-001";
    private const string VersionId = "version-attempt-1";

    [Fact]
    public void Derive_proposal_id_is_deterministic_for_the_same_inputs()
    {
        string first = AgentProposalIdentity.DeriveProposalId(InteractionId, VersionId);
        string second = AgentProposalIdentity.DeriveProposalId(InteractionId, VersionId);

        second.ShouldBe(first);
    }

    [Fact]
    public void Derive_proposal_id_differs_for_different_versions()
        => AgentProposalIdentity.DeriveProposalId(InteractionId, VersionId)
            .ShouldNotBe(AgentProposalIdentity.DeriveProposalId(InteractionId, "version-attempt-2"));

    [Fact]
    public void Derive_proposal_id_differs_for_different_interactions()
        => AgentProposalIdentity.DeriveProposalId(InteractionId, VersionId)
            .ShouldNotBe(AgentProposalIdentity.DeriveProposalId("interaction-002", VersionId));

    [Fact]
    public void Derive_proposal_id_is_distinct_from_the_posting_message_id_and_idempotency_key_for_the_same_pair()
    {
        // Different per-purpose tags ("proposal-id" vs "message-id"/"idempotency-key") so the proposal id can never collide
        // with the posting ids derived from the same (interaction, version) pair (AD-13).
        string proposalId = AgentProposalIdentity.DeriveProposalId(InteractionId, VersionId);
        proposalId.ShouldNotBe(AgentResponsePostingIdentity.DeriveMessageId(InteractionId, VersionId));
        proposalId.ShouldNotBe(AgentResponsePostingIdentity.DeriveIdempotencyKey(InteractionId, VersionId));
    }

    [Fact]
    public void Derive_proposal_id_is_non_empty_colon_free_lowercase_hex()
    {
        string proposalId = AgentProposalIdentity.DeriveProposalId(InteractionId, VersionId);

        proposalId.Length.ShouldBe(64); // SHA-256 lowercase hex
        proposalId.ShouldNotContain(":");
        proposalId.ShouldBe(proposalId.ToLowerInvariant());
    }
}
