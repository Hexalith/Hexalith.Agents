namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// A safe, content-free count of proposal-workflow outcomes dimensioned by one canonical
/// <see cref="ProposalOperationStatus"/> (Story 4.3 AC4). It carries both terminal-state rates
/// (<see cref="ProposalOperationStatus.Rejected"/>/<see cref="ProposalOperationStatus.Abandoned"/>/
/// <see cref="ProposalOperationStatus.Expired"/>) and posting outcomes
/// (<see cref="ProposalOperationStatus.Posted"/>/<see cref="ProposalOperationStatus.PostingPending"/>/
/// <see cref="ProposalOperationStatus.PostingFailed"/>). The count is a telemetry rate dimensioned ONLY by the safe
/// status enum — never by any generated/edited content, id, secret, or per-record summary text (AC4 second clause;
/// AD-14).
/// </summary>
/// <param name="Status">The canonical proposal/posting status the count is dimensioned by.</param>
/// <param name="Count">The authorized count of proposals in this status (zero or more).</param>
public sealed record ProposalOutcomeCount(ProposalOperationStatus Status, int Count);
