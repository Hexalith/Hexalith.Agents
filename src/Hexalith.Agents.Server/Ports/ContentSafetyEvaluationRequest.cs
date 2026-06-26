using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal request to the content-safety evaluator (Story 2.4; AC2, AC3; AD-14). It carries the generated content
/// to evaluate (held in memory only) plus the effective Content Safety Policy's safe governance descriptors so the
/// evaluator can apply the configured constraints/categories and failure handling.
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type. <see cref="GeneratedContent"/> is sensitive
/// conversation-derived content used ONLY transiently in memory for the evaluation; it is never persisted or logged
/// (AD-14). The category/constraint lists are safe governance labels from the resolved policy (AD-14).
/// </remarks>
/// <param name="GeneratedContent">The generated content to evaluate — sensitive, in-memory only (AD-14).</param>
/// <param name="PromptConstraints">The effective policy's safe prompt-constraint descriptors.</param>
/// <param name="BlockedOutputCategories">The effective policy's safe blocked-output category labels.</param>
/// <param name="RestrictedOutputCategories">The effective policy's safe restricted-output category labels.</param>
/// <param name="FailureHandling">The effective policy's safety failure-handling governance choice.</param>
public sealed record ContentSafetyEvaluationRequest(
    string GeneratedContent,
    IReadOnlyList<string> PromptConstraints,
    IReadOnlyList<string> BlockedOutputCategories,
    IReadOnlyList<string> RestrictedOutputCategories,
    ContentSafetyFailureHandling FailureHandling);
