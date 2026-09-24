using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Exclusive and nonextending data handling grace deadline.</summary>
public sealed class DataHandlingGraceDeadlineTests
{
    [Fact]
    public void Changed_terms_reject_when_imported_version_is_exhausted()
    {
        var current = new ProviderDataHandlingRecord(30, false, ["EU"], "terms", int.MaxValue);
        var changed = current with { RetentionDays = 20, DataHandlingVersion = 0 };

        ProviderDataHandlingPolicy.Validate(changed, current).ShouldBe("Data handling version is exhausted.");
        Should.Throw<InvalidOperationException>(() => ProviderDataHandlingPolicy.Assign(changed, current));
    }

    [Fact]
    public void Successive_cumulative_tightening_uses_first_deadline_exclusively()
    {
        DateTimeOffset firstAt = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        ProviderDataHandlingRecord accepted = new(90, true, ["EU", "US"], "terms", 1, firstAt.AddDays(-10));
        ProviderDataHandlingRecord first = Declare(accepted, new(60, true, ["EU", "US"], "terms", 2, firstAt));
        ProviderDataHandlingRecord second = Declare(first, new(30, false, ["EU"], "terms", 3, firstAt.AddDays(20)));
        var tenant = new TenantProviderEntryState { Enabled = true, AcceptedTerms = accepted };
        DateTimeOffset deadline = firstAt.AddDays(30);
        TenantProviderEligibility.Evaluate(tenant, [accepted, first, second], deadline.AddTicks(-1)).Status.ShouldBe("Grace");
        TenantProviderEligibility.Evaluate(tenant, [accepted, first, second], deadline.AddTicks(-1))
            .GraceExpiresAt.ShouldBe(deadline);
        TenantProviderEligibility.Evaluate(tenant, [accepted, first, second], deadline).Status.ShouldBe("GraceExpired");
    }

    [Fact]
    public void Decline_loosening_or_missing_evidence_blocks_grace()
    {
        DateTimeOffset now = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        ProviderDataHandlingRecord accepted = new(30, false, ["EU"], "terms", 1, now.AddDays(-1));
        ProviderDataHandlingRecord loosening = new(60, false, ["EU"], "terms", 2, now);
        var tenant = new TenantProviderEntryState { Enabled = true, AcceptedTerms = accepted };
        TenantProviderEligibility.Evaluate(tenant, [accepted, loosening], now).Status.ShouldBe("TermsChanged");
        TenantProviderEligibility.Evaluate(tenant, [accepted, loosening with { DataHandlingVersion = 3 }], now)
            .Status.ShouldBe("UnknownEvidence");
        tenant.DeclinedVersion = 2;
        TenantProviderEligibility.Evaluate(tenant, [accepted, loosening], now).Status.ShouldBe("Declined");
        TenantProviderEligibility.Evaluate(tenant, [accepted, loosening, loosening with { DataHandlingVersion = 3 }], now)
            .Status.ShouldBe("AcceptanceRequired");
    }

    [Theory]
    [InlineData(false, "TermsChanged")]
    [InlineData(true, "Grace")]
    public void Tightening_declaration_matrix_requires_recorded_operator_evidence(bool declare, string expectedStatus)
    {
        DateTimeOffset now = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        ProviderDataHandlingRecord accepted = new(90, true, ["EU", "US"], "terms", 1, now.AddDays(-1));
        ProviderDataHandlingRecord undeclared = new(30, false, ["EU"], "terms", 2, now);
        var tenant = new TenantProviderEntryState { Enabled = true, AcceptedTerms = accepted };

        ProviderDataHandlingRecord next = declare ? Declare(accepted, undeclared) : undeclared;
        TenantProviderEligibility.Evaluate(tenant, [accepted, next], now).Status.ShouldBe(expectedStatus);
    }

    [Fact]
    public void Forged_tightening_diff_blocks_grace()
    {
        DateTimeOffset now = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        ProviderDataHandlingRecord accepted = new(90, true, ["EU", "US"], "terms", 1, now.AddDays(-1));
        ProviderDataHandlingRecord declared = Declare(accepted, new(30, false, ["EU"], "terms", 2, now));
        var tenant = new TenantProviderEntryState { Enabled = true, AcceptedTerms = accepted };
        TenantProviderEligibility.Evaluate(tenant, [accepted, declared with
        {
            TighteningDeclaration = declared.TighteningDeclaration! with { FromVersion = 0 },
        }], now).Status.ShouldBe("TermsChanged");
    }

    private static ProviderDataHandlingRecord Declare(ProviderDataHandlingRecord prior, ProviderDataHandlingRecord current)
        => current with
        {
            TighteningDeclaration = ProviderDataHandlingPolicy.DeclareTightening(prior, current, "operator")
                ?? throw new InvalidOperationException("Expected a tightening."),
        };
}
