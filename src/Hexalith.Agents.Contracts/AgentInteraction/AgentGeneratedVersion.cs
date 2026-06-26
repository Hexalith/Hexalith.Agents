namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// One durably-recorded generated version of an Agent Call's output (AC2, AC4; AD-5 append-only version history; FR-19,
/// FR-20). It is created ONLY on a successful, safety-passing generation and is the single approvable/postable unit the
/// response-mode branch (Story 2.5 automatic post / Story 3.1 proposal) consumes. On any failure no version is emitted,
/// so there is structurally nothing a later story could approve or post (AD-5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitive content (AD-14):</b> <see cref="GeneratedContent"/> is conversation-derived content of the SAME class as
/// the caller <c>Prompt</c>. It lives ONLY on the durable success events (<see cref="Events.AgentOutputGenerated"/> for a
/// generated version, <see cref="Events.ProposedAgentReplyEdited"/> for an edited version) and the aggregate state — it
/// must NEVER appear on a command (other than the legitimate content-bearing write-path edit command/result), view,
/// evidence, rejection, failure event, log, telemetry, or audit summary. Every other member is a safe scalar/reference:
/// provider/model identity, capability/policy versions, token usage counts, and the edited-version provenance ids (AD-9, AD-14).
/// </para>
/// <para>
/// <see cref="VersionId"/> is derived deterministically from <see cref="AttemptId"/> so a retried generation reuses the
/// same identity and never produces a duplicate version (AD-13). <see cref="ContentSafetyPolicyVersion"/> records the
/// policy version the content passed, for audit.
/// </para>
/// </remarks>
/// <param name="VersionId">The deterministic version identifier (derived from <see cref="AttemptId"/>; AD-13).</param>
/// <param name="AttemptId">The deterministic generation attempt identifier reused across retries (AD-13).</param>
/// <param name="Kind">How this version was produced (V1 = <see cref="AgentGenerationKind.Generated"/>).</param>
/// <param name="GeneratedContent">The generated content — sensitive conversation-derived content; durable here only, never surfaced (AD-14).</param>
/// <param name="ProviderId">The safe provider identifier the content was generated with (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the content was generated with (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the generation.</param>
/// <param name="ContentSafetyPolicyVersion">The Content Safety Policy version the generated content passed.</param>
/// <param name="PromptTokenCount">The prompt/input token usage (a safe count).</param>
/// <param name="OutputTokenCount">The generated-output token usage (a safe count).</param>
/// <param name="SourceVersionId">For an <see cref="AgentGenerationKind.Edited"/> version, the id of the version it was edited from (its provenance); <see langword="null"/> for a first-pass <see cref="AgentGenerationKind.Generated"/> version (Story 3.3; AC1; AD-5, AD-17).</param>
/// <param name="EditorPartyId">For an <see cref="AgentGenerationKind.Edited"/> version, the authoring Approver's stable Party reference (a reference, not PII — AD-7); <see langword="null"/> for a <see cref="AgentGenerationKind.Generated"/> version (Story 3.3; AC1).</param>
public record AgentGeneratedVersion(
    string VersionId,
    string AttemptId,
    AgentGenerationKind Kind,
    string GeneratedContent,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    int PromptTokenCount,
    int OutputTokenCount,
    string? SourceVersionId = null,
    string? EditorPartyId = null);
