using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>The directory twin policy maps every accepted command through the same Compute.</summary>
public sealed class ConversationDirectoryTwinPolicyTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void EachCommandDecisionAgreesWithItsFirstDurableFact(int variant)
    {
        var s = ConversationDirectoryTestData.State(); var p = ConversationDirectoryTestData.Permit(); var l = ConversationDirectoryTestData.Lease();
        object command = variant switch
        {
            0 => new RegisterInteractionPermit(p, s.Revision),
            1 => new AcknowledgeInteractionCreation(p.AgentInteractionId, p.PermitId, p.CreationOutboxId, 1, s.Revision),
            2 => new AcquireConversationEffectLease(l, s.Revision),
            3 => new CommitConversationEffect(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, s.Revision),
            4 => new SettleConversationEffectLease(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, 2, 4, s.Revision),
            5 => new CancelConversationEffectLease(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, s.Revision),
            _ => new EnterConversationDeletionClosing(p.TenantId, p.ConversationId, new string('d', 64), s.Revision),
        };
        var facts = ConversationDirectoryPolicy.Evaluate(command, s);
        ConversationDirectoryPolicy.Decide(command, s).ShouldBe(facts[0].GetType().Name);
        facts.Count.ShouldBe(1);
    }
}
