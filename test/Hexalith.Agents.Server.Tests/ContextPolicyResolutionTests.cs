namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.AgentInteractions;

using Shouldly;

/// <summary>
/// Tests for <see cref="ContextPolicyResolution"/> (Story 2.3; AC4; AD-11, OQ-10). V1 ships exactly one context policy
/// (<c>"full-conversation-v1"</c>) which approves NO bounded behavior, so an oversized Conversation always blocks rather
/// than being silently truncated; any unknown reference also fails closed to no approved behavior. This pins the OQ-10
/// resolution (the bounded path's shape exists and is tested, but no concrete bounded policy is wired in V1).
/// </summary>
public sealed class ContextPolicyResolutionTests
{
    [Fact]
    public void V1_default_policy_resolves_to_no_approved_bounded_behavior()
    {
        // The only V1 policy approves no bounded behavior — oversized context blocks, never silently truncates (AC4).
        ContextPolicyResolution.Resolve(AgentInteractionSnapshot.DefaultContextPolicyReference).ShouldBeNull();
    }

    [Theory]
    [InlineData("bounded-conversation-v1")]
    [InlineData("summarize-and-fit-v2")]
    [InlineData("")]
    [InlineData("unknown-policy-reference")]
    public void Unknown_or_unrecognized_policy_references_resolve_to_no_approved_bounded_behavior(string reference)
    {
        // Any reference other than a wired approved policy fails closed to null — no invented truncation behavior (AD-11).
        ContextPolicyResolution.Resolve(reference).ShouldBeNull();
    }
}
