namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, display-ready summary of ONE durably-recorded version in a Proposed Agent Reply's append-only history, for the
/// Story 3.7 version-history surface (AC2; AD-5, AD-14). It exposes ONLY safe references — the version id, how the
/// version was produced (<see cref="Kind"/>), its provenance/author ids, the safe provider/model identity, the optional
/// ISO-8601 creation timestamp (from EventStore event metadata), and coarse approval/posting markers — and deliberately
/// <b>NEVER</b> the generated/edited <c>GeneratedContent</c>, a raw provider/Conversations payload, an EventStore stream
/// name, a stack trace, or a secret (AD-14). It is the safe projection of <see cref="AgentGeneratedVersion"/> with its
/// single content-bearing member dropped: the generated content's sole durable home stays the Story 2.4
/// <c>AgentOutputGenerated</c> / Story 3.3 <c>ProposedAgentReplyEdited</c> success events, read for display only through
/// the authorized durable version reader — never through this content-free projection.
/// </summary>
/// <remarks>
/// Prior versions remain listed after every transition (edit, regeneration, approval, rejection, abandonment, expiry)
/// because the history is append-only (AD-5). The <see cref="IsApproved"/>/<see cref="IsPosted"/> markers identify
/// exactly which version was approved/posted so the surface never implies the whole proposal posted when only one
/// frozen version did. All member names are kept clear of the forbidden secret tokens
/// (<c>Secret</c>/<c>ApiKey</c>/<c>Credential</c>/<c>Password</c>/<c>ConnectionString</c>) and of the content-bearing
/// member names the read-view guard forbids.
/// </remarks>
/// <param name="VersionId">The deterministic version identifier (no content; AD-13, AD-14).</param>
/// <param name="Kind">How this version was produced — generated, edited, or regenerated (AD-5).</param>
/// <param name="ProviderId">The safe provider identifier the version was produced with (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the version was produced with (a reference, not a secret — AD-9).</param>
/// <param name="SourceVersionId">For an edited/regenerated version, the id of the version it derives from (its provenance); <see langword="null"/> for a first-pass generated version.</param>
/// <param name="EditorPartyId">For an edited version, the authoring Approver's stable Party reference (a reference, not PII — AD-7); <see langword="null"/> for a generated/regenerated version.</param>
/// <param name="CreatedAt">The optional ISO-8601 creation timestamp sourced from EventStore event metadata (<see langword="null"/> when unavailable; AD-3).</param>
/// <param name="IsApproved">Whether this exact version is the approved one (AD-5 — approval selects exactly one version).</param>
/// <param name="IsPosted">Whether this exact version was posted as a Conversation Message (AD-5).</param>
public record ProposalVersionSummary(
    string VersionId,
    AgentGenerationKind Kind,
    string ProviderId,
    string ModelId,
    string? SourceVersionId,
    string? EditorPartyId,
    string? CreatedAt,
    bool IsApproved,
    bool IsPosted);
