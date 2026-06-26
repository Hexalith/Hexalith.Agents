namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The fail-closed classification of the Agent message append to the Source Conversation (Story 2.5; AC2, AC3; AD-12,
/// AD-14). Only <see cref="Posted"/> is the success outcome; every other value fails closed.
/// </summary>
public enum ConversationAppendOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a fail-closed append failure.</summary>
    Unknown = 0,

    /// <summary>The message was appended (or deduped on the deterministic id) — success.</summary>
    Posted,

    /// <summary>Conversations rejected the append — fail closed.</summary>
    PostRejected,

    /// <summary>The Source Conversation is missing, unauthorized, or stale — fail closed.</summary>
    ConversationUnavailable,

    /// <summary>The append adapter failed (it threw or returned a fail-closed adapter outcome) — fail closed; no error text exposed (AD-14).</summary>
    AdapterFailure,
}

/// <summary>
/// Server-internal result of the Agent message append (Story 2.5; AC2, AC3). Safe classification only.
/// </summary>
/// <param name="Outcome">The fail-closed append classification.</param>
public sealed record ConversationAppendResult(ConversationAppendOutcome Outcome)
{
    /// <summary>Gets the fail-closed conversation-unavailable result (the deferred default).</summary>
    public static ConversationAppendResult ConversationUnavailable { get; } = new(ConversationAppendOutcome.ConversationUnavailable);

    /// <summary>Gets the fail-closed adapter-failure result.</summary>
    public static ConversationAppendResult AdapterFailure { get; } = new(ConversationAppendOutcome.AdapterFailure);
}
