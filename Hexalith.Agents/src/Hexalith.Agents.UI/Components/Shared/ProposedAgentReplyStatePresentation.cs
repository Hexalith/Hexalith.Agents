using System;
using System.Globalization;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.FrontComposer.Shell.Components.Icons;

using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from the safe <see cref="ProposedAgentReplyState"/> to its Fluent semantic badge role
/// + curated icon + whole-string localization key, plus a deterministic coarse "age" bucket helper (AC1). Bind status to
/// a Fluent role, never to a hex value (DESIGN Colors). The mapping derives only the coarse, content-free classification
/// and never exposes any generated content, secret, id, or PII (AD-14). Mirrors <see cref="AgentCallStatusPresentation"/>
/// and is unit-testable in isolation (no bUnit). Every switch is <b>total</b> so the reserved 3.3–3.6 states render
/// through a safe default until those stories add explicit roles.
/// </summary>
public static class ProposedAgentReplyStatePresentation
{
    /// <summary>
    /// Maps a proposal state to its Fluent semantic badge role. <c>Pending</c> (awaiting approval), <c>Edited</c> (edited,
    /// still awaiting approval), and <c>Regenerated</c> (regenerated, still awaiting approval — the in-progress/pending set)
    /// are Informative (DESIGN <c>status-informative</c>); the sentinel and every reserved 3.5–3.6 state map through the
    /// Subtle total default until their owning stories add explicit roles (mirroring
    /// <see cref="AgentCallStatusPresentation.MapStatus"/>'s totality). Brand/Success are never used for a not-yet-resolved
    /// proposal.
    /// </summary>
    /// <param name="state">The safe proposal state.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(ProposedAgentReplyState state)
        => state switch
        {
            ProposedAgentReplyState.Pending => BadgeColor.Informative,
            ProposedAgentReplyState.Edited => BadgeColor.Informative,
            ProposedAgentReplyState.Regenerated => BadgeColor.Informative,
            ProposedAgentReplyState.Unknown => BadgeColor.Subtle,
            _ => BadgeColor.Subtle,
        };

    /// <summary>
    /// Maps a proposal state to its icon, composed ONLY from the curated <see cref="FcFluentIcons"/> factory. <c>Pending</c>
    /// reuses the in-flight <see cref="FcFluentIcons.ArrowSync16"/> (no clock glyph exists in the curated set, exactly as
    /// the call-status/readiness badges reuse curated glyphs); <c>Edited</c> uses the curated <see cref="FcFluentIcons.Edit16"/>;
    /// every other (incl. reserved) state maps through the question-mark total default.
    /// </summary>
    /// <param name="state">The safe proposal state.</param>
    /// <returns>The Fluent icon for the state.</returns>
    public static Icon IconFor(ProposedAgentReplyState state)
        => state switch
        {
            ProposedAgentReplyState.Pending => FcFluentIcons.ArrowSync16(),
            ProposedAgentReplyState.Edited => FcFluentIcons.Edit16(),
            ProposedAgentReplyState.Regenerated => FcFluentIcons.ArrowSync16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for a proposal-state label (UX-DR14).</summary>
    /// <param name="state">The safe proposal state.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(ProposedAgentReplyState state)
        => $"Agents.ProposalState.Label.{state}";

    /// <summary>
    /// The whole-string localization key for the coarse "age" bucket of a proposal, deterministic given
    /// <paramref name="now"/> so component tests render a stable value (no <see cref="DateTimeOffset.UtcNow"/> inside the
    /// component path — avoids flaky tests). It derives only a coarse bucket from the optional ISO-8601 creation
    /// timestamp and never derives a secret, id, or PII (AD-14). A null/unparseable timestamp returns the safe
    /// "unknown age" bucket; a future timestamp clamps to the freshest bucket.
    /// </summary>
    /// <param name="createdAtIso">The optional ISO-8601 creation timestamp (<see langword="null"/> when unavailable).</param>
    /// <param name="now">The reference "now" (pass <c>TimeProvider.GetUtcNow()</c> from the host).</param>
    /// <returns>The whole-string age-bucket resource key.</returns>
    public static string AgeLabelKeyOrText(string? createdAtIso, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(createdAtIso)
            || !DateTimeOffset.TryParse(
                createdAtIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset createdAt))
        {
            return "Agents.ProposalQueue.Age.Unknown";
        }

        TimeSpan age = now - createdAt;
        if (age < TimeSpan.FromHours(1))
        {
            return "Agents.ProposalQueue.Age.LessThanHour";
        }

        if (age < TimeSpan.FromDays(1))
        {
            return "Agents.ProposalQueue.Age.Today";
        }

        return age < TimeSpan.FromDays(7)
            ? "Agents.ProposalQueue.Age.ThisWeek"
            : "Agents.ProposalQueue.Age.Older";
    }
}
