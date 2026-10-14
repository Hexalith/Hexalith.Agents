using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>Produces the same safe current basis for direct API reads and EventStore query reads.</summary>
internal static class AgentSetupBasisEnricher
{
    /// <summary>Enriches only an already-authorized exact-scope setup; current resolver uncertainty blocks each source.</summary>
    internal static async Task<AgentSetupResult> EnrichAsync(AgentSetupResult result, AgentSetupReadModel? model,
        IApproverPolicyResolver? resolver, CancellationToken cancellationToken)
    {
        if (result.Setup is not { } setup || model?.ApproverPolicySources is not { Count: > 0 } sources)
        {
            return result;
        }

        ApproverPolicyResolutionResult? resolved = null;
        if (resolver is not null)
        {
            try
            {
                resolved = await resolver.ResolveAsync(model.TenantId,
                    new AgentApproverPolicy(sources, model.ApproverPolicyDisclosure), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                resolved = null;
            }
        }

        var basis = new List<ApproverPolicyBasisEntry>();
        for (int index = 0; index < sources.Count; index++)
        {
            ApproverSourceResolution? item = resolved?.Sources.Count == sources.Count ? resolved.Sources[index] : null;
            ApproverPolicyValidationStatus status = item is null || item.Kind != sources[index].Kind
                || item.Source != sources[index]
                || sources[index].Kind is ApproverPolicySourceKind.Caller or ApproverPolicySourceKind.Unknown
                ? ApproverPolicyValidationStatus.Unavailable : item.Outcome switch
                {
                    ApproverSourceOutcome.Resolved => ApproverPolicyValidationStatus.Valid,
                    ApproverSourceOutcome.Missing => ApproverPolicyValidationStatus.Missing,
                    ApproverSourceOutcome.Disabled => ApproverPolicyValidationStatus.Disabled,
                    ApproverSourceOutcome.Ambiguous => ApproverPolicyValidationStatus.Ambiguous,
                    ApproverSourceOutcome.Unauthorized => ApproverPolicyValidationStatus.Unauthorized,
                    _ => ApproverPolicyValidationStatus.Unavailable,
                };
            basis.Add(new(sources[index].Kind, status));
        }

        return result with { Setup = setup with { Agent = setup.Agent with { ApproverPolicyBasis = basis } } };
    }
}
