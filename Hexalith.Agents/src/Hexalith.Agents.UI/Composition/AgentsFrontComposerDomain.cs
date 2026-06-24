namespace Hexalith.Agents.UI.Composition;

/// <summary>
/// Marker for the Agents FrontComposer domain. The shell discovers the static
/// <see cref="AgentsFrontComposerRegistration"/> (a static <c>Manifest</c> + <c>RegisterDomain</c>); this empty
/// type mirrors the Tenants precedent (<c>TenantsFrontComposerDomain</c>) and names the composition unit.
/// </summary>
public sealed class AgentsFrontComposerDomain;
