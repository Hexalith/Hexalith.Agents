namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of measuring the token count of the loaded Conversation context for a selected Provider/model
/// (Story 2.3; AC2, AC3). It carries ONLY a fail-closed availability flag and the safe measured
/// <see cref="TokenCount"/> — never the raw content that was measured (AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type. <see cref="NotAvailable"/> is the fail-closed default
/// (the deferred measurer's result, and the safe value when no tokenizer is bound), which drives the budget decision to
/// <c>ModelBudgetUnavailable</c> rather than a guessed count.
/// </remarks>
/// <param name="IsAvailable">Whether a trustworthy token count was produced.</param>
/// <param name="TokenCount">The measured token count (<c>0</c> when not available).</param>
public sealed record ConversationContextTokenMeasurement(
    bool IsAvailable,
    int TokenCount)
{
    /// <summary>Gets the fail-closed not-available measurement (no trustworthy count) — drives a model-budget-unavailable block.</summary>
    public static ConversationContextTokenMeasurement NotAvailable { get; } = new(false, 0);
}
