using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Authorizes only a contiguous fold that corresponds to the current authoritative source head.</summary>
public sealed class ProjectedTenantAccessReader(ICurrentTenantAuthoritySource source, IAgentDependencyAvailability availability,
    TimeProvider clock, ICurrentHumanActorSource? actorSource = null) : ITenantAccessReader
{
    /// <inheritdoc />
    public async Task<TenantAccessReadResult> ReadAsync(string tenantId, string actorUserId, string callerPartyId, CancellationToken ct)
    {
        if ((availability.Check("EXT-HOST-1") ?? availability.Check("EXT-PARTIES-1")) is { } blocker)
        {
            return TenantAccessReadResult.Unavailable with { Dependency = blocker };
        }

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(actorUserId))
        {
            return new(AgentInteractionGateOutcome.Unauthorized, false);
        }

        try
        {
            if (actorSource is null)
            {
                return TenantAccessReadResult.Unavailable;
            }

            DateTimeOffset started = clock.GetUtcNow();
            CurrentHumanActorEvidence? actor = await actorSource.ReadAsync(tenantId, actorUserId, ct).ConfigureAwait(false);
            bool administration = string.IsNullOrEmpty(callerPartyId);
            ct.ThrowIfCancellationRequested();
            if (!IsCurrentActor(actor, tenantId, actorUserId, callerPartyId, administration, started, clock.GetUtcNow()))
            {
                return new(AgentInteractionGateOutcome.Unauthorized, false);
            }

            TenantAuthorityEvidence? evidence = await source.ReadAsync(tenantId, false, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            DateTimeOffset now = clock.GetUtcNow();
            bool granted = IsCurrentActor(actor, tenantId, actorUserId, callerPartyId, administration, started, now)
                && Grants(evidence, tenantId, actor.ActorId, administration ? "Agents.Administrator" : "User",
                    administration ? null : callerPartyId, false, now, started);
            return new(granted ? AgentInteractionGateOutcome.Satisfied : AgentInteractionGateOutcome.Unauthorized, granted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return TenantAccessReadResult.Unavailable;
        }
    }

    private static bool IsCurrentActor([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] CurrentHumanActorEvidence? actor, string tenantId, string subject,
        string callerPartyId, bool administration, DateTimeOffset started, DateTimeOffset now)
        => actor?.IsCurrentFor(tenantId, subject, administration ? null : callerPartyId, started, now) == true;

    /// <summary>Checks complete source scope, event sequence, current tenant state and exact actor/role binding.</summary>
    internal static bool Grants(TenantAuthorityEvidence? evidence, string tenantId, string actorId, string role,
        string? partyId, bool global, DateTimeOffset now, DateTimeOffset? notBefore = null)
    {
        if (evidence is null || evidence.TenantId != tenantId
            || evidence.Domain != (global ? "global-administrators" : "tenants")
            || evidence.SourceTenantId != "system" || evidence.SourceAggregateId != (global ? "global-administrators" : tenantId)
            || evidence.SourceHead <= 0 || evidence.Events.Count != evidence.SourceHead
            || evidence.ObservedAt == default || evidence.ObservedAt > now || evidence.ValidUntil <= now
            || (notBefore is { } started && (now < started || evidence.ObservedAt < started))
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
