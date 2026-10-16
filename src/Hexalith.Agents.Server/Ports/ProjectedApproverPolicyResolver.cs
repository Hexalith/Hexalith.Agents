using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Resolves exact safe source/actor/binding/read authority using fresh coherent current evidence.</summary>
public sealed class ProjectedApproverPolicyResolver(ICurrentApproverEvidenceSource source,
    IAgentDependencyAvailability availability, TimeProvider clock) : IApproverPolicyResolver
{
    /// <inheritdoc />
    public Task<ApproverPolicyResolutionResult> ResolveAsync(string tenantId, AgentApproverPolicy policy, CancellationToken ct)
        => ResolveCoreAsync(tenantId, policy, null, ct);

    /// <inheritdoc />
    public Task<ApproverPolicyResolutionResult> ResolveForActorAsync(string tenantId, AgentApproverPolicy policy,
        ApproverResolutionContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ResolveCoreAsync(tenantId, policy, context, ct);
    }

    private async Task<ApproverPolicyResolutionResult> ResolveCoreAsync(string tenantId, AgentApproverPolicy policy,
        ApproverResolutionContext? context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var results = new List<ApproverSourceResolution>();
        foreach (string record in new[] { "EXT-HOST-1", "EXT-PARTIES-1", "EXT-CONV-AI-1" })
        {
            if (availability.Check(record) is { } blocker)
            {
                return new(policy.Sources.Select(item => new ApproverSourceResolution(item.Kind, ApproverSourceOutcome.Unavailable)).ToArray()) { Dependency = blocker };
            }
        }

        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tenantId) || (context is not null
            && (string.IsNullOrWhiteSpace(context.ActorId) || string.IsNullOrWhiteSpace(context.PartyId)
                || string.IsNullOrWhiteSpace(context.SourceConversationId))))
        {
            return new(policy.Sources.Select(item => new ApproverSourceResolution(item.Kind, ApproverSourceOutcome.Unauthorized)
            {
                Source = item,
            }).ToArray());
        }

        DateTimeOffset evaluationStarted = clock.GetUtcNow();
        DateTimeOffset lastObserved = evaluationStarted;
        foreach (ApproverPolicySource configured in policy.Sources)
        {
            ApproverSourceOutcome outcome = ApproverSourceOutcome.Unavailable;
            CurrentApproverEvidence? accepted = null;
            IReadOnlyList<CurrentApproverEvidence> captured = [];
            if (configured.Kind is ApproverPolicySourceKind.Caller or ApproverPolicySourceKind.Unknown || !Enum.IsDefined(configured.Kind))
            {
                results.Add(new(configured.Kind, outcome) { Source = configured });
                continue;
            }

            try
            {
                DateTimeOffset started = clock.GetUtcNow();
                if (started < lastObserved)
                {
                    return Unavailable(policy);
                }

                CurrentApproverEvidence[] candidates = (await source.ReadAsync(tenantId, configured, context?.SourceConversationId, ct).ConfigureAwait(false)).ToArray();
                DateTimeOffset now = clock.GetUtcNow();
                ct.ThrowIfCancellationRequested();
                if (now < started)
                {
                    return Unavailable(policy);
                }

                // Any malformed candidate blocks the source; filtering it out must not create false uniqueness.
                bool valid = candidates.All(item => item?.Source == configured && item.Actor is { } actor
                    && actor.IsCurrentFor(tenantId, actor.Subject, null, started, now)
                    && (configured.Kind != ApproverPolicySourceKind.PredefinedParty || configured.PartyId == item.Actor.PartyId));
                lastObserved = now;
                bool coherent = valid && candidates.GroupBy(item => item.Actor.PartyId, StringComparer.Ordinal)
                    .All(group => group.Count() == 1);
                if (valid && coherent && candidates.Length > 0)
                {
                    captured = candidates;
                    if (context is null)
                    {
                        outcome = ApproverSourceOutcome.Resolved;
                    }
                    else
                    {
                        CurrentApproverEvidence[] matching = candidates.Where(item => item.Actor.ActorId == context.ActorId
                            && item.Actor.PartyId == context.PartyId && item.Actor.ActorId != context.ExcludedActorId
                            && !string.IsNullOrWhiteSpace(context.SourceConversationId)
                            && item.SourceConversationId == context.SourceConversationId && item.HasReadAccess).ToArray();
                        accepted = matching.Length == 1 ? matching[0] : null;
                        outcome = accepted is not null ? ApproverSourceOutcome.Resolved : ApproverSourceOutcome.Unauthorized;
                    }
                }
                else if (valid)
                {
                    outcome = candidates.Length == 0 ? ApproverSourceOutcome.Missing : ApproverSourceOutcome.Ambiguous;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                accepted = null;
            }

            results.Add(new(configured.Kind, outcome) { Source = configured, Evidence = accepted, Candidates = captured });
        }

        CurrentApproverEvidence[] currentCandidates = results.SelectMany(item => item.Candidates).ToArray();
        if (currentCandidates.Length == 0)
        {
            ct.ThrowIfCancellationRequested();
            return new(results);
        }

        // Recheck every accepted interval after the final source await; later revocation/expiry never falls back.
        bool confirmed = false;
        try
        {
            ct.ThrowIfCancellationRequested();
            confirmed = await source.ConfirmAsync(tenantId, currentCandidates, ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            confirmed = false;
        }

        DateTimeOffset completed = clock.GetUtcNow();
        ct.ThrowIfCancellationRequested();
        if (!confirmed || completed < lastObserved || currentCandidates.Any(candidate =>
            !candidate.Actor.IsCurrentFor(tenantId, candidate.Actor.Subject, null, evaluationStarted, completed)))
        {
            return Unavailable(policy);
        }

        return new(results);
    }

    private static ApproverPolicyResolutionResult Unavailable(AgentApproverPolicy policy)
        => new(policy.Sources.Select(item => new ApproverSourceResolution(item.Kind, ApproverSourceOutcome.Unavailable)
        {
            Source = item,
        }).ToArray());
}
