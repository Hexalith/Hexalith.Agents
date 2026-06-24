using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Resources;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC5 / Task 3 — every status label, blocker reason, response-mode label, disclosure, source kind, provider/model
/// state, configuration state, capability, and surface state is one complete localizable whole string (UX-DR14), and
/// a key exists for EVERY enum value in BOTH cultures. The component tests use a key-returning stub localizer, so
/// they cannot detect a missing .resx entry; these tests bind to the real embedded resources to close that gap.
/// </summary>
public sealed class LocalizationResourceTests
{
    private static readonly ResourceManager _resources = new(
        "Hexalith.Agents.UI.Resources.AgentsResources",
        typeof(AgentsResources).Assembly);

    /// <summary>Every resource key the setup surfaces resolve dynamically from an enum value or surface kind.</summary>
    public static IEnumerable<string> EnumDerivedKeys()
    {
        foreach (AgentReadinessState state in Enum.GetValues<AgentReadinessState>())
        {
            yield return AgentReadiness.LabelKeyFor(state);
        }

        foreach (ProviderReadinessState state in Enum.GetValues<ProviderReadinessState>())
        {
            yield return AgentReadiness.LabelKeyFor(state);
        }

        foreach (AgentActivationBlocker blocker in Enum.GetValues<AgentActivationBlocker>())
        {
            yield return AgentReadiness.BlockerKeyFor(blocker);
        }

        foreach (AgentLifecycleStatus lifecycle in Enum.GetValues<AgentLifecycleStatus>())
        {
            yield return $"Agents.Lifecycle.{lifecycle}";
        }

        foreach (AgentResponseMode mode in Enum.GetValues<AgentResponseMode>())
        {
            yield return $"Agents.ResponseMode.{mode}";
        }

        foreach (ApproverPolicyBasisDisclosure disclosure in Enum.GetValues<ApproverPolicyBasisDisclosure>())
        {
            yield return $"Agents.ApproverPolicy.Disclosure.{disclosure}";
        }

        foreach (ApproverPolicySourceKind kind in Enum.GetValues<ApproverPolicySourceKind>())
        {
            yield return $"Agents.ApproverPolicy.SourceKind.{kind}";
        }

        foreach (ProviderModelStatus status in Enum.GetValues<ProviderModelStatus>())
        {
            yield return $"Agents.ProviderCatalog.ModelStatus.{status}";
        }

        foreach (ProviderConfigurationState state in Enum.GetValues<ProviderConfigurationState>())
        {
            yield return $"Agents.ProviderCatalog.ConfigurationState.{state}";
        }

        foreach (ProviderModelCapabilityFlags flag in Enum.GetValues<ProviderModelCapabilityFlags>())
        {
            yield return $"Agents.ProviderCatalog.Capability.{flag}";
        }

        yield return "Agents.ProviderCatalog.Capability.TextGeneration";

        foreach (AgentSurfaceKind kind in Enum.GetValues<AgentSurfaceKind>())
        {
            yield return $"Agents.Surface.{kind}.Title";
            yield return $"Agents.Surface.{kind}.Message";
        }

        // Story 2.6 — call-status labels + coarse reasons (one whole string per AgentCallStatus value; UX-DR14).
        foreach (AgentCallStatus state in Enum.GetValues<AgentCallStatus>())
        {
            yield return AgentCallStatusPresentation.LabelKeyFor(state);
            if (AgentCallStatusPresentation.ReasonKeyFor(state) is { } reason)
            {
                yield return reason;
            }
        }

        // Story 2.6 — fine-grained safe reasons (one whole string per reason-enum value; UX-DR14).
        foreach (AgentInteractionContextBlockReason r in Enum.GetValues<AgentInteractionContextBlockReason>())
        {
            yield return AgentCallStatusPresentation.ReasonKeyFor(r);
        }

        foreach (AgentOutputGenerationFailureReason r in Enum.GetValues<AgentOutputGenerationFailureReason>())
        {
            yield return AgentCallStatusPresentation.ReasonKeyFor(r);
        }

        foreach (AgentResponsePostingFailureReason r in Enum.GetValues<AgentResponsePostingFailureReason>())
        {
            yield return AgentCallStatusPresentation.ReasonKeyFor(r);
        }

        foreach (AgentInteractionGateOutcome o in Enum.GetValues<AgentInteractionGateOutcome>())
        {
            yield return AgentCallStatusPresentation.ReasonKeyFor(o);
        }

        foreach (AgentInteractionGateCheck c in Enum.GetValues<AgentInteractionGateCheck>())
        {
            yield return AgentCallStatusPresentation.ReasonKeyFor(c);
        }

        // Story 2.6 — nav entry, page header, invocation panel, and feedback whole strings.
        yield return "Agents.Navigation.ConversationCall";
        yield return "Agents.ConversationCall.Title";
        yield return "Agents.ConversationCall.Eyebrow";
        yield return "Agents.ConversationCall.Description";
        yield return "Agents.ConversationCall.Panel.Label";
        yield return "Agents.ConversationCall.Panel.CallLabel";
        yield return "Agents.ConversationCall.Panel.AgentName";
        yield return "Agents.ConversationCall.Panel.Caller.Label";
        yield return "Agents.ConversationCall.Panel.Caller.You";
        yield return "Agents.ConversationCall.Panel.Agent.Label";
        yield return "Agents.ConversationCall.Panel.SourceConversation.Label";
        yield return "Agents.ConversationCall.Panel.Prompt.Label";
        yield return "Agents.ConversationCall.Panel.Prompt.Placeholder";
        yield return "Agents.ConversationCall.Panel.ResponseMode.Label";
        yield return "Agents.ConversationCall.Panel.Implication.Automatic";
        yield return "Agents.ConversationCall.Panel.Implication.Confirmation";
        yield return "Agents.ConversationCall.Panel.Submit";
        yield return "Agents.ConversationCall.Panel.Cancel";
        yield return "Agents.ConversationCall.Panel.Status.Calling";
        yield return "Agents.ConversationCall.Panel.Status.Generating";
        yield return "Agents.ConversationCall.Panel.Status.Posting";
        yield return "Agents.ConversationCall.Panel.Status.Posted";
        yield return "Agents.ConversationCall.Panel.Status.Failed";
        yield return "Agents.ConversationCall.Feedback.Unavailable";
    }

    public static IEnumerable<object[]> EnumDerivedKeyCases() => EnumDerivedKeys().Distinct().Select(key => new object[] { key });

    [Theory]
    [MemberData(nameof(EnumDerivedKeyCases))]
    public void Every_enum_derived_key_resolves_to_a_non_empty_english_whole_string(string key)
    {
        string? value = _resources.GetString(key, CultureInfo.InvariantCulture);
        value.ShouldNotBeNullOrWhiteSpace($"the English .resx is missing a whole string for '{key}' (UX-DR14 / Task 3)");
    }

    [Fact]
    public void Every_enum_derived_key_is_translated_in_french()
    {
        ResourceSet? french = _resources.GetResourceSet(CultureInfo.GetCultureInfo("fr"), createIfNotExists: true, tryParents: false);
        french.ShouldNotBeNull("the French satellite resources (AgentsResources.fr) must be built and discoverable");

        List<string> missing = EnumDerivedKeys()
            .Distinct()
            .Where(key => string.IsNullOrWhiteSpace(french!.GetString(key)))
            .ToList();

        missing.ShouldBeEmpty($"the French .resx is missing whole strings for: {string.Join(", ", missing)}");
    }
}
