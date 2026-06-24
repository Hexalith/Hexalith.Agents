using System.Threading;
using System.Threading.Tasks;

using Bunit;
using Bunit.TestDoubles;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Resources;
using Hexalith.Agents.UI.Services.Gateways;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Testing;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Shared bUnit harness for the Agents admin-setup UI tests. Builds on the FrontComposer test host (FluentUI,
/// localization, the shell services, and an on-demand Fluxor store), then registers a key-returning
/// <see cref="StubAgentsLocalizer"/>, NSubstitute gateways (defaulting to the fail-closed result), and bUnit
/// authorization. The deferred gateway placeholders are never exercised — every page reads the substituted seam.
/// </summary>
public abstract class AgentsTestContext : FrontComposerTestBase
{
    protected AgentsTestContext()
    {
        SetupGateway.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.NotAuthorized()));
        SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.NotAuthorized()));
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.NotAuthorized()));

        Services.AddSingleton(SetupGateway);
        Services.AddSingleton(CatalogGateway);
        Services.AddSingleton<IStringLocalizer<AgentsResources>>(new StubAgentsLocalizer());
        Authorization = AddAuthorization();
    }

    /// <summary>The substituted Agent status/configuration read gateway.</summary>
    protected IAgentSetupGateway SetupGateway { get; } = Substitute.For<IAgentSetupGateway>();

    /// <summary>The substituted provider-catalog read gateway.</summary>
    protected IProviderCatalogGateway CatalogGateway { get; } = Substitute.For<IProviderCatalogGateway>();

    /// <summary>The bUnit authorization context (default: unauthenticated).</summary>
    protected BunitAuthorizationContext Authorization { get; }

    /// <summary>Renders a page component standalone (store initialized first), without the shell chrome.</summary>
    protected IRenderedComponent<TPage> RenderPage<TPage>()
        where TPage : IComponent
    {
        InitializeStoreAsync().GetAwaiter().GetResult();
        return Render<TPage>();
    }

    /// <summary>Renders a routable page component inside the real <see cref="FrontComposerShell"/> (store initialized first).</summary>
    protected IRenderedComponent<FrontComposerShell> RenderInShell<TPage>()
        where TPage : IComponent
    {
        InitializeStoreAsync().GetAwaiter().GetResult();
        return Render<FrontComposerShell>(parameters => parameters
            .Add(shell => shell.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TPage>(0);
                builder.CloseComponent();
            })));
    }

    /// <summary>
    /// Renders a page inside the shell with an explicit navigation fragment so the shell-owned skip links and the
    /// navigation landmark render deterministically (AC6), independent of domain nav discovery.
    /// </summary>
    protected IRenderedComponent<FrontComposerShell> RenderInShellWithNavigation<TPage>()
        where TPage : IComponent
    {
        InitializeStoreAsync().GetAwaiter().GetResult();
        return Render<FrontComposerShell>(parameters => parameters
            .Add(shell => shell.Navigation, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span data-testid=\"agents-test-nav\">nav</span>")))
            .Add(shell => shell.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TPage>(0);
                builder.CloseComponent();
            })));
    }
}
