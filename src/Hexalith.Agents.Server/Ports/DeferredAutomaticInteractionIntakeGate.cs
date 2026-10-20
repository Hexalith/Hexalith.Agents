using Hexalith.Agents.Contracts.Security;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Host admission and fresh actor binding are unavailable; no dependency read is attempted.</summary>
public sealed class DeferredAutomaticInteractionIntakeGate : IAutomaticInteractionIntakeGate
{
    /// <inheritdoc />
    public Task<AutomaticInteractionGateResult> EvaluateAsync(AutomaticInteractionIntakeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request); ct.ThrowIfCancellationRequested();
        return Task.FromResult(new AutomaticInteractionGateResult(false, null, new DependencyNotAvailable("EXT-HOST-1")));
    }
}
