using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IApproverPolicyResolver"/> registered so the Server DI graph is complete and compiles
/// cleanly, while the live bindings to the Tenants projection, the Conversations facilitator resolver, and the
/// Parties directory are deferred to the dedicated read-model/topology story (mirroring Story 1.2/1.4/1.5 deferring
/// their read-model/dispatch bindings). It throws a clear, actionable error if it is ever invoked at runtime before
/// the real resolver is wired — it is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
public sealed class DeferredApproverPolicyResolver : IApproverPolicyResolver
{
    /// <inheritdoc />
    public Task<ApproverPolicyResolutionResult> ResolveAsync(string tenantId, AgentApproverPolicy policy, CancellationToken ct)
        => throw new NotSupportedException(
            "The live Agents approver-policy resolver is not wired yet (Story 1.6 defers the Tenants-projection / "
            + "Conversations-facilitator / Parties bindings, mirroring Story 1.2/1.4/1.5). The PredefinedParty leg can "
            + "reuse the live PartiesAgentPartyDirectory once the read-model story wires the others. Register a concrete "
            + "IApproverPolicyResolver in the read-model/topology story.");
}
