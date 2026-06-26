using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

public sealed class AgentAuditQueryContractsTests
{
    [Fact]
    public void Audit_query_records_expose_stable_discriminators()
    {
        GetAgentInteractionStatusQuery.Domain.ShouldBe("agent-interaction");
        GetAgentInteractionStatusQuery.QueryType.ShouldBe("get-agent-interaction-status");
        GetAgentInteractionGateEvidenceQuery.QueryType.ShouldBe("get-agent-interaction-gate-evidence");
        GetAgentInteractionContextEvidenceQuery.QueryType.ShouldBe("get-agent-interaction-context-evidence");
        GetAgentGenerationEvidenceQuery.QueryType.ShouldBe("get-agent-generation-evidence");
        GetAgentPostingEvidenceQuery.QueryType.ShouldBe("get-agent-posting-evidence");
        GetAgentProposalEditEvidenceQuery.QueryType.ShouldBe("get-agent-proposal-edit-evidence");
        GetAgentProposalRegenerationEvidenceQuery.QueryType.ShouldBe("get-agent-proposal-regeneration-evidence");
        GetAgentProposalApprovalEvidenceQuery.QueryType.ShouldBe("get-agent-proposal-approval-evidence");
        GetAgentAuditAvailabilityQuery.QueryType.ShouldBe("get-agent-audit-availability");
    }

    [Fact]
    public void Added_audit_query_records_round_trip_without_tenant_or_secret_fields()
    {
        RoundTrip(new GetAgentGenerationEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");
        RoundTrip(new GetAgentPostingEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");
        RoundTrip(new GetAgentAuditAvailabilityQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");

        string json = JsonSerializer.Serialize(new GetAgentGenerationEvidenceQuery("interaction-1"));
        json.ShouldNotContain("tenant", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public void Audit_governance_readiness_blocks_content_bearing_redacted_excerpt()
    {
        AgentAuditGovernanceReadiness readiness = AgentAuditGovernanceReadiness.MetadataOnlyBlocked;

        readiness.ActiveTreatment.ShouldBe(ContentSafetyAuditTreatment.MetadataOnly);
        readiness.BlockedTreatment.ShouldBe(ContentSafetyAuditTreatment.RedactedExcerpt);
        readiness.Blockers.ShouldContain(AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved);
    }

    [Fact]
    public void All_audit_query_records_share_the_agent_interaction_domain()
    {
        const string Domain = "agent-interaction";

        GetAgentInteractionStatusQuery.Domain.ShouldBe(Domain);
        GetAgentInteractionGateEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentInteractionContextEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentGenerationEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentPostingEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentProposalEditEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentProposalRegenerationEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentProposalApprovalEvidenceQuery.Domain.ShouldBe(Domain);
        GetAgentAuditAvailabilityQuery.Domain.ShouldBe(Domain);
    }

    [Fact]
    public void Existing_audit_query_records_round_trip_without_tenant_or_secret_fields()
    {
        RoundTrip(new GetAgentInteractionStatusQuery()).ShouldNotBeNull(); // keyed only by the envelope aggregate id — no id payload
        RoundTrip(new GetAgentInteractionGateEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");
        RoundTrip(new GetAgentInteractionContextEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");
        RoundTrip(new GetAgentProposalEditEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");
        RoundTrip(new GetAgentProposalRegenerationEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");
        RoundTrip(new GetAgentProposalApprovalEvidenceQuery("interaction-1")).AgentInteractionId.ShouldBe("interaction-1");

        string json = JsonSerializer.Serialize(new GetAgentProposalApprovalEvidenceQuery("interaction-1"));
        json.ShouldNotContain("tenant", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public void Audit_governance_readiness_serializes_by_name_without_secret_or_content()
    {
        string json = JsonSerializer.Serialize(AgentAuditGovernanceReadiness.MetadataOnlyBlocked);

        json.ShouldContain("MetadataOnly");
        json.ShouldContain("RedactedExcerpt");
        json.ShouldContain(AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("prompt", Case.Insensitive);
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
