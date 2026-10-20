using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Protection aliases are target scoped; matching an alias never authorizes another tenant or interaction.</summary>
public sealed class CrossTenantProtectionKeyAliasSubstitutionTests
{
    [Theory]
    [InlineData("tenant-b", false)]
    [InlineData("tenant-a", true)]
    public void WrongProtectionTenantOrInteractionCannotCreatePermit(string tenant, bool changeInteraction)
    {
        var p = ConversationDirectoryTestData.Permit();
        p = p with { Request = p.Request with { Target = p.Request.Target with { TenantId = tenant, AgentInteractionId = changeInteraction ? new string('b', 64) : p.AgentInteractionId } } };
        var result = ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(p, 0), null, ConversationDirectoryTestData.Envelope());
        result.Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.ProtectionTargetMismatch);
    }
}
