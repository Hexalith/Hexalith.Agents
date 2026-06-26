namespace Hexalith.Agents.Server.Tests;

using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Ports;

using Hexalith.Parties.Client;
using Hexalith.Parties.Client.Abstractions;
using Hexalith.Parties.Contracts.Commands;
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.ValueObjects;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Adapter tests for <see cref="PartiesAgentPartyDirectory"/> (Story 1.4 AC1, AC2). Drives a substituted public
/// Parties client and asserts each observed <see cref="PartyDetail"/> state maps to the correct fail-closed
/// <see cref="PartyLinkValidationStatus"/> (Dev Notes › Parties State → Verdict Mapping), that provisioning builds
/// an <c>Organization</c> <see cref="CreateParty"/> with a deterministic id derived from the Agent id, and that no
/// Party PII ever returns to the Agents side (only <c>{ Status, PartyId }</c> crosses the boundary).
/// </summary>
public sealed class PartiesAgentPartyDirectoryTests
{
    private const string TenantId = "acme";
    private const string PartyId = "party-001";

    private readonly IPartiesQueryClient _queryClient = Substitute.For<IPartiesQueryClient>();
    private readonly IPartiesCommandClient _commandClient = Substitute.For<IPartiesCommandClient>();

    private PartiesAgentPartyDirectory Directory => new(_queryClient, _commandClient);

    // ===== Validate-existing verdict mapping =====

    [Fact]
    public async Task Active_current_party_maps_to_valid_and_returns_only_the_id()
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(Party(isActive: true, freshness: ProjectionFreshnessStatus.Current));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Valid);
        result.PartyId.ShouldBe(PartyId); // the safe id reference, never the display name
    }

    [Fact]
    public async Task Active_party_with_null_freshness_maps_to_valid()
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(Party(isActive: true, freshness: null));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Valid);
    }

    [Fact]
    public async Task Not_found_maps_to_missing()
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new PartiesClientException(404, "Not Found", null, null, null));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Missing);
        result.PartyId.ShouldBeNull();
    }

    [Theory]
    [InlineData(false, false, false)] // inactive
    [InlineData(true, true, false)]   // erased
    [InlineData(true, false, true)]   // restricted
    public async Task Inactive_erased_or_restricted_party_maps_to_disabled(bool isActive, bool isErased, bool isRestricted)
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(Party(isActive: isActive, isErased: isErased, isRestricted: isRestricted, freshness: ProjectionFreshnessStatus.Current));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Disabled);
    }

    [Theory]
    [InlineData(ProjectionFreshnessStatus.Stale)]
    [InlineData(ProjectionFreshnessStatus.Rebuilding)]
    [InlineData(ProjectionFreshnessStatus.Degraded)]
    [InlineData(ProjectionFreshnessStatus.Unavailable)]
    [InlineData(ProjectionFreshnessStatus.LocalOnly)]
    public async Task Non_current_projection_maps_to_unavailable_failing_closed(ProjectionFreshnessStatus freshness)
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .Returns(Party(isActive: true, freshness: freshness));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unavailable); // never trust a stale projection as fresh (AD-12)
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task Unauthorized_read_maps_to_unauthorized(int status)
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new PartiesClientException(status, "Forbidden", null, null, null));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unauthorized);
    }

    [Fact]
    public async Task Gateway_error_maps_to_unavailable()
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new PartiesClientException(500, "Server Error", null, null, null));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
    }

    [Fact]
    public async Task Unexpected_transport_failure_maps_to_unavailable()
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new System.Net.Http.HttpRequestException("connection reset"));

        AgentPartyValidationResult result = await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
    }

    // ===== Provisioning =====

    [Fact]
    public async Task Provisioning_builds_an_organization_party_with_a_deterministic_id_and_no_pii_returned()
    {
        CreateParty? captured = null;
        _commandClient.CreatePartyWithResultAsync(Arg.Do<CreateParty>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(new PartiesCommandResult<PartyDetail>("corr", Party(id: "agent-hexa")));

        var request = new AgentPartyProvisioningRequest("hexa", "Agent hexa");
        AgentPartyValidationResult result = await Directory.ProvisionAgentPartyAsync(TenantId, request, CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Valid);
        result.PartyId.ShouldBe(PartiesAgentPartyDirectory.DeriveProvisionedPartyId("hexa"));

        captured.ShouldNotBeNull();
        captured.PartyId.ShouldBe("agent-hexa");          // deterministic id derived from the Agent id (AD-13)
        captured.Type.ShouldBe(PartyType.Organization);   // no AiAgent/Bot/System type — Organization avoids person PII
        captured.PersonDetails.ShouldBeNull();
        captured.OrganizationDetails.ShouldNotBeNull().LegalName.ShouldBe("Agent hexa"); // label lives in Parties only
    }

    [Fact]
    public void Provisioned_party_id_is_deterministic_for_a_given_agent()
    {
        PartiesAgentPartyDirectory.DeriveProvisionedPartyId("hexa")
            .ShouldBe(PartiesAgentPartyDirectory.DeriveProvisionedPartyId("hexa"));
    }

    [Fact]
    public async Task Provisioning_unauthorized_maps_to_unauthorized()
    {
        _commandClient.CreatePartyWithResultAsync(Arg.Any<CreateParty>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PartiesClientException(403, "Forbidden", null, null, null));

        AgentPartyValidationResult result = await Directory.ProvisionAgentPartyAsync(
            TenantId, new AgentPartyProvisioningRequest("hexa", "Agent hexa"), CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unauthorized);
        result.PartyId.ShouldBeNull();
    }

    [Fact]
    public async Task Provisioning_gateway_failure_maps_to_unavailable()
    {
        _commandClient.CreatePartyWithResultAsync(Arg.Any<CreateParty>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PartiesClientException(500, "Server Error", null, null, null));

        AgentPartyValidationResult result = await Directory.ProvisionAgentPartyAsync(
            TenantId, new AgentPartyProvisioningRequest("hexa", "Agent hexa"), CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
    }

    [Fact]
    public async Task Provisioning_unexpected_transport_failure_maps_to_unavailable()
    {
        // Symmetry with validate-existing: a non-gateway transport fault is dependency uncertainty — fail closed.
        _commandClient.CreatePartyWithResultAsync(Arg.Any<CreateParty>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new System.Net.Http.HttpRequestException("connection reset"));

        AgentPartyValidationResult result = await Directory.ProvisionAgentPartyAsync(
            TenantId, new AgentPartyProvisioningRequest("hexa", "Agent hexa"), CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
    }

    // ===== Cancellation must propagate — never be reclassified as a fail-closed verdict (AD-12 fail-closed is for
    // dependency uncertainty, not for a caller-requested cancellation) =====

    [Fact]
    public async Task Validate_existing_propagates_cancellation_instead_of_mapping_to_a_verdict()
    {
        _queryClient.GetPartyAsync(PartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Directory.ValidateExistingPartyAsync(TenantId, PartyId, CancellationToken.None));
    }

    [Fact]
    public async Task Provisioning_propagates_cancellation_instead_of_mapping_to_a_verdict()
    {
        _commandClient.CreatePartyWithResultAsync(Arg.Any<CreateParty>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Directory.ProvisionAgentPartyAsync(
                TenantId, new AgentPartyProvisioningRequest("hexa", "Agent hexa"), CancellationToken.None));
    }

    // Builds a PartyDetail whose PII fields carry obvious sentinels — the adapter must never surface them.
    private static PartyDetail Party(
        bool isActive = true,
        bool isErased = false,
        bool isRestricted = false,
        ProjectionFreshnessStatus? freshness = null,
        string id = PartyId)
        => new()
        {
            Id = id,
            Type = PartyType.Organization,
            DisplayName = "SHOULD-NOT-LEAK-DisplayName",
            SortName = "should-not-leak-sortname",
            IsActive = isActive,
            IsErased = isErased,
            IsRestricted = isRestricted,
            Freshness = freshness is null ? null : ProjectionFreshnessMetadata.Create(freshness.Value),
        };
}
