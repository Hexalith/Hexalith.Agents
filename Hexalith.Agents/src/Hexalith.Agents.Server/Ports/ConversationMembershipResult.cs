namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The fail-closed classification of the Agent <c>AiAgent</c> membership ensure in the Source Conversation (Story 2.5;
/// AC1; AD-6, AD-7, AD-12). Only <see cref="Present"/>/<see cref="Established"/> permit the append; every other value
/// fails closed.
/// </summary>
/// <remarks>
/// <see cref="SeamUnavailable"/> is the fail-closed value returned when the Agent is NOT already a participant and there
/// is no public Conversations membership-establish seam to add it (the verified current reality — there is no
/// <c>AddParticipantAsync</c> on <c>IConversationClient</c>). It maps to <c>MembershipUnavailable</c> upstream. This
/// satisfies AD-7 "fails closed if the Conversations membership seam is missing/unavailable" by construction.
/// </remarks>
public enum ConversationMembershipOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a fail-closed membership failure.</summary>
    Unknown = 0,

    /// <summary>The Agent is already present as an <c>AiAgent</c> participant — proceed to append.</summary>
    Present,

    /// <summary>The Agent was established as an <c>AiAgent</c> participant via a public membership seam — proceed to append. (Not reachable today; reserved for when the seam exists — AD-6 / Story 2.0a.)</summary>
    Established,

    /// <summary>Conversations rejected the Agent's <c>AiAgent</c> participation — fail closed.</summary>
    MembershipRejected,

    /// <summary>The Source Conversation is missing, unauthorized, or stale — fail closed.</summary>
    ConversationUnavailable,

    /// <summary>The Agent is not a participant and no public membership-establish seam exists — fail closed (AD-6, AD-7).</summary>
    SeamUnavailable,
}

/// <summary>
/// Server-internal result of the Agent <c>AiAgent</c> membership ensure (Story 2.5; AC1). Safe classification only.
/// </summary>
/// <param name="Outcome">The fail-closed membership classification.</param>
public sealed record ConversationMembershipResult(ConversationMembershipOutcome Outcome)
{
    /// <summary>Gets the fail-closed seam-unavailable result (the deferred default; no public establish seam).</summary>
    public static ConversationMembershipResult SeamUnavailable { get; } = new(ConversationMembershipOutcome.SeamUnavailable);

    /// <summary>Gets the fail-closed conversation-unavailable result.</summary>
    public static ConversationMembershipResult ConversationUnavailable { get; } = new(ConversationMembershipOutcome.ConversationUnavailable);
}
