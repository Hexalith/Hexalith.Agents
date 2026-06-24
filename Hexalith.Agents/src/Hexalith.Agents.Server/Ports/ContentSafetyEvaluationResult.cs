namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The safe verdict of a content-safety evaluation (Story 2.4; AC2, AC3; AD-14). A coarse pass/block classification with
/// an optional safe category label — never the evaluated content or a raw engine payload.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized verdict is treated as a block
/// (fail closed). The evaluator never returns the content it inspected.
/// </remarks>
public enum ContentSafetyVerdict
{
    /// <summary>Absent/unrecognized verdict sentinel — treated as a block (fail closed).</summary>
    Unknown = 0,

    /// <summary>The generated content passed the effective Content Safety Policy.</summary>
    Passed,

    /// <summary>The generated content was blocked by the effective Content Safety Policy.</summary>
    Blocked,
}

/// <summary>
/// Server-internal result of a content-safety evaluation (Story 2.4; AC2, AC3; AD-14). It carries ONLY the safe
/// <see cref="Verdict"/> and an optional safe <see cref="BlockedCategoryLabel"/> — never the evaluated content (AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type. <see cref="Blocked"/> is the fail-closed default the
/// deferred evaluator returns (no concrete filter engine is bound — PRD OQ-9), so generation fails closed to
/// <c>ContentSafetyBlocked</c> rather than passing unevaluated content downstream.
/// </remarks>
/// <param name="Verdict">The safe pass/block verdict.</param>
/// <param name="BlockedCategoryLabel">An optional safe category label for a block (never content), or <see langword="null"/>.</param>
public sealed record ContentSafetyEvaluationResult(
    ContentSafetyVerdict Verdict,
    string? BlockedCategoryLabel)
{
    /// <summary>Gets the fail-closed blocked verdict (the deferred default; no category label).</summary>
    public static ContentSafetyEvaluationResult Blocked { get; } = new(ContentSafetyVerdict.Blocked, null);
}
