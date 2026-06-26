using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// In-memory <see cref="IFrontComposerRegistry"/> + <see cref="IFrontComposerNavEntryRegistry"/> that captures
/// what <see cref="Hexalith.Agents.UI.Composition.AgentsFrontComposerRegistration.RegisterDomain"/> contributes,
/// so AC1 tests can assert the registered manifest and the ordered, policy-gated nav entries (mirrors the Tenants
/// capturing-registry pattern).
/// </summary>
internal sealed class CapturingFrontComposerRegistry : IFrontComposerRegistry, IFrontComposerNavEntryRegistry
{
    public List<(string Name, string BoundedContext)> NavGroups { get; } = [];

    public List<FrontComposerNavEntry> NavEntries { get; } = [];

    public List<DomainManifest> Manifests { get; } = [];

    public void AddNavGroup(string name, string boundedContext) => NavGroups.Add((name, boundedContext));

    public void AddNavEntry(FrontComposerNavEntry entry) => NavEntries.Add(entry);

    public IReadOnlyList<FrontComposerNavEntry> GetNavEntries() => NavEntries;

    public IReadOnlyList<DomainManifest> GetManifests() => Manifests;

    public void RegisterDomain(DomainManifest manifest) => Manifests.Add(manifest);
}
