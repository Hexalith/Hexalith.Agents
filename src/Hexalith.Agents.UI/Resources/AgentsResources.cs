namespace Hexalith.Agents.UI.Resources;

/// <summary>
/// Strongly-typed marker for the Agents admin-setup UI localization resources (AC1, AC4, AC5). The
/// <c>AgentsResources.resx</c> (en) and <c>AgentsResources.fr.resx</c> (fr) satellite resources are bound through
/// <c>IStringLocalizer&lt;AgentsResources&gt;</c>. Every status label, blocker reason, denial reason, provider/model
/// state, response-mode label, and column header is one complete localizable whole string with named/positional
/// placeholders — never assembled from runtime fragments (UX-DR14). Mirrors <c>TenantsResources</c>.
/// </summary>
public sealed class AgentsResources;
