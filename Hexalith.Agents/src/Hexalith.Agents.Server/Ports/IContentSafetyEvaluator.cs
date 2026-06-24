using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the generation orchestration uses to evaluate generated content against the effective Content
/// Safety Policy BEFORE any downstream artifact (Story 2.4; AC2, AC3; FR-26, FR-27; AD-14). This story wires the
/// enforcement SHAPE — block-before-side-effect, fail-closed, metadata-only audit — not a concrete filter engine; the
/// concrete taxonomy/filter is deferred (PRD OQ-9), so the default DI graph binds the fail-closed
/// <see cref="DeferredContentSafetyEvaluator"/>.
/// </summary>
/// <remarks>
/// The evaluator returns only a safe verdict (<see cref="ContentSafetyEvaluationResult"/>) — never the evaluated content
/// or a raw engine payload (AD-14). The generated content on the request is held in memory only for the evaluation.
/// Mirrors how Story 2.3 wired the bounded-context shape without a concrete bounded behavior.
/// </remarks>
public interface IContentSafetyEvaluator
{
    /// <summary>Evaluates generated content against the effective policy and returns a safe pass/block verdict (AC2, AC3).</summary>
    /// <param name="request">The evaluation request (in-memory generated content + the policy's constraints/categories/handling).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe verdict (passed/blocked + optional safe category label).</returns>
    Task<ContentSafetyEvaluationResult> EvaluateAsync(ContentSafetyEvaluationRequest request, CancellationToken ct);
}
