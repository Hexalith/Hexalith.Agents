using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.EventStore.Contracts.Security;
namespace Hexalith.Agents.Server.Ports;

/// <summary>Activity-local deterministic adapter result; failed or incomplete bytes are never returned.</summary>
/// <param name="Outcome">The Outcome binding.</param>
/// <param name="DependencyRecordId">The DependencyRecordId binding.</param>
/// <param name="Authority">The Authority binding.</param>
/// <param name="Output">The Output binding.</param>
/// <param name="ContentDigest">The ContentDigest binding.</param>
/// <param name="DigestKeyVersion">The DigestKeyVersion binding.</param>
/// <param name="FailureKind">The safe failure classification; output safety uses the recorded authority mapping.</param>
public sealed record AutomaticInteractionEffectResult(AutomaticInteractionOutcome Outcome, string? DependencyRecordId = null, ProviderOutputAuthority? Authority = null, ProtectedContent? Output = null, string? ContentDigest = null, string? DigestKeyVersion = null, AutomaticInteractionFailureKind FailureKind = AutomaticInteractionFailureKind.Unknown);
