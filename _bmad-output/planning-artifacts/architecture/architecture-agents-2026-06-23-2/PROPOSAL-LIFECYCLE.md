# Proposal lifecycle convention

A Confirmation-mode proposal has its own sub-state and append-only version history. The interaction status records the outcome of its latest operation. An edit can therefore fail while the proposal remains pending and the original version remains available.

| Proposal sub-state | Action boundary |
| --- | --- |
| `Pending`, `Edited`, `Regenerated` | An authorized user may act on a pending version. An accepted edit or regeneration appends a version. A failed attempt preserves the existing sub-state and history. A new attempt checks current authority again. |
| `Approved`, `PostingPending`, `PostingFailed` | The approved version is frozen. Posting and reconciliation use its exact identity. A posting failure offers no edit or regeneration. Governed posting recovery follows the posting rules in AD-5 and AD-13. |
| `Posted`, `Rejected`, `Abandoned`, `Expired` | Fresh mutations of the proposal are closed. Preserve prior versions for authorized inspection. A new Agent Call has its own identity. |
| Missing or unknown | Withhold proposal actions. |

The Server orchestrator checks current action authority and assembles the trusted command result. The aggregate checks the stream, exact retries and action preconditions before calling the pure policy. For example, resending an edit whose version already landed produces a no-op. For a fresh edit, the policy maps the trusted result to the event through `Evaluate` and to the reported outcome through `Decide`. Both use the same decision function. [Command-step convention](IMPLEMENTATION-CONVENTIONS.md) · [Edit policy](../../../../src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs)

The proposal detail page owns selection, comparison, action availability and refresh after a completed action. It hosts the existing editor, regenerator and resolution controls. Each control submits through its gateway. Server authorization and aggregate preconditions still apply if a caller bypasses the page or the view goes stale. The current page groups `PostingFailed` with terminal presentations and offers a new Agent Call. Story 7.7 tracks the server-evaluated recovery actions for the approved version. [Story 7.7](../../epics.md#story-77-restore-posting-failed-recovery-and-proposal-state-parity) · [Proposal detail](../../../../src/Hexalith.Agents.UI/Components/Pages/ProposalDetail.razor)

Epic 7 requires a durable user action intent and fresh authority for live actions. An authorized edit preserves the source version and author and records exactly one new version. Approval selects a single preserved version. The workflow records posting intent before the external append and reconciles an uncertain outcome by its original message identity. [Stories 7.1–7.7](../../epics.md#epic-7-complete-confirmation-and-approval) · [Architecture decisions](ARCHITECTURE-SPINE.md)
