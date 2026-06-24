using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IContentSafetyEvaluator"/> registered by default so the Server DI graph is complete and
/// compiles cleanly while the concrete content-safety filter engine/taxonomy stays deferred (PRD OQ-9). It is never
/// exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="ContentSafetyEvaluationResult.Blocked"/>: with no engine
/// bound, generated content cannot be cleared, so generation fails closed to <c>ContentSafetyBlocked</c> rather than
/// posting unevaluated content (AD-14; FR-27). It wires the enforcement SHAPE, not the engine.
/// </remarks>
public sealed class DeferredContentSafetyEvaluator : IContentSafetyEvaluator
{
    /// <inheritdoc />
    public Task<ContentSafetyEvaluationResult> EvaluateAsync(ContentSafetyEvaluationRequest request, CancellationToken ct)
        => Task.FromResult(ContentSafetyEvaluationResult.Blocked);
}
