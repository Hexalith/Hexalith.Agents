namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Server.Application.AgentInteractions;

using Shouldly;

/// <summary>
/// Tests for the deterministic <see cref="AgentProposalEditIdentity"/> derivation (Story 3.3; AC1, AC4; AD-13). An edited
/// version id derived from the same <c>(interaction, source version, edit attempt)</c> triple must be identical across calls
/// (so a retried edit reuses the same id and the aggregate's terminal no-op dedupes it), distinct for different inputs, and
/// distinct from the proposal id / posting ids for the same pair (the purpose tag differs so the id families never collide).
/// </summary>
public sealed class AgentProposalEditIdentityTests
{
    private const string InteractionId = "interaction-001";
    private const string SourceVersionId = "version-attempt-1";
    private const string EditAttemptId = "edit-attempt-1";

    [Fact]
    public void Derive_edited_version_id_is_deterministic_for_the_same_inputs()
    {
        string first = AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId);
        string second = AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId);

        second.ShouldBe(first);
    }

    [Fact]
    public void Derive_edited_version_id_differs_for_different_edit_attempts()
        => AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId)
            .ShouldNotBe(AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, "edit-attempt-2"));

    [Fact]
    public void Derive_edited_version_id_differs_for_different_source_versions()
        => AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId)
            .ShouldNotBe(AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, "version-attempt-2", EditAttemptId));

    [Fact]
    public void Derive_edited_version_id_differs_for_different_interactions()
        => AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId)
            .ShouldNotBe(AgentProposalEditIdentity.DeriveEditedVersionId("interaction-002", SourceVersionId, EditAttemptId));

    [Fact]
    public void Derive_edited_version_id_is_distinct_from_the_proposal_id_for_the_same_pair()
    {
        // Different per-purpose tags ("proposal-edit-version-id" vs "proposal-id") so the edited version id can never
        // collide with the proposal id derived from the same (interaction, version) pair (AD-13).
        string editedVersionId = AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId);
        editedVersionId.ShouldNotBe(AgentProposalIdentity.DeriveProposalId(InteractionId, SourceVersionId));
    }

    [Fact]
    public void Derive_edited_version_id_is_non_empty_colon_free_lowercase_hex()
    {
        string editedVersionId = AgentProposalEditIdentity.DeriveEditedVersionId(InteractionId, SourceVersionId, EditAttemptId);

        editedVersionId.Length.ShouldBe(64); // SHA-256 lowercase hex
        editedVersionId.ShouldNotContain(":");
        editedVersionId.ShouldBe(editedVersionId.ToLowerInvariant());
    }
}
