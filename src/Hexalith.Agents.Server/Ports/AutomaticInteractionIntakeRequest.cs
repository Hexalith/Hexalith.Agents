using Hexalith.EventStore.Contracts.Security;
namespace Hexalith.Agents.Server.Ports;

/// <summary>Private ingress request; caller evidence is resolved by the gate and content is sealed before intake.</summary>
/// <param name="TenantId">The TenantId binding.</param>
/// <param name="AuthenticatedTenantId">The AuthenticatedTenantId binding.</param>
/// <param name="ActorId">The ActorId binding.</param>
/// <param name="CallerPartyId">The CallerPartyId binding.</param>
/// <param name="IsPerson">The IsPerson binding.</param>
/// <param name="AgentId">The AgentId binding.</param>
/// <param name="ConversationId">The ConversationId binding.</param>
/// <param name="ClientIdempotencyKey">The ClientIdempotencyKey binding.</param>
/// <param name="Request">The Request binding.</param>
/// <param name="RequestDigest">The RequestDigest binding.</param>
/// <param name="DigestKeyVersion">The DigestKeyVersion binding.</param>
public sealed record AutomaticInteractionIntakeRequest(string TenantId, string AuthenticatedTenantId, string ActorId, string CallerPartyId, bool IsPerson, string AgentId, string ConversationId, string ClientIdempotencyKey, ProtectedContent Request, string RequestDigest, string DigestKeyVersion);
