namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Server.Application.AgentInteractions;

using Shouldly;

/// <summary>
/// Tests for the deterministic <see cref="AgentProposalRegenerationIdentity"/> derivation (Story 3.4; AC1, AC2, AC4; AD-13).
/// The attempt id and regenerated-version id derived from the same <c>(interaction, source conversation, regeneration
/// attempt)</c> triple must be identical across calls (so a retried regeneration reuses the same ids and the aggregate's
/// terminal no-op dedupes it), distinct for different inputs, distinct from EACH OTHER (the two purpose tags differ), and
/// distinct from the proposal id / edited version id for the same inputs (the id families never collide).
/// </summary>
public sealed class AgentProposalRegenerationIdentityTests
{
    private const string InteractionId = "interaction-001";
    private const string SourceConversationId = "conversation-001";
    private const string RegenerationAttemptId = "regeneration-attempt-1";

    [Fact]
    public void Derive_ids_are_deterministic_for_the_same_inputs()
    {
        AgentProposalRegenerationIdentity.DeriveAttemptId(InteractionId, SourceConversationId, RegenerationAttemptId)
            .ShouldBe(AgentProposalRegenerationIdentity.DeriveAttemptId(InteractionId, SourceConversationId, RegenerationAttemptId));
        AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId)
            .ShouldBe(AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId));
    }

    [Fact]
    public void Attempt_id_and_version_id_are_distinct_for_the_same_inputs()
        => AgentProposalRegenerationIdentity.DeriveAttemptId(InteractionId, SourceConversationId, RegenerationAttemptId)
            .ShouldNotBe(AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId));

    [Fact]
    public void Version_id_differs_for_different_regeneration_attempts()
        => AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId)
            .ShouldNotBe(AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, "regeneration-attempt-2"));

    [Fact]
    public void Version_id_differs_for_different_source_conversations()
        => AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId)
            .ShouldNotBe(AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, "conversation-002", RegenerationAttemptId));

    [Fact]
    public void Version_id_differs_for_different_interactions()
        => AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId)
            .ShouldNotBe(AgentProposalRegenerationIdentity.DeriveVersionId("interaction-002", SourceConversationId, RegenerationAttemptId));

    [Fact]
    public void Regenerated_version_id_is_distinct_from_the_edited_version_id_and_proposal_id()
    {
        // Different per-purpose tags so the regenerated version id can never collide with the edited version id or the
        // proposal id derived from related inputs (AD-13).
        string regeneratedVersionId = AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId);
        regeneratedVersionId.ShouldNotBe(AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceConversationId, RegenerationAttemptId));
        regeneratedVersionId.ShouldNotBe(AgentProposalIdentity.DeriveProposalId(InteractionId, SourceConversationId));
    }

    [Fact]
    public void Derived_ids_are_non_empty_colon_free_lowercase_hex()
    {
        foreach (string id in new[]
        {
            AgentProposalRegenerationIdentity.DeriveAttemptId(InteractionId, SourceConversationId, RegenerationAttemptId),
            AgentProposalRegenerationIdentity.DeriveVersionId(InteractionId, SourceConversationId, RegenerationAttemptId),
        })
        {
            id.Length.ShouldBe(64); // SHA-256 lowercase hex
            id.ShouldNotContain(":");
            id.ShouldBe(id.ToLowerInvariant());
        }
    }
}
