using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.EventStore.Contracts.Security;
namespace Hexalith.Agents.Contracts.ConversationAgentState;

/// <summary>Immutable target-limited creation intent; request bytes remain in EventStore protection.</summary>
/// <param name="TenantId">The TenantId binding.</param>
/// <param name="ConversationId">The ConversationId binding.</param>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="AgentId">The AgentId binding.</param>
/// <param name="CallerPartyId">The CallerPartyId binding.</param>
/// <param name="ClientIdempotencyKey">The ClientIdempotencyKey binding.</param>
/// <param name="PermitId">The PermitId binding.</param>
/// <param name="CreationOutboxId">The CreationOutboxId binding.</param>
/// <param name="RequestDigest">The RequestDigest binding.</param>
/// <param name="Snapshot">The Snapshot binding.</param>
/// <param name="DirectoryEpoch">The DirectoryEpoch binding.</param>
/// <param name="Request">The Request binding.</param>
/// <param name="DigestKeyVersion">The DigestKeyVersion binding.</param>
public sealed record InteractionPermit(string TenantId, string ConversationId, string AgentInteractionId, string AgentId, string CallerPartyId, string ClientIdempotencyKey, string PermitId, string CreationOutboxId, string RequestDigest, AgentInteractionSnapshot Snapshot, DirectoryEpochInstallation DirectoryEpoch, ProtectedContent Request, string DigestKeyVersion);
