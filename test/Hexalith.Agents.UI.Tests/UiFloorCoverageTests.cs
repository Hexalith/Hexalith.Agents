using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;   // ProposedAgentReplyState
using Hexalith.Agents.Contracts.Operations;          // AuditAvailabilityStatus
using Hexalith.Agents.UI.Components.Shared;          // AgentSurfaceKind, AgentReadiness(+State), AgentCallStatus(+Presentation), ProposedAgentReplyStatePresentation, OperationalStatusPresentation, ProposalApprover
using Hexalith.Agents.UI.Composition;                // AgentsFrontComposerRegistration
using Hexalith.Agents.UI.Resources;                  // AgentsResources
using Hexalith.Agents.UI.Tests.Conformance;          // RequirementTraits

using Microsoft.AspNetCore.Components;               // RouteAttribute

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 4.5 — FrontComposer UI-floor coverage meta-test (AC3). The real gap the per-page suites cannot close on their
/// own: a <b>future page must not silently skip the UX floor</b>. This enumerates every routable Agents page and asserts
/// each is reachable through a policy-gated nav entry (UX-DR1/41); confirms the 8 canonical grid-surface states (UX-DR30),
/// the five canonical state families' complete 1:1 localized mapping (UX-DR25–29), and the presence of the reused floor
/// suites (accessibility, badge, localization, nav, fail-closed gateway, composition); and proves the constrained-viewport
/// fail-closed guard on the high-impact approve/post action (UX-DR40). Every failure message cites the missing page + UX-DR.
/// </summary>
public sealed class UiFloorCoverageTests : AgentsTestContext
{
    private static readonly ResourceManager _resources = new(
        "Hexalith.Agents.UI.Resources.AgentsResources",
        typeof(AgentsResources).Assembly);

    // ===== UX-DR1/41 — the page-coverage gap: every page reachable through a policy-gated nav entry =====

    [Fact]
    [Trait(RequirementTraits.UxRequirement, "UX-DR1")]
    [Trait(RequirementTraits.UxRequirement, "UX-DR41")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.UiFloor)]
    public void Every_routable_agents_page_is_reachable_through_a_policy_gated_nav_entry()
    {
        CapturingFrontComposerRegistry registry = new();
        AgentsFrontComposerRegistration.RegisterDomain(registry);

        registry.NavEntries.ShouldAllBe(
            entry => !string.IsNullOrWhiteSpace(entry.RequiredPolicy),
            "UX-DR1/41: every Agents nav entry must be policy-gated — an ungated link leaks records (AD-12).");

        HashSet<string> navHrefs = registry.NavEntries.Select(entry => entry.Href).ToHashSet(StringComparer.Ordinal);

        Type[] pages = RoutablePages();
        pages.ShouldNotBeEmpty("UX-DR1: expected to discover the routable Agents pages for the floor-coverage meta-test.");

        foreach (Type page in pages)
        {
            foreach (RouteAttribute route in page.GetCustomAttributes<RouteAttribute>(inherit: false))
            {
                string template = route.Template;
                int parameterStart = template.IndexOf("/{", StringComparison.Ordinal);
                string reachableThrough = parameterStart >= 0 ? template[..parameterStart] : template;

                navHrefs.ShouldContain(
                    reachableThrough,
                    $"UX-DR1/41: Agents page '{page.Name}' route '{template}' is not reachable through a policy-gated nav entry — a page must not silently skip the FrontComposer floor.");
            }
        }
    }

    // ===== UX-DR15/30 — every routable page composes the shell layout AND the surface-state floor (per page) =====

    [Fact]
    [Trait(RequirementTraits.UxRequirement, "UX-DR15")]
    [Trait(RequirementTraits.UxRequirement, "UX-DR30")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.UiFloor)]
    public void Every_routable_agents_page_composes_the_shell_layout_and_surface_state_floor()
    {
        // Nav-reachability alone (above) does not stop a future page from skipping the *layout/state* floor: a page could
        // be nav-listed yet render its own markup without the FrontComposer shell (UX-DR15 page measures) or the canonical
        // surface states (UX-DR30). Enforce both per page from source so a new page cannot silently drop FcPageLayout or
        // AgentSurfaceState. Every current page satisfies this; the guard is for the next one (the AC3 "no page silently
        // skips the floor" intent — the nav test covers reachability, this covers composition).
        string pagesDirectory = Path.Combine(SourceRoot(), "Hexalith.Agents.UI", "Components", "Pages");
        Directory.Exists(pagesDirectory).ShouldBeTrue($"UX-DR15/30: expected the Agents Pages source directory at '{pagesDirectory}'.");

        Type[] pages = RoutablePages();
        pages.ShouldNotBeEmpty("UX-DR30: expected to discover routable Agents pages for the floor-composition meta-test.");

        foreach (Type page in pages)
        {
            string source = Path.Combine(pagesDirectory, $"{page.Name}.razor");
            File.Exists(source).ShouldBeTrue($"UX-DR15/30: routable page '{page.Name}' must have a '.razor' source at '{source}'.");

            string markup = File.ReadAllText(source);
            markup.ShouldContain("FcPageLayout",
                customMessage: $"UX-DR15: Agents page '{page.Name}' must compose the FrontComposer shell layout (FcPageLayout) — a page must not silently skip the floor.");
            markup.ShouldContain("AgentSurfaceState",
                customMessage: $"UX-DR30: Agents page '{page.Name}' must render its non-success states through AgentSurfaceState (the 8 canonical grid-surface states) — a page must not silently skip the floor.");
        }
    }

    // ===== UX-DR30 — the 8 canonical grid/list surface states (the floor state vocabulary) =====

    [Fact]
    [Trait(RequirementTraits.UxRequirement, "UX-DR30")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.UiFloor)]
    public void The_eight_canonical_grid_surface_states_are_present()
    {
        string[] expected =
        [
            nameof(AgentSurfaceKind.Loading),
            nameof(AgentSurfaceKind.Empty),
            nameof(AgentSurfaceKind.FilteredEmpty),
            nameof(AgentSurfaceKind.Error),
            nameof(AgentSurfaceKind.PermissionDenied),
            nameof(AgentSurfaceKind.Stale),
            nameof(AgentSurfaceKind.Degraded),
            nameof(AgentSurfaceKind.Unavailable),
        ];
        string[] actual = Enum.GetNames<AgentSurfaceKind>();

        foreach (string state in expected)
        {
            actual.ShouldContain(state, $"UX-DR30: the grid/list surface floor must keep the '{state}' state.");
        }

        actual.Length.ShouldBe(expected.Length,
            "UX-DR30: AgentSurfaceKind must define exactly the 8 floor states (loading/empty/filtered-empty/error/permission-denied + the stale/degraded/unavailable freshness split).");
    }

    // ===== UX-DR floor — the reused per-surface floor suites stay present =====

    [Fact]
    [Trait(RequirementTraits.Architecture, "AD-15")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.UiFloor)]
    public void The_ux_floor_test_suites_cover_every_agents_surface()
    {
        Assembly uiTests = typeof(UiFloorCoverageTests).Assembly;
        foreach (string suite in new[]
        {
            "AccessibilityTests",        // UX-DR32/33/36/37 — skip links, landmarks, focus, live regions
            "BadgeConformanceTests",     // UX-DR11/12/22 — color + icon + visible text, whole-string localization
            "AgentsNavigationTests",     // UX-DR1/41 — policy-gated, ordered nav entries
            "LocalizationResourceTests", // UX-DR14 — en/fr parity for every enum/surface key
            "DeferredGatewayTests",      // AD-12 — fail-closed read surfaces
            "AgentsUiCompositionTests",  // gateway DI composition seam (scoped gateways + deferred placeholders)
        })
        {
            uiTests.GetType($"Hexalith.Agents.UI.Tests.{suite}").ShouldNotBeNull(
                $"UX-DR floor: the '{suite}' floor suite must remain present so every Agents surface keeps its floor coverage.");
        }
    }

    // ===== UX-DR25–29 — the five canonical state families: complete, 1:1, localized =====

    [Fact]
    [Trait(RequirementTraits.UxRequirement, "UX-DR25")]
    [Trait(RequirementTraits.UxRequirement, "UX-DR26")]
    [Trait(RequirementTraits.UxRequirement, "UX-DR27")]
    [Trait(RequirementTraits.UxRequirement, "UX-DR28")]
    [Trait(RequirementTraits.UxRequirement, "UX-DR29")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.UiFloor)]
    public void Every_canonical_state_family_is_completely_mapped_and_localized()
    {
        AssertFamilyMapped("Agent readiness (UX-DR25)", Enum.GetValues<AgentReadinessState>().Select(AgentReadiness.LabelKeyFor));
        AssertFamilyMapped("Provider/model (UX-DR26)", Enum.GetValues<ProviderReadinessState>().Select(AgentReadiness.LabelKeyFor));
        AssertFamilyMapped("Agent Call (UX-DR27)", Enum.GetValues<AgentCallStatus>().Select(AgentCallStatusPresentation.LabelKeyFor));
        AssertFamilyMapped("Proposal lifecycle (UX-DR28)", Enum.GetValues<ProposedAgentReplyState>().Select(ProposedAgentReplyStatePresentation.LabelKeyFor));
        AssertFamilyMapped("Audit availability (UX-DR29)", Enum.GetValues<AuditAvailabilityStatus>().Select(OperationalStatusPresentation.LabelKeyFor));
    }

    // ===== UX-DR40 — constrained-viewport fail-closed on the high-impact approve/post action =====

    [Fact]
    [Trait(RequirementTraits.UxRequirement, "UX-DR40")]
    [Trait(RequirementTraits.Gate, RequirementTraits.Gates.UiFloor)]
    public void Constrained_viewport_makes_proposal_approval_unavailable_with_a_visible_reason()
    {
        // Constrained: the high-impact approve/post action is unavailable, surfaced through a focusable, aria-described
        // reason (never a bare disabled control or a color/animation-only signal); review-only access stays.
        IRenderedComponent<ProposalApprover> constrained = Render<ProposalApprover>(parameters => parameters
            .Add(approver => approver.CanApprove, true)
            .Add(approver => approver.Constrained, true)
            .Add(approver => approver.SelectedVersionId, "version-1"));

        constrained.FindAll("[data-testid='proposal-approver-approve']")
            .ShouldBeEmpty("UX-DR40: the approve/post action must be unavailable on a constrained viewport.");

        IElement reason = constrained.Find("[data-testid='proposal-approver-unavailable']");
        reason.GetAttribute("tabindex").ShouldBe("0", "UX-DR40: the unavailable reason must be focusable (not a bare disabled control).");
        reason.GetAttribute("aria-describedby").ShouldNotBeNullOrEmpty("UX-DR40: the unavailable reason must be aria-described (not a color/animation-only signal).");
        reason.TextContent.ShouldContain("Agents.ProposalApprover.ConstrainedUnavailable", customMessage: "UX-DR40: the unavailable state must carry a visible reason string.");
        constrained.Find("[data-testid='proposal-approver-selected-version']"); // review-only access remains

        // Unconstrained contrast: the approve action returns — the guard is the constraint, not a removed feature.
        IRenderedComponent<ProposalApprover> open = Render<ProposalApprover>(parameters => parameters
            .Add(approver => approver.CanApprove, true)
            .Add(approver => approver.Constrained, false)
            .Add(approver => approver.SelectedVersionId, "version-1"));

        open.Find("[data-testid='proposal-approver-approve']");
        open.FindAll("[data-testid='proposal-approver-unavailable']")
            .ShouldBeEmpty("UX-DR40: an unconstrained viewport keeps the high-impact approve action available.");
    }

    /// <summary>Every routable Agents page (a <c>Components.Pages</c> component declaring a <see cref="RouteAttribute"/>).</summary>
    private static Type[] RoutablePages() => typeof(ProposalApprover).Assembly.GetTypes()
        .Where(type => type.Namespace == "Hexalith.Agents.UI.Components.Pages"
            && type.GetCustomAttributes<RouteAttribute>(inherit: false).Any())
        .ToArray();

    /// <summary>Walks up from the test output directory to the workspace root.</summary>
    private static string WorkspaceRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Agents.slnx"))
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "test")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the agents workspace root from the test output directory.");
    }

    /// <summary>Returns the Agents source root, supporting the workspace-root <c>src/</c> layout.</summary>
    private static string SourceRoot() => Path.Combine(WorkspaceRoot(), "src");

    private static void AssertFamilyMapped(string family, IEnumerable<string> labelKeys)
    {
        string[] keys = labelKeys.ToArray();

        keys.ShouldAllBe(key => !string.IsNullOrWhiteSpace(key),
            $"UX-DR25–29: {family} has a state with no presentation key (a missing/collapsed state).");
        keys.Distinct(StringComparer.Ordinal).Count().ShouldBe(keys.Length,
            $"UX-DR25–29: {family} collapses two states onto one presentation key — the mapping must be 1:1.");

        foreach (string key in keys)
        {
            _resources.GetString(key, CultureInfo.InvariantCulture)
                .ShouldNotBeNullOrWhiteSpace($"UX-DR14: {family} key '{key}' has no English whole string.");
        }
    }
}
