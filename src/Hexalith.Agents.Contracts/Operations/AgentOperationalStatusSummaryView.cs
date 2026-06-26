using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Safe, display-ready operational-status summary for the authorized operator surface (Story 4.3 AC1, AC4; FR22, FR23,
/// FR25). It exposes ONLY safe enums/ids/ints/ISO-8601 strings for the AC4 launch-monitoring signals: the Agent
/// <see cref="AgentReadiness"/> state, the readiness <see cref="ReadinessBlockers"/> (grouped by recovery action in the
/// UI), the named audit-governance launch-readiness blockers, the canonical <see cref="AuditAvailability"/>, the recent
/// Agent Call <see cref="RecentCallOutcomes"/> counts, the proposal terminal-state/posting <see cref="ProposalOutcomes"/>
/// counts, and the <see cref="PendingProposalCount"/> (reusing the Story 3.2
/// <see cref="AgentInteraction.PendingProposalsResult.PendingCount"/> semantics). It deliberately carries <b>NO</b> raw
/// prompt/generated/edited/context content and <b>NO</b> per-record summary text — every rate is dimensioned only by safe
/// enums/ids/timestamps (AC4 second clause; AD-14). All member names are kept clear of the forbidden secret tokens
/// (<c>Secret</c>/<c>ApiKey</c>/<c>Credential</c>/<c>Password</c>/<c>ConnectionString</c>).
/// </summary>
/// <remarks>
/// The live aggregate projection that computes <see cref="RecentCallOutcomes"/>/<see cref="ProposalOutcomes"/> /
/// terminal-state rates does not exist yet (the Server <c>Projections/</c> folder is <c>.gitkeep</c>-only); binding this
/// view to the EventStore read-model/projection read path is deferred to the operational read-model/topology work
/// (AD-16). The stable view/query/result contracts land here so the UI gateway seam can fail closed against the default
/// DI graph (mirroring Story 3.2's deferred pending-proposal queue). Counts are content-free aggregate rates, never a
/// per-record list — a denied/unavailable read returns no summary at all (the result wrapper fails closed; AD-12).
/// </remarks>
/// <param name="AgentReadiness">The canonical Agent (<c>hexa</c>) readiness state.</param>
/// <param name="ReadinessBlockers">The safe activation blockers preventing callability (empty when callable) — grouped by recovery action in the UI.</param>
/// <param name="AuditGovernanceBlockers">The named launch-readiness blockers from <see cref="AgentAuditGovernanceReadiness"/> (e.g. the content-bearing-audit retention/legal-hold/export/deletion blocker).</param>
/// <param name="AuditAvailability">The canonical audit-evidence availability state (never rendered as success unless <see cref="AuditAvailabilityStatus.AuditAvailable"/>; AD-5).</param>
/// <param name="RecentCallOutcomes">The recent Agent Call outcome counts, dimensioned only by <see cref="AgentCallOperationStatus"/> (AC4).</param>
/// <param name="ProposalOutcomes">The proposal terminal-state and posting outcome counts, dimensioned only by <see cref="ProposalOperationStatus"/> (AC4).</param>
/// <param name="PendingProposalCount">The authorized pending-proposal count (reuses the Story 3.2 <c>PendingProposalsResult.PendingCount</c> semantics; <c>0</c> on a fail-closed read).</param>
/// <param name="GeneratedAt">The optional ISO-8601 timestamp at which the summary was computed (<see langword="null"/> when no live source has produced it yet — the UI renders a "not available yet" affordance, never a fabricated value).</param>
/// <param name="LaunchReadinessBlockers">The named launch-readiness blockers preventing production-like generation (Story 4.4 AC4). A <em>distinct</em> typed list — never merged into <see cref="ReadinessBlockers"/> (typed <see cref="AgentActivationBlocker"/>) or <see cref="AuditGovernanceBlockers"/> (typed <see cref="string"/>). Appended last per AD-17.</param>
public sealed record AgentOperationalStatusSummaryView(
    AgentReadinessStatus AgentReadiness,
    IReadOnlyList<AgentActivationBlocker> ReadinessBlockers,
    IReadOnlyList<string> AuditGovernanceBlockers,
    AuditAvailabilityStatus AuditAvailability,
    IReadOnlyList<AgentCallOutcomeCount> RecentCallOutcomes,
    IReadOnlyList<ProposalOutcomeCount> ProposalOutcomes,
    int PendingProposalCount,
    string? GeneratedAt,
    IReadOnlyList<AgentLaunchReadinessBlocker> LaunchReadinessBlockers);
