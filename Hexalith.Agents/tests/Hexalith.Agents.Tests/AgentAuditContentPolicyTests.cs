using System.Text.Json;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;

using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>
/// AC4 enforcement tests for the content-bearing audit block (Story 4.2). Only <see cref="ContentSafetyAuditTreatment.MetadataOnly"/>
/// is active; any content-bearing request (<see cref="ContentSafetyAuditTreatment.RedactedExcerpt"/> or the
/// <see cref="ContentSafetyAuditTreatment.Unknown"/> sentinel) must resolve to a safe non-success <c>Blocked</c> result —
/// never success, never content — while the named retention/legal-hold/export/deletion blocker stays surfaced (AD-14).
/// </summary>
public sealed class AgentAuditContentPolicyTests
{
    [Fact]
    public void Evaluate_metadata_only_succeeds()
    {
        AgentOperationResult<ContentSafetyAuditTreatment> result = AgentAuditContentPolicy.Evaluate(ContentSafetyAuditTreatment.MetadataOnly);

        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(AgentOperationStatus.Succeeded);
        result.Value.ShouldBe(ContentSafetyAuditTreatment.MetadataOnly);
    }

    [Theory]
    [InlineData(ContentSafetyAuditTreatment.RedactedExcerpt)]
    [InlineData(ContentSafetyAuditTreatment.Unknown)]
    public void Evaluate_content_bearing_treatment_is_blocked_never_success(ContentSafetyAuditTreatment treatment)
    {
        AgentOperationResult<ContentSafetyAuditTreatment> result = AgentAuditContentPolicy.Evaluate(treatment);

        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(AgentOperationStatus.Blocked);
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Blocked);
        result.Value.ShouldBe(ContentSafetyAuditTreatment.Unknown); // no content-bearing treatment is ever emitted
    }

    [Fact]
    public void GetReadiness_reports_metadata_only_with_the_named_governance_blocker()
    {
        AgentAuditGovernanceReadiness readiness = AgentAuditContentPolicy.GetReadiness();

        readiness.ActiveTreatment.ShouldBe(ContentSafetyAuditTreatment.MetadataOnly);
        readiness.BlockedTreatment.ShouldBe(ContentSafetyAuditTreatment.RedactedExcerpt);
        readiness.Blockers.ShouldContain(AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved);
    }

    [Fact]
    public void Readiness_serializes_enums_by_name_without_secret_or_content()
    {
        string json = JsonSerializer.Serialize(AgentAuditContentPolicy.GetReadiness());

        json.ShouldContain("MetadataOnly");
        json.ShouldContain("RedactedExcerpt");
        json.ShouldContain(AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("prompt", Case.Insensitive);
    }
}
