using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents;                                       // AgentsAssemblyMarker (domain assembly)
using Hexalith.Agents.Agent;                                 // AgentAggregate, AgentState, AgentInspection
using Hexalith.Agents.AgentInteraction;                      // AgentInteractionAggregate, AgentInteractionState, AgentInteractionAuditInspection
using Hexalith.Agents.Contracts.Agent;                       // AgentInspectionStatus, AgentApproverPolicy, ApproverPolicyBasisDisclosure
using Hexalith.Agents.Contracts.Agent.Commands;              // ConfigureAgentApproverPolicy
using Hexalith.Agents.Contracts.AgentInteraction;            // AgentInteractionStatus, AgentInteractionInspectionStatus, AgentProposalApprovalResult/Outcome, ApproverPolicyValidationStatus
using Hexalith.Agents.Contracts.AgentInteraction.Commands;   // RequestAgentInteraction, ApproveProposedAgentReply
using Hexalith.Agents.Contracts.AgentInteraction.Events;     // AgentInteractionContextReady, AgentOutputGenerated, AgentResponsePosted
using Hexalith.Agents.Contracts.ProviderCatalog;             // ProviderCatalogInspectionStatus
using Hexalith.Agents.ProviderCatalog;                       // ProviderCatalogAggregate, ProviderCatalogState, ProviderCatalogInspection

using Hexalith.EventStore.Contracts.Results;                 // DomainResult

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests.Conformance;

/// <summary>
/// Story 4.5 — consolidated domain governance-gate suite (AC1). This class is the single, requirement-tagged entry
/// point that asserts each AD-17 domain gate end-to-end through the <b>real reflection-dispatch pipeline</b> (the same
/// <see cref="AgentInteractionTestData.ProcessAndApplyAsync{TCommand}"/> the per-story end-to-end suites use). It does
/// NOT re-author the per-story tests: where a gate is already fully proven elsewhere it drives the same data/path or
/// asserts the proving suite is present, so this reads as the "table of contents" of the AD-17 governance gates. Every
/// failure message embeds the governing FR/NFR/AD id (Task 1; AC1 "failures identify the relevant FR, NFR, or UX-DR").
/// </summary>
[Trait(RequirementTraits.Architecture, "AD-17")]
public sealed class GovernanceConformanceTests
{
    private const string ApproverPartyId = "approver-party-001";
    private const string ApprovalMessageId = "approval-message-001";

    /// <summary>A Confirmation-mode request driven through the real pipeline (the proposal-lifecycle gates need it).</summary>
    private static RequestAgentInteraction ConfirmationRequest { get; } = new(
        AgentId, SourceConversationId, CallerPartyId, Prompt, IdempotencyKey, ConfirmationSnapshot, ClientCorrelationId);

    // ===== Gate 1 — Aggregate transition purity (AD-3, NFR-3) =====

    [Fact]
    [Trait(RequirementTraits.Architecture, "AD-3")]
    [Trait(RequirementTraits.Requirement, "NFR-3")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.TransitionPurity)]
    public async Task Aggregate_transition_purity_is_proven_by_deterministic_replay_for_every_aggregate()
    {
        // Meta-assertion (do not re-author per-event replay; reuse the per-aggregate replay suites as the proof):
        // every aggregate must keep a deterministic replay suite proving Handle emits events only (no wall-clock /
        // Guid.NewGuid / I-O) and that applying the emitted events reproduces state deterministically.
        Assembly domainTests = typeof(GovernanceConformanceTests).Assembly;
        Assembly domain = typeof(AgentsAssemblyMarker).Assembly;

        foreach ((string aggregate, string replaySuite) in new[]
        {
            ("AgentAggregate", "AgentStateReplayTests"),
            ("AgentInteractionAggregate", "AgentInteractionStateReplayTests"),
            ("ProviderCatalogAggregate", "ProviderCatalogStateReplayTests"),
        })
        {
            domain.GetTypes().Any(t => t.Name == aggregate).ShouldBeTrue(
                $"AD-3/NFR-3: pure aggregate '{aggregate}' must exist in the domain assembly under purity conformance.");
            domainTests.GetType($"Hexalith.Agents.Tests.{replaySuite}").ShouldNotBeNull(
                $"AD-3/NFR-3: aggregate '{aggregate}' must keep deterministic replay suite '{replaySuite}' "
                + "(Handle emits events only — no wall-clock/Guid.NewGuid/I-O — and replay-through-Apply is deterministic).");
        }

        // Direct determinism check: two independent identical command streams reproduce identical state (a wall-clock or
        // Guid.NewGuid in any Handle would make the two diverge).
        static async Task<AgentInteractionState> DriveToGeneratedAsync()
        {
            var aggregate = new AgentInteractionAggregate();
            var state = new AgentInteractionState();
            await ProcessAndApplyAsync(aggregate, state, ValidRequest());
            await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
            await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
            await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
            return state;
        }

        AgentInteractionState one = await DriveToGeneratedAsync();
        AgentInteractionState two = await DriveToGeneratedAsync();

        string viewOne = JsonSerializer.Serialize(AgentInteractionAuditInspection.GetStatus(one, isAuthorized: true).View);
        string viewTwo = JsonSerializer.Serialize(AgentInteractionAuditInspection.GetStatus(two, isAuthorized: true).View);
        viewOne.ShouldBe(viewTwo, "AD-3/NFR-3: identical command streams must reproduce identical state (no wall-clock/Guid.NewGuid in Handle).");
        one.GeneratedVersions![0].VersionId.ShouldBe(two.GeneratedVersions![0].VersionId,
            "AD-13/AD-3: the generated version id must derive deterministically from the interaction's attempt id.");
    }

    // ===== Gate 2 — Authorization fail-closed (FR-19, FR-20, FR-21, NFR-1, AD-12) =====

    [Fact]
    [Trait(RequirementTraits.Requirement, "FR-19")]
    [Trait(RequirementTraits.Requirement, "FR-20")]
    [Trait(RequirementTraits.Requirement, "FR-21")]
    [Trait(RequirementTraits.Architecture, "AD-12")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.AuthorizationFailClosed)]
    public void Authorization_fails_closed_indistinguishably_for_every_read_inspection()
    {
        // For each aggregate's representative read inspection: an unauthorized read of a PRESENT aggregate and an
        // unauthorized read of an ABSENT aggregate return the same shape (same status, no payload) — the caller can
        // never tell whether the aggregate exists, so authorization fails closed without fingerprinting (AD-12).

        // Agent inspection.
        AgentState presentAgent = AgentTestData.StateWith(AgentTestData.ValidCreate());
        var agentPresent = AgentInspection.GetStatus(presentAgent, isAgentsAdmin: false);
        var agentAbsent = AgentInspection.GetStatus(state: null, isAgentsAdmin: false);
        agentPresent.Status.ShouldBe(AgentInspectionStatus.NotAuthorized, "FR-20/NFR-1/AD-12: an unauthorized Agent read must fail closed.");
        agentPresent.Status.ShouldBe(agentAbsent.Status, "FR-19/NFR-1/AD-12: present vs absent Agent reads must be indistinguishable to an unauthorized caller.");
        agentPresent.Agent.ShouldBeNull("AD-12/AD-14: an unauthorized Agent read must carry no Agent data.");
        agentAbsent.Agent.ShouldBeNull("AD-12/AD-14: an absent-Agent read must carry no Agent data.");

        // AgentInteraction audit/status inspection.
        AgentInteractionState presentInteraction = StateRequested();
        var interactionPresent = AgentInteractionAuditInspection.GetStatus(presentInteraction, isAuthorized: false);
        var interactionAbsent = AgentInteractionAuditInspection.GetStatus(state: null, isAuthorized: false);
        interactionPresent.Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized, "FR-20/NFR-1/AD-12: an unauthorized interaction read must fail closed.");
        interactionPresent.Status.ShouldBe(interactionAbsent.Status, "FR-19/NFR-1/AD-12: present vs absent interaction reads must be indistinguishable to an unauthorized caller.");
        interactionPresent.View.ShouldBeNull("AD-12/AD-14: an unauthorized interaction read must carry no status view.");
        interactionAbsent.View.ShouldBeNull("AD-12/AD-14: an absent-interaction read must carry no status view.");

        // ProviderCatalog inspection.
        ProviderCatalogState presentCatalog = ProviderCatalogTestData.StateWith(ProviderCatalogTestData.ValidCreate());
        var catalogPresent = ProviderCatalogInspection.GetEntry(presentCatalog, isProviderAdmin: false, "openai", "gpt-4o");
        var catalogAbsent = ProviderCatalogInspection.GetEntry(state: null, isProviderAdmin: false, "openai", "gpt-4o");
        catalogPresent.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized, "FR-20/NFR-1/AD-12: an unauthorized provider-catalog read must fail closed.");
        catalogPresent.Status.ShouldBe(catalogAbsent.Status, "FR-19/NFR-1/AD-12: present vs absent provider-catalog reads must be indistinguishable to an unauthorized caller.");
        catalogPresent.Entries.ShouldBeEmpty("AD-12/AD-14: an unauthorized provider-catalog read must carry no entries.");
        catalogAbsent.Entries.ShouldBeEmpty("AD-12/AD-14: an absent provider-catalog read must carry no entries.");
    }

    // ===== Gate 3 — Proposal version immutability (FR-14, AD-5) =====

    [Fact]
    [Trait(RequirementTraits.Requirement, "FR-14")]
    [Trait(RequirementTraits.Architecture, "AD-5")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.ProposalImmutability)]
    public async Task Every_proposal_version_is_preserved_unchanged_through_the_terminal_approval()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        // generate -> propose -> edit -> regenerate: each appends an immutable version (append-only; AD-5).
        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));
        await ProcessAndApplyAsync(aggregate, state, EditCommand(EditedProposalResult()));
        DomainResult regenerated = await ProcessAndApplyAsync(aggregate, state, RegenerateCommand(RegeneratedProposalResult()));
        regenerated.IsSuccess.ShouldBeTrue("FR-14/AD-5: a regeneration over a pending edited proposal must append, not replace.");

        string[] preserved = state.GeneratedVersions.ShouldNotBeNull().Select(v => v.VersionId).ToArray();
        preserved.ShouldBe([PostedVersionId, EditedVersionId, RegeneratedVersionId],
            "FR-14/AD-5: generated, edited, and regenerated versions must all be preserved append-only in order.");

        // approve exactly one preserved version -> terminal transition.
        await ProcessAndApplyAsync(aggregate, state, ApproveCommand(RegeneratedVersionId));
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPosted, "FR-17: approving a preserved version posts it.");

        // After the terminal transition every prior version id is still present and unchanged.
        state.GeneratedVersions!.Select(v => v.VersionId).ShouldBe(preserved,
            "FR-14/AD-5: every prior proposal version id must survive the terminal approval unchanged.");
        state.ApprovedVersionId.ShouldBe(RegeneratedVersionId, "FR-14/AD-5: approval selects exactly one preserved version.");
        preserved.ShouldContain(state.ApprovedVersionId, "FR-14/AD-5: the approved version must be exactly one of the preserved versions.");
    }

    // ===== Gate 4 — Replay / idempotency (AD-13) =====

    [Fact]
    [Trait(RequirementTraits.Architecture, "AD-13")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.ReplayIdempotency)]
    public async Task Re_asserted_equal_commands_no_op_and_posting_ids_derive_deterministically()
    {
        // (a) Re-asserting an identical Agent configuration command is an idempotent no-op (no duplicate event).
        var agentAggregate = new AgentAggregate();
        AgentState agentState = AgentTestData.StateWithApproverPolicy(AgentTestData.ValidCreate());
        DomainResult agentNoOp = await AgentTestData.ProcessAndApplyAsync(
            agentAggregate, agentState, new ConfigureAgentApproverPolicy(AgentTestData.SampleApproverPolicy));
        agentNoOp.IsNoOp.ShouldBeTrue("AD-13: re-asserting an identical Agent configuration command must be an idempotent no-op.");

        // (b) Re-issuing the same deterministic posting (same MessageId / idempotency key) is a clean no-op — no
        //     duplicate Conversation Message.
        var interactionAggregate = new AgentInteractionAggregate();
        AgentInteractionState approved = StateProposalCreated();
        ApproveProposedAgentReply approve = ApproveCommand(PostedVersionId);
        await ProcessAndApplyAsync(interactionAggregate, approved, approve);
        DomainResult retry = await ProcessAndApplyAsync(interactionAggregate, approved, approve);
        retry.IsNoOp.ShouldBeTrue("AD-13: re-issuing the same deterministic posting must be a no-op — no duplicate Conversation Message.");

        // (c) The posting MessageId + idempotency key derive deterministically from the interaction + approved version:
        //     two independent derivations from identical inputs are equal.
        var firstAggregate = new AgentInteractionAggregate();
        AgentInteractionState first = StateProposalCreated();
        await ProcessAndApplyAsync(firstAggregate, first, ApproveCommand(PostedVersionId));
        var secondAggregate = new AgentInteractionAggregate();
        AgentInteractionState second = StateProposalCreated();
        await ProcessAndApplyAsync(secondAggregate, second, ApproveCommand(PostedVersionId));

        first.ApprovalPostingMessageId.ShouldBe(second.ApprovalPostingMessageId, "AD-13: the posting MessageId must derive deterministically from AgentInteractionId + approved VersionId.");
        first.ApprovalPostingIdempotencyKey.ShouldBe(second.ApprovalPostingIdempotencyKey, "AD-13: the posting idempotency key must derive deterministically from AgentInteractionId + approved VersionId.");
        first.ApprovedVersionId.ShouldBe(second.ApprovedVersionId, "AD-13: independent runs over identical inputs must approve the same version.");
    }

    // ===== Gate 5 — Tenant isolation (FR-19, NFR-2, AD-12) =====

    [Fact]
    [Trait(RequirementTraits.Requirement, "FR-19")]
    [Trait(RequirementTraits.Requirement, "NFR-2")]
    [Trait(RequirementTraits.Architecture, "AD-12")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.TenantIsolation)]
    public void Cross_tenant_audit_read_fails_closed_indistinguishably_from_not_found()
    {
        // An interaction exists under tenant A. A reader scoped to a different tenant is unauthorized for it, so the
        // audit/status surface returns no view — the same "no record" shape an authorized reader sees for a genuinely
        // absent interaction. The cross-tenant reader therefore learns nothing about existence (FR-19/NFR-2/AD-12).
        AgentInteractionState tenantAInteraction = StateRequested();

        var crossTenantRead = AgentInteractionAuditInspection.GetStatus(tenantAInteraction, isAuthorized: false);
        var notFoundToAuthorized = AgentInteractionAuditInspection.GetStatus(state: null, isAuthorized: true);

        crossTenantRead.Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized, "FR-19/NFR-2/AD-12: a cross-tenant audit read must fail closed.");
        crossTenantRead.View.ShouldBeNull("FR-19/NFR-2/AD-12: a cross-tenant audit read must expose no status view.");
        notFoundToAuthorized.View.ShouldBeNull("FR-19/NFR-2/AD-12: an absent interaction exposes no status view.");
        // No payload is returned in either case: the cross-tenant reader cannot distinguish "exists in another tenant"
        // from "does not exist".
        (crossTenantRead.View is null && notFoundToAuthorized.View is null).ShouldBeTrue(
            "FR-19/NFR-2/AD-12: cross-tenant existence must be indistinguishable from not-found.");
    }

    // ===== Gate 6 — Context-too-large blocking (FR-9, NFR-8, AD-11) =====

    [Fact]
    [Trait(RequirementTraits.Requirement, "FR-9")]
    [Trait(RequirementTraits.Requirement, "NFR-8")]
    [Trait(RequirementTraits.Architecture, "AD-11")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.ContextTooLarge)]
    public async Task Context_too_large_blocks_with_no_version_proposal_or_posting()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ValidRequest());
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        DomainResult context = await ProcessAndApplyAsync(aggregate, state, ContextCommand(OversizedMeasurement()));

        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked, "FR-9/NFR-8/AD-11: an over-budget context with no approved bounded behavior must block.");
        context.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty("FR-9/AD-11: context-too-large must never silently truncate to a ready context.");

        // No provider call: a generate over a blocked context fails closed and creates no version, proposal, or posting.
        DomainResult generate = await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        generate.Events.OfType<AgentOutputGenerated>().ShouldBeEmpty("FR-9/NFR-8/AD-11: a blocked context must produce no generated version.");
        state.GeneratedVersions.ShouldBeNull("FR-9/NFR-8/AD-11: a blocked context must leave no generated version.");
        state.ProposalState.ShouldBeNull("FR-9/NFR-8/AD-11: a blocked context must create no proposal.");
        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked, "FR-9/NFR-8/AD-11: a blocked interaction must stay blocked (no posting).");
    }

    // ===== Gate 7 — Content Safety enforcement (FR-26, FR-27, NFR-7, AD-14) =====

    [Fact]
    [Trait(RequirementTraits.Requirement, "FR-26")]
    [Trait(RequirementTraits.Requirement, "FR-27")]
    [Trait(RequirementTraits.Requirement, "NFR-7")]
    [Trait(RequirementTraits.Architecture, "AD-14")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.ContentSafety)]
    public async Task Content_safety_block_prevents_any_conversation_side_effect()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ValidRequest());
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));

        // A safety-blocked generation (the result deliberately still carries unsafe content) must create no version.
        DomainResult generate = await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SafetyBlockedGenerationResult()));
        generate.Events.OfType<AgentOutputGenerated>().ShouldBeEmpty("FR-26/FR-27/AD-14: safety-blocked content must never become a generated version.");
        state.GeneratedVersions.ShouldBeNull("FR-27/NFR-7/AD-14: a safety block must leave no postable/approvable version.");
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed, "FR-27/NFR-7: a safety block records a fail-closed SafetyFailed status.");

        // No Conversation side effect: a post over the safety-blocked interaction is refused and posts no message.
        DomainResult post = await ProcessAndApplyAsync(aggregate, state, PostCommand(PostedResult()));
        post.Events.OfType<AgentResponsePosted>().ShouldBeEmpty("FR-27/AD-14: a safety-blocked interaction must post no Conversation Message.");
        state.GeneratedVersions.ShouldBeNull("FR-27/NFR-7/AD-14: a safety block must remain version-free after a refused post.");
    }

    // ===== Gate 8 — Audit completeness for every posted response (FR-24, NFR-5, AD-17) =====

    [Fact]
    [Trait(RequirementTraits.Requirement, "FR-24")]
    [Trait(RequirementTraits.Requirement, "NFR-5")]
    [Trait(RequirementTraits.Architecture, "AD-17")]
    [Trait(RequirementTraits.Architecture, "AD-14")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.AuditCompleteness)]
    public async Task Automatic_mode_posted_response_is_audit_traceable_and_content_free()
    {
        // The Confirmation-mode counterpart is proven by AgentInteractionAuditTraceabilityE2ETests (asserted present
        // below). This adds the missing Automatic-mode counterpart: an automatically posted response is traceable
        // MessageId <- AgentInteractionId + VersionId and the audit surface carries no prompt/generated content (AD-14).
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ValidRequest());
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        await ProcessAndApplyAsync(aggregate, state, PostCommand(PostedResult()));
        state.Status.ShouldBe(AgentInteractionStatus.Posted, "FR-11/FR-24: an Automatic-mode interaction terminates Posted.");

        object? statusView = AgentInteractionAuditInspection.GetStatus(state, isAuthorized: true).View
            .ShouldNotBeNull("FR-24/NFR-5: a posted response must expose an authorized audit status view.");
        object postingEvidence = AgentInteractionAuditInspection.GetPostingEvidence(state, isAuthorized: true)
            .ShouldNotBeNull("FR-24/NFR-5/AD-17: every posted response must record posting evidence.");

        string serialized = JsonSerializer.Serialize(new { statusView, postingEvidence });
        serialized.ShouldContain(InteractionId, customMessage: "FR-24/AD-17: posting evidence must trace back to the AgentInteractionId.");
        serialized.ShouldContain(PostedMessageId, customMessage: "FR-24/AD-17: posting evidence must carry the Conversation MessageId.");
        serialized.ShouldContain(PostedVersionId, customMessage: "FR-24/AD-17: posting evidence must link the posted version id (MessageId <- AgentInteractionId + VersionId).");
        serialized.ShouldNotContain(Prompt, customMessage: "AD-14: the audit surface must never carry the caller prompt.");
        serialized.ShouldNotContain(GeneratedContentText, customMessage: "AD-14: the audit surface must never carry generated content.");

        typeof(GovernanceConformanceTests).Assembly.GetType("Hexalith.Agents.Tests.AgentInteractionAuditTraceabilityE2ETests")
            .ShouldNotBeNull("FR-24/AD-17: Confirmation-mode posted-response audit traceability must remain proven by AgentInteractionAuditTraceabilityE2ETests.");
    }

    private static ApproveProposedAgentReply ApproveCommand(string selectedVersionId)
        => new(
            InteractionId,
            new AgentProposalApprovalResult(
                AgentProposalApprovalOutcome.Posted,
                SampleProposalId,
                SourceConversationId,
                selectedVersionId,
                ApproverPartyId,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ApproverPolicyValidationStatus.Valid,
                ApproverPolicyBasisDisclosure.UserVisible,
                AgentPartyId,
                ApprovalMessageId,
                IdempotencyKey,
                ApprovalMessageId));
}
