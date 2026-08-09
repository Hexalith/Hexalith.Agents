namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The truth stage an Agent setup observation has reached (Story 5.2 AC2). A caller must never collapse these
/// stages: only <see cref="ProjectionConfirmed"/> is evidence that the durable read model reflects the change.
/// </summary>
/// <remarks>
/// The stages are deliberately ordered by strength of evidence. <see cref="Submitted"/> means the command was
/// accepted by the gateway and nothing more; <see cref="AuthoritativePending"/> means the accepted change is not
/// yet visible in the projected read model; <see cref="ProjectionConfirmed"/> means the projected read model has
/// caught up with the accepted configuration version. None of the stages says anything about callability —
/// lifecycle <c>Active</c> is a lifecycle flag, not a callability claim.
/// </remarks>
public enum AgentSetupTruthState
{
    /// <summary>No truth stage has been established (the fail-closed default).</summary>
    Unknown = 0,

    /// <summary>The command was accepted for processing; no durable evidence exists yet.</summary>
    Submitted = 1,

    /// <summary>The Agent stream holds the accepted change, but the projected read model has not caught up.</summary>
    AuthoritativePending = 2,

    /// <summary>The projected read model reflects the accepted configuration version.</summary>
    ProjectionConfirmed = 3,
}
