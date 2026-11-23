namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Records the scalar terms of an exactly measured Conversation context budget.</summary>
/// <param name="FullContextTokenCount">The measured token count, including zero for a measured empty payload.</param>
/// <param name="ProviderCapabilityVersion">The admitted model capability version used for measurement.</param>
/// <param name="ContextWindowTokenLimit">The admitted model context-window token limit.</param>
/// <param name="ReservedOutputTokenCount">The admitted output allowance reserved before fitting context.</param>
/// <param name="SafetyMarginPercent">The percentage of the window reserved as a safety margin.</param>
/// <param name="SafetyMarginTokenCount">The safety margin rounded up to whole tokens.</param>
/// <param name="AvailableContextTokenBudget">The remaining budget after reserves, which can be negative.</param>
public sealed record AgentAttemptContextBudgetEvidence(
    int FullContextTokenCount,
    int ProviderCapabilityVersion,
    int ContextWindowTokenLimit,
    int ReservedOutputTokenCount,
    int SafetyMarginPercent,
    int SafetyMarginTokenCount,
    int AvailableContextTokenBudget);
