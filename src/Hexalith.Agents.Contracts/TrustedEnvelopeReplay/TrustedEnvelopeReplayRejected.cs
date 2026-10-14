namespace Hexalith.Agents.Contracts.TrustedEnvelopeReplay;

/// <summary>Safe closed rejection with no claimed tenant or nonce disclosed.</summary>
/// <param name="Reason">Closed content-free code.</param>
public sealed record TrustedEnvelopeReplayRejected(string Reason) : IRejectionEvent;
