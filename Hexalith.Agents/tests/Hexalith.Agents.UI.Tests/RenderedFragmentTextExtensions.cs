using System.Linq;

using Bunit;

using Microsoft.AspNetCore.Components;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Test helpers for asserting against the user-visible text of a rendered component, excluding HTML attributes
/// and styles. FluentUI v5 design tokens surface forbidden substrings inside attribute/style values (e.g.
/// <c>color="success"</c>, <c>--colorStatusSuccessForeground1</c>), so secret/PII non-disclosure guards that must
/// verify a value is never <em>shown</em> assert against this visible text rather than the raw markup.
/// </summary>
internal static class RenderedFragmentTextExtensions
{
    public static string VisibleText<TComponent>(this IRenderedComponent<TComponent> rendered)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(rendered);
        return string.Concat(rendered.Nodes.Select(static node => node.TextContent));
    }
}
