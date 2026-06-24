using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the invocation gate orchestration uses to read the caller's access to the Source Conversation
/// (Story 2.2; AC1, AC2, AC3; AD-3, AD-6, AD-12). It is the Agents-owned wrapper over the Conversations <b>public</b>
/// authorized read seam <c>IConversationClient.GetConversationAsync(...)</c>: it checks (a) the caller participates
/// with sufficient role and (b) the conversation loaded fresh enough, then maps that to a safe
/// <see cref="ConversationAccessReadResult"/> — never surfacing Conversation content, participant PII, or a stream name
/// (AD-6, AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port preserves the AD-3 round-trip: the pure aggregate receives the verdict through the trusted gate
/// command, never by calling Conversations itself. The live <c>IConversationClient</c> binding is deferred to Story 2.3
/// (which wires the live context read); the deferred reader fails closed by returning
/// <see cref="ConversationAccessReadResult.Unavailable"/>. This story never invokes a live <c>GetConversationAsync</c>.
/// </remarks>
public interface IConversationAccessReader
{
    /// <summary>Reads the caller's Source Conversation access and maps it to a fail-closed gate result (AC1, AC2, AC3).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed, never revealing existence — AC3).</param>
    /// <param name="sourceConversationId">The source Conversation reference (opaque on the Agents side — AD-6).</param>
    /// <param name="callerPartyId">The caller's stable Party reference whose participation/role is checked (AD-7).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe conversation-access read result (outcome + freshness).</returns>
    Task<ConversationAccessReadResult> ReadAsync(string tenantId, string sourceConversationId, string callerPartyId, CancellationToken ct);
}
