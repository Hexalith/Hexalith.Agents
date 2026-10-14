namespace Hexalith.Agents.Contracts.TrustedEnvelopeReplay;

/// <summary>Original immutable reserved-system replay admission; exact retry cannot renew its times.</summary>
/// <param name="Registration">Exact admitted original identity and timing.</param>
public sealed record TrustedEnvelopeFirstSeen(RegisterTrustedEnvelopeFirstSeen Registration) : IEventPayload;
