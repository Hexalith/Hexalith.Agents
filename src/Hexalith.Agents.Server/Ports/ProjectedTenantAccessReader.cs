using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Authorizes only a contiguous fold that corresponds to the current authoritative source head.</summary>
public sealed class ProjectedTenantAccessReader(ICurrentTenantAuthoritySource source, IAgentDependencyAvailability availability,
    TimeProvider clock) : ITenantAccessReader
{
    /// <inheritdoc />
    public async Task<TenantAccessReadResult> ReadAsync(string tenantId, string actorUserId, string callerPartyId, CancellationToken ct)
    {
        if (availability.Check("EXT-HOST-1") is { } blocker)
        {
            return TenantAccessReadResult.Unavailable with { Dependency = blocker };
        }

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(actorUserId))
        {
            return new(AgentInteractionGateOutcome.Unauthorized, false);
        }

        try
        {
            TenantAuthorityEvidence? evidence = await source.ReadAsync(tenantId, false, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            bool administration = string.IsNullOrEmpty(callerPartyId);
            bool granted = Grants(evidence, tenantId, actorUserId, administration ? "Agents.Administrator" : "User",
                administration ? null : callerPartyId, false, clock.GetUtcNow());
            return new(granted ? AgentInteractionGateOutcome.Satisfied : AgentInteractionGateOutcome.Unauthorized, granted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return TenantAccessReadResult.Unavailable;
        }
    }

    /// <summary>Checks complete source scope, event sequence, current tenant state and exact actor/role binding.</summary>
    internal static bool Grants(TenantAuthorityEvidence? evidence, string tenantId, string actorId, string role,
        string? partyId, bool global, DateTimeOffset now)
    {
        if (evidence is null || evidence.TenantId != tenantId
            || evidence.Domain != (global ? "global-administrators" : "tenant")
            || evidence.SourceHead <= 0 || evidence.Events.Count != evidence.SourceHead
            || evidence.ObservedAt == default || evidence.ObservedAt > now || evidence.ValidUntil <= now
            || evidence.ObservedAt.Offset != TimeSpan.Zero || evidence.ValidUntil.Offset != TimeSpan.Zero)
        {
            return false;
        }

        bool active = global;
        bool granted = false;
        long position = 0;
        foreach (TenantAuthorityEvent item in evidence.Events)
        {
            if (item.Sequence != ++position || item.Role is not ("TenantActive" or "User" or "Agents.Administrator" or "Agents.PlatformOperator"))
            {
                return false;
            }

            if (item.Role == "TenantActive")
            {
                active = item.Granted;
            }
            else if (item.ActorId == actorId && item.Role == role && item.PartyId == partyId)
            {
                granted = item.Granted;
            }
        }

        return position == evidence.SourceHead && active && granted;
    }
}
