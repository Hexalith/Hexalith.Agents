namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// The canonical UX call-status of one Agent Call (<c>hexa</c>) — the display taxonomy of UX-DR27 plus the posting
/// tail of UX-DR28/Story-2.6 AC (AC1, AC2). It is derived from the safe durable
/// <see cref="Contracts.AgentInteraction.AgentInteractionStatus"/> via
/// <see cref="AgentCallStatusPresentation.MapStatus(Contracts.AgentInteraction.AgentInteractionStatus)"/>, with three
/// UI-only transient states layered on top while a milestone is pending.
/// </summary>
/// <remarks>
/// <see cref="ContextLoading"/>, <see cref="Generating"/>, and <see cref="PostingPending"/> are <b>UI-only transient</b>
/// states with no durable contract field — exactly like <see cref="AgentReadinessState.Checking"/>. They are derived
/// from an in-flight hint while the next durable milestone is pending and are <b>never persisted</b> (Story 2.5 "Why No
/// Persisted Generating/PostingPending State"; AD-3). Only <see cref="Posted"/> is rendered/announced/coloured as a
/// posted Conversation Message (AD-6; UX-DR22) — an in-flight/<see cref="Generated"/> state is never worded as posted.
/// </remarks>
public enum AgentCallStatus
{
    /// <summary>The Agent Call request record was created (durable <c>Requested</c>; Story 2.1).</summary>
    Requested,

    /// <summary>The invocation gate passed (durable <c>Authorized</c>; Story 2.2) — context building is next.</summary>
    Authorized,

    /// <summary>An authorization-class gate check failed — the caller is not permitted (durable <c>Denied</c>; Story 2.2).</summary>
    Denied,

    /// <summary>A dependency-readiness gate check failed — required state is missing/stale/unavailable (durable <c>Blocked</c>; Story 2.2).</summary>
    Blocked,

    /// <summary>UI-only transient: context build is in flight after <see cref="Authorized"/> (no durable field; AD-3).</summary>
    ContextLoading,

    /// <summary>Conversation context could not be built within safe bounds (durable <c>ContextBlocked</c>; Story 2.3).</summary>
    ContextBlocked,

    /// <summary>UI-only transient: generation is in flight after the durable <c>ContextReady</c> milestone (no durable field; AD-3).</summary>
    Generating,

    /// <summary>Generation failed closed — provider/timeout/adapter/context/policy class (durable <c>GenerationFailed</c>; Story 2.4).</summary>
    GenerationFailed,

    /// <summary>Generated content was blocked by Content Safety Policy (durable <c>SafetyFailed</c>; Story 2.4).</summary>
    SafetyFailed,

    /// <summary>Generation + safety passed; in Automatic mode posting is next — never rendered as posted (durable <c>Generated</c>; Story 2.4).</summary>
    Generated,

    /// <summary>UI-only transient: automatic posting is in flight after <see cref="Generated"/> (no durable field; AD-3).</summary>
    PostingPending,

    /// <summary>The generated content was posted to the Source Conversation as a Conversation Message — terminal success (durable <c>Posted</c>; Story 2.5).</summary>
    Posted,

    /// <summary>Posting failed closed after successful generation (durable <c>PostingFailed</c>; Story 2.5).</summary>
    PostingFailed,

    /// <summary>Sentinel — an absent/unrecognized status never resolves to a concrete UX state.</summary>
    Unknown,
}
