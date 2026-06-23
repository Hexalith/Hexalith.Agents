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
    /// Computes the current activation blockers for an Agent's state (AC2). An empty list means the Agent is
    /// activatable as configured. Order is stable (display name, then instructions) for deterministic results.
    /// </summary>
    /// <param name="displayName">The Agent's display name.</param>
    /// <param name="instructions">The Agent's instructions text.</param>
    /// <returns>The specific blockers (empty when none).</returns>
    internal static IReadOnlyList<AgentActivationBlocker> ComputeActivationBlockers(string displayName, string instructions)
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

        return blockers;
    }
}
