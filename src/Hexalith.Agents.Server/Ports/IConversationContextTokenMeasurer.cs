using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the context orchestration uses to measure the token count of the loaded Conversation context
/// for a selected Provider/model (Story 2.3; AC2, AC3; AD-3). The orchestrator measures and the pure aggregate decides
/// on the supplied count, so the budget decision stays pure and unit-testable.
/// </summary>
/// <remarks>
/// The live token-measurement binding stays deferred behind this port — no tokenizer library exists in the module and
/// the architecture keeps provider SDK "deferred, adapter-local when selected" (a later adapter story binds a concrete
/// tokenizer when a model is selected). The deferred measurer returns
/// <see cref="ConversationContextTokenMeasurement.NotAvailable"/> so the budget read fails closed to
/// <c>ModelBudgetUnavailable</c>. The supplied messages hold sensitive content used only transiently in memory (AD-14).
/// </remarks>
public interface IConversationContextTokenMeasurer
{
    /// <summary>Measures the token count of the supplied visible timeline messages for the selected Provider/model (AC2).</summary>
    /// <param name="messages">The loaded visible timeline messages (sensitive — measured in memory only; AD-14).</param>
    /// <param name="providerId">The selected stable provider identifier.</param>
    /// <param name="modelId">The selected stable model identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe token measurement (availability + measured count).</returns>
    Task<ConversationContextTokenMeasurement> MeasureAsync(IReadOnlyList<ConversationContextMessage> messages, string providerId, string modelId, CancellationToken ct);
}
