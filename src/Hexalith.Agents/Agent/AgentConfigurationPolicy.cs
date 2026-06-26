using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Pure, dependency-free configuration and activation-gate rules shared by <see cref="AgentAggregate"/>
/// (create/update structural validation and the activation gate) and <see cref="AgentInspection"/> (status-view
/// instruction presence/validity and the current activation blockers). Centralizing the rules keeps the mutation
/// path and the read path in lock-step so the status view's blockers always match what an activation attempt
/// would reject (AC2).
/// </summary>
/// <remarks>
/// V1 gates only this story's required fields (display name present; Agent Instructions present and valid). The
/// blocker model is additively extensible — later stories (1.4–1.7) append party-identity, provider/model,
/// response/approver, and content-safety gates without reshaping events (AD-2). No method here reads I/O, time, or
/// secrets, and none echoes the raw Agent Instructions text (AD-14).
/// </remarks>
internal static class AgentConfigurationPolicy
{
    /// <summary>Maximum length of the safe display name.</summary>
    internal const int MaxDisplayNameLength = 256;

    /// <summary>Maximum length of the optional safe description.</summary>
    internal const int MaxDescriptionLength = 1024;

    /// <summary>Minimum meaningful length of valid Agent Instructions (present-but-too-short is invalid, not missing).</summary>
    internal const int MinInstructionsLength = 10;

    /// <summary>Maximum length of stored Agent Instructions (a safe storage bound, not a content scheme).</summary>
    internal const int MaxInstructionsLength = 32_000;

    /// <summary>Whether Agent Instructions are present (non-blank).</summary>
    /// <param name="instructions">The Agent Instructions text.</param>
    /// <returns><see langword="true"/> when present.</returns>
    internal static bool HasInstructions(string? instructions)
        => !string.IsNullOrWhiteSpace(instructions);

    /// <summary>Whether present Agent Instructions meet V1 validity (non-blank and within the valid length band).</summary>
    /// <param name="instructions">The Agent Instructions text.</param>
    /// <returns><see langword="true"/> when present and valid.</returns>
    internal static bool AreInstructionsValid(string? instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
        {
            return false;
        }

        int length = instructions.Trim().Length;
        return length is >= MinInstructionsLength and <= MaxInstructionsLength;
    }

    /// <summary>
    /// Validates create/update structural input: blank display name / instructions are tolerated (an incomplete
    /// draft), but over-bound values cannot be stored. Returns a safe classification reason, or
    /// <see langword="null"/> when the input is storable. Never echoes the offending value or instructions text.
    /// </summary>
    /// <param name="displayName">The normalized display name.</param>
    /// <param name="description">The normalized optional description.</param>
    /// <param name="instructions">The normalized Agent Instructions text.</param>
    /// <returns>A safe rejection reason, or <see langword="null"/> when storable.</returns>
    internal static string? ValidateStorableInput(string displayName, string? description, string instructions)
    {
        if (displayName.Length > MaxDisplayNameLength)
        {
            return $"DisplayName must not exceed {MaxDisplayNameLength} characters.";
        }

        if (description is not null && description.Length > MaxDescriptionLength)
        {
            return $"Description must not exceed {MaxDescriptionLength} characters.";
        }

        return instructions.Length > MaxInstructionsLength
            ? $"Instructions must not exceed {MaxInstructionsLength} characters."
            : null;
    }

    /// <summary>
    /// Computes the current activation blockers for an Agent's state (AC2; 1.4 AC4; 1.5 AC2; 1.6 AC1, AC3; 1.7 AC2,
    /// AC4). An empty list means the Agent is activatable as configured. Order is stable and deterministic: display
    /// name → instructions → party identity → provider selection → provider unavailable → response mode → approver
    /// policy missing → approver policy unresolvable → content safety policy missing. Party identity, provider/model
    /// readiness, response mode, approver policy, and content safety are <em>distinct</em> readiness gates — separate
    /// from lifecycle and from the configuration gates.
    /// </summary>
    /// <param name="displayName">The Agent's display name.</param>
    /// <param name="instructions">The Agent's instructions text.</param>
    /// <param name="hasPartyIdentity">Whether a valid Party identity is linked (1.4 AC4).</param>
    /// <param name="hasProviderSelection">Whether a Provider/model has been selected at all (1.5 AC2).</param>
    /// <param name="selectedProviderReady">
    /// Whether the selected Provider/model is currently ready (the trusted catalog verdict was <c>Valid</c>). Only
    /// consulted when <paramref name="hasProviderSelection"/> is set (1.5 AC2).
    /// </param>
    /// <param name="responseMode">The configured Response Mode (<c>Unknown</c> until a mode is chosen; 1.6 AC1).</param>
    /// <param name="hasApproverPolicy">Whether at least one approver source is configured (1.6 AC2). Only consulted in Confirmation mode.</param>
    /// <param name="approverPolicyResolved">
    /// Whether the configured approver policy currently resolves (the trusted approver verdict was <c>Valid</c>).
    /// Only consulted in Confirmation mode when <paramref name="hasApproverPolicy"/> is set (1.6 AC3).
    /// </param>
    /// <param name="hasContentSafetyPolicy">
    /// Whether an active Content Safety Policy is configured (1.7 AC2). A pure state check — content safety is
    /// self-contained Agent state with no external dependency, so (unlike the provider/approver gates) it needs no
    /// trusted verdict and cannot be bypassed by a direct-gateway activation.
    /// </param>
    /// <returns>The specific blockers (empty when none).</returns>
    internal static IReadOnlyList<AgentActivationBlocker> ComputeActivationBlockers(
        string displayName,
        string instructions,
        bool hasPartyIdentity,
        bool hasProviderSelection,
        bool selectedProviderReady,
        AgentResponseMode responseMode,
        bool hasApproverPolicy,
        bool approverPolicyResolved,
        bool hasContentSafetyPolicy)
    {
        var blockers = new List<AgentActivationBlocker>();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            blockers.Add(AgentActivationBlocker.MissingDisplayName);
        }

        if (!HasInstructions(instructions))
        {
            blockers.Add(AgentActivationBlocker.MissingInstructions);
        }
        else if (!AreInstructionsValid(instructions))
        {
            blockers.Add(AgentActivationBlocker.InvalidInstructions);
        }

        if (!hasPartyIdentity)
        {
            blockers.Add(AgentActivationBlocker.MissingPartyIdentity);
        }

        // Provider/model readiness is appended in deterministic order: a missing selection is reported as
        // MissingProviderSelection; a present-but-not-ready selection as ProviderUnavailable (1.5 AC2).
        if (!hasProviderSelection)
        {
            blockers.Add(AgentActivationBlocker.MissingProviderSelection);
        }
        else if (!selectedProviderReady)
        {
            blockers.Add(AgentActivationBlocker.ProviderUnavailable);
        }

        // Response-mode + approver-policy gates are appended last (1.6). A not-yet-chosen mode blocks; Automatic mode
        // requires no approver policy; Confirmation mode requires a configured policy that currently resolves.
        if (responseMode == AgentResponseMode.Unknown)
        {
            blockers.Add(AgentActivationBlocker.MissingResponseMode);
        }
        else if (responseMode == AgentResponseMode.Confirmation)
        {
            if (!hasApproverPolicy)
            {
                blockers.Add(AgentActivationBlocker.MissingApproverPolicy);
            }
            else if (!approverPolicyResolved)
            {
                blockers.Add(AgentActivationBlocker.ApproverPolicyUnresolvable);
            }
        }

        // Content safety is the final Epic 1 gate, appended last (1.7 AC2, AC4). A missing active policy blocks
        // production/production-like enablement; because an empty/invalid policy is rejected at configuration time, a
        // present policy is a valid one and this gate is a pure state check needing no verdict.
        if (!hasContentSafetyPolicy)
        {
            blockers.Add(AgentActivationBlocker.MissingContentSafetyPolicy);
        }

        return blockers;
    }
}
