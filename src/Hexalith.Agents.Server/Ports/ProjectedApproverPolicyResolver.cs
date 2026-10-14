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
        => ResolveCoreAsync(tenantId, policy, context, ct);

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

        foreach (ApproverPolicySource configured in policy.Sources)
        {
            ApproverSourceOutcome outcome = ApproverSourceOutcome.Unavailable;
            CurrentApproverEvidence? accepted = null;
            IReadOnlyList<CurrentApproverEvidence> captured = [];
            if (configured.Kind is ApproverPolicySourceKind.Caller or ApproverPolicySourceKind.Unknown || !Enum.IsDefined(configured.Kind))
            {
                results.Add(new(configured.Kind, outcome));
                continue;
            }

            try
            {
                DateTimeOffset started = clock.GetUtcNow();
                IReadOnlyList<CurrentApproverEvidence> candidates = await source.ReadAsync(tenantId, configured, context?.SourceConversationId, ct).ConfigureAwait(false);
                captured = candidates.ToArray();
                DateTimeOffset now = clock.GetUtcNow();
                ct.ThrowIfCancellationRequested();
                // Any malformed candidate blocks the source; filtering it out must not create false uniqueness.
                bool valid = now >= started && candidates.All(item => item.Source == configured && item.Actor.TenantId == tenantId
                    && !string.IsNullOrWhiteSpace(item.Actor.ActorId) && !string.IsNullOrWhiteSpace(item.Actor.PartyId)
                    && item.Actor.BindingVersion > 0 && item.Actor.SourcePosition > 0 && item.Actor.ObservedAt >= started
                    && item.Actor.ObservedAt <= now && item.Actor.ValidUntil > now
                    && (configured.Kind != ApproverPolicySourceKind.PredefinedParty || configured.PartyId == item.Actor.PartyId));
                bool coherent = candidates.GroupBy(item => item.Actor.PartyId, StringComparer.Ordinal)
                    .All(group => group.Count() == 1);
                if (valid && coherent && candidates.Count > 0)
                {
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
                    outcome = candidates.Count == 0 ? ApproverSourceOutcome.Missing : ApproverSourceOutcome.Ambiguous;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                accepted = null;
            }

            results.Add(new(configured.Kind, outcome) { Source = configured, Evidence = accepted, Candidates = captured });
        }

        // Recheck every accepted interval after the final source await; later revocation/expiry never falls back.
        bool confirmed = false;
        try
        {
            confirmed = await source.ConfirmAsync(tenantId, results.SelectMany(item => item.Candidates).ToArray(), ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            confirmed = false;
        }

        DateTimeOffset completed = clock.GetUtcNow();
        ct.ThrowIfCancellationRequested();
        if (!confirmed || results.Any(item => item.Candidates.Any(candidate => candidate.Actor.ValidUntil <= completed
            || candidate.Actor.ObservedAt > completed)))
        {
            return new(results.Select(item => item with { Outcome = ApproverSourceOutcome.Unavailable, Evidence = null }).ToArray());
        }

        return new(results);
    }
}
