using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the context orchestration uses to read the authorized Source Conversation content for context
/// building (Story 2.3; AC1, AC2, AC3; AD-3, AD-6, AD-12). It is the Agents-owned wrapper over the Conversations
/// <b>public</b> authorized read seam <c>IConversationClient.GetConversationAsync(...)</c>: it returns the loaded
/// visible timeline content needed for token measurement plus the load classification and freshness, mapped to a safe
/// <see cref="ConversationContextReadResult"/>.
/// </summary>
/// <remarks>
/// This is the <em>content</em> read that Story 2.2's <c>IConversationAccessReader</c> deferred to this story; it is
/// distinct from that gate access reader (which returns only an access outcome + freshness). Keeping this a port
/// preserves the AD-3 round-trip: the pure aggregate receives the measurement through the trusted context command,
/// never by calling Conversations itself. The live binding (<c>ConversationClientContextReader</c>) is registered only
/// behind a <c>Conversations</c> config section; the deferred reader fails closed by returning
/// <see cref="ConversationContextReadResult.Unavailable"/>. The caller identity carries both a security principal
/// (<paramref name="callerPrincipalId"/>, mapped to the Conversations authorized-read principal) and the
/// <paramref name="callerPartyId"/> (available for participation cross-checks).
/// </remarks>
public interface IConversationContextReader
{
    /// <summary>Reads the authorized Source Conversation content and maps it to a fail-closed read result (AC1, AC2, AC3).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed, never revealing existence — AC1).</param>
    /// <param name="sourceConversationId">The source Conversation reference (opaque on the Agents side — AD-6).</param>
    /// <param name="callerPartyId">The caller's stable Party reference (available for participation cross-checks; AD-7).</param>
    /// <param name="callerPrincipalId">The caller security principal used for the Conversations authorized read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe conversation-context read result (load outcome + loaded messages + freshness).</returns>
    Task<ConversationContextReadResult> ReadAsync(string tenantId, string sourceConversationId, string callerPartyId, string callerPrincipalId, CancellationToken ct);
}
