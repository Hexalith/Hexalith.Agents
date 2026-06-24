namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Shouldly;

/// <summary>
/// Precedence tests for the pure <see cref="ApproverPolicyVerdict"/> evaluator (Story 1.6 AC3; AD-8, AD-12). The
/// verdict is fail-closed worst-first: an empty policy is <c>Incomplete</c>; then <c>Unauthorized</c> dominates,
/// then <c>Unavailable</c>/<c>Unknown</c>, then <c>Ambiguous</c>, then <c>Disabled</c>, then <c>Missing</c>; only an
/// all-<c>Resolved</c> policy is <c>Valid</c>. Verified through a directly constructed
/// <see cref="ApproverPolicyResolutionResult"/>.
/// </summary>
public sealed class ApproverPolicyVerdictTests
{
    [Fact]
    public void Empty_policy_is_incomplete_regardless_of_the_resolution()
        => ApproverPolicyVerdict.Evaluate(Policy(0), Result())
            .ShouldBe(ApproverPolicyValidationStatus.Incomplete);

    [Fact]
    public void All_resolved_sources_are_valid()
        => ApproverPolicyVerdict.Evaluate(Policy(3), Result(
                ApproverSourceOutcome.Resolved,
                ApproverSourceOutcome.Resolved,
                ApproverSourceOutcome.Resolved))
            .ShouldBe(ApproverPolicyValidationStatus.Valid);

    [Fact]
    public void Any_unauthorized_source_dominates_every_other_outcome()
        => ApproverPolicyVerdict.Evaluate(Policy(5), Result(
                ApproverSourceOutcome.Missing,
                ApproverSourceOutcome.Disabled,
                ApproverSourceOutcome.Ambiguous,
                ApproverSourceOutcome.Unavailable,
                ApproverSourceOutcome.Unauthorized))
            .ShouldBe(ApproverPolicyValidationStatus.Unauthorized);

    [Fact]
    public void Unavailable_beats_ambiguous_disabled_and_missing()
        => ApproverPolicyVerdict.Evaluate(Policy(4), Result(
                ApproverSourceOutcome.Missing,
                ApproverSourceOutcome.Disabled,
                ApproverSourceOutcome.Ambiguous,
                ApproverSourceOutcome.Unavailable))
            .ShouldBe(ApproverPolicyValidationStatus.Unavailable);

    [Fact]
    public void Unknown_source_outcome_fails_closed_to_unavailable()
        => ApproverPolicyVerdict.Evaluate(Policy(2), Result(
                ApproverSourceOutcome.Resolved,
                ApproverSourceOutcome.Unknown))
            .ShouldBe(ApproverPolicyValidationStatus.Unavailable);

    [Fact]
    public void Ambiguous_beats_disabled_and_missing()
        => ApproverPolicyVerdict.Evaluate(Policy(3), Result(
                ApproverSourceOutcome.Missing,
                ApproverSourceOutcome.Disabled,
                ApproverSourceOutcome.Ambiguous))
            .ShouldBe(ApproverPolicyValidationStatus.Ambiguous);

    [Fact]
    public void Disabled_beats_missing()
        => ApproverPolicyVerdict.Evaluate(Policy(2), Result(
                ApproverSourceOutcome.Missing,
                ApproverSourceOutcome.Disabled))
            .ShouldBe(ApproverPolicyValidationStatus.Disabled);

    [Fact]
    public void Any_missing_source_is_missing_when_nothing_worse_is_present()
        => ApproverPolicyVerdict.Evaluate(Policy(2), Result(
                ApproverSourceOutcome.Resolved,
                ApproverSourceOutcome.Missing))
            .ShouldBe(ApproverPolicyValidationStatus.Missing);

    [Fact]
    public void Partial_resolution_not_covering_every_source_fails_closed_to_unavailable()
        // A degraded/partial read that resolves fewer outcomes than the configured sources must never be Valid for the
        // uncovered sources — it fails closed to Unavailable (AC3; AD-12), even when every returned outcome is Resolved.
        => ApproverPolicyVerdict.Evaluate(Policy(3), Result(
                ApproverSourceOutcome.Resolved,
                ApproverSourceOutcome.Resolved))
            .ShouldBe(ApproverPolicyValidationStatus.Unavailable);

    private static AgentApproverPolicy Policy(int sourceCount)
    {
        var sources = new List<ApproverPolicySource>(sourceCount);
        for (int i = 0; i < sourceCount; i++)
        {
            sources.Add(new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null));
        }

        return new AgentApproverPolicy(sources, ApproverPolicyBasisDisclosure.OperatorOnly);
    }

    private static ApproverPolicyResolutionResult Result(params ApproverSourceOutcome[] outcomes)
    {
        var sources = new List<ApproverSourceResolution>(outcomes.Length);
        foreach (ApproverSourceOutcome outcome in outcomes)
        {
            sources.Add(new ApproverSourceResolution(ApproverPolicySourceKind.Caller, outcome));
        }

        return new ApproverPolicyResolutionResult(sources);
    }
}
