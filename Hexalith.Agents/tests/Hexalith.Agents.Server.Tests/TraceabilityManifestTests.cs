namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Hexalith.Agents.Server.Tests.Conformance;

using Shouldly;

/// <summary>
/// Story 4.5 — machine-checkable backing for the AC4 governance-conformance report. Drives an in-repo manifest
/// (<c>Conformance/traceability-manifest.json</c>) mapping every <c>FR-1..28</c>, <c>NFR-1..10</c>, <c>AD-1..19</c>,
/// <c>UX-DR1..41</c> id → <c>{ story, verificationPath, blocker }</c> and asserts: (a) <b>completeness</b> — every id in
/// those ranges is present exactly once (no silent gap or duplicate); (b) <b>honesty</b> — every <c>verificationPath</c>
/// resolves to a real file on disk via <see cref="ModuleLayout"/> (cross-project file existence, not reflection); and
/// (c) every unresolved-governance id is flagged <c>blocker:true</c> and named in the report's blocker section. This
/// prevents the traceability matrix from claiming coverage that does not exist. Bounded by design: file-existence +
/// id-completeness only, no deep semantic check.
/// </summary>
[Trait(RequirementTraits.Requirement, "FR-23")]
[Trait(RequirementTraits.Architecture, "AD-17")]
public sealed class TraceabilityManifestTests
{
    /// <summary>
    /// The unresolved-governance ids that MUST be explicit launch blockers, not hidden assumptions (Story 4.5 Dev Notes /
    /// AC4 report §5): bounded-context values (OQ-10), content-bearing audit (OQ-8), Content Safety categories (OQ-9),
    /// launch metric thresholds (OQ-11), latency SLOs (OQ-5), cost-control thresholds (OQ-6), the Conversations
    /// AddParticipant membership seam (AD-6/AD-7), the deferred live runtime owner (AD-18), and MCP/A2A/tool schemas (AD-19).
    /// </summary>
    private static readonly string[] _unresolvedGovernanceIds =
    [
        "FR-9", "FR-24", "FR-26", "FR-28", "NFR-9", "NFR-10", "AD-6", "AD-7", "AD-18", "AD-19",
    ];

    private static readonly Manifest _manifest = LoadManifest();

    [Fact]
    public void Manifest_covers_every_requirement_id_exactly_once()
    {
        AssertExactIds(_manifest.Fr, "FR-", 1, 28);
        AssertExactIds(_manifest.Nfr, "NFR-", 1, 10);
        AssertExactIds(_manifest.Ad, "AD-", 1, 19);
        AssertExactIds(_manifest.Uxdr, "UX-DR", 1, 41);
    }

    [Fact]
    public void Every_verification_path_resolves_to_a_real_file_on_disk()
    {
        foreach (ManifestEntry entry in AllEntries())
        {
            string resolved = Path.Combine(ModuleLayout.ModuleRoot, entry.VerificationPath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(resolved).ShouldBeTrue(
                $"Traceability is dishonest: '{entry.Id}' names verification path '{entry.VerificationPath}', which does not exist on disk.");
        }
    }

    [Fact]
    public void Every_unresolved_governance_id_is_a_blocker_named_in_the_report()
    {
        Dictionary<string, ManifestEntry> byId = AllEntries().ToDictionary(entry => entry.Id);

        // (c1) Every unresolved-governance id is present and flagged blocker:true.
        foreach (string id in _unresolvedGovernanceIds)
        {
            byId.ShouldContainKey(id, $"AC4: unresolved-governance id '{id}' is missing from the traceability manifest.");
            byId[id].Blocker.ShouldBeTrue($"AC4: unresolved-governance id '{id}' must be flagged blocker:true, not a hidden assumption.");
        }

        // (c2) The blocker set is exactly the unresolved-governance set — no stray blocker, no silently-dropped one.
        string[] blockerIds = AllEntries().Where(entry => entry.Blocker).Select(entry => entry.Id).OrderBy(id => id).ToArray();
        blockerIds.ShouldBe(_unresolvedGovernanceIds.OrderBy(id => id).ToArray(),
            "AC4: the manifest's blocker:true set must equal the explicit unresolved-governance launch-blocker set.");

        // (c3) The report's blocker section names every unresolved-governance id (not hidden in the prose).
        string reportPath = Path.GetFullPath(Path.Combine(
            ModuleLayout.ModuleRoot, "..", "_bmad-output", "implementation-artifacts", "4-5-governance-conformance-report.md"));
        File.Exists(reportPath).ShouldBeTrue($"AC4: the governance-conformance report must exist at '{reportPath}'.");

        string report = File.ReadAllText(reportPath);
        foreach (string id in _unresolvedGovernanceIds)
        {
            report.ShouldContain(id, customMessage: $"AC4: the report must name unresolved-governance launch blocker '{id}' explicitly.");
        }
    }

    private static IEnumerable<ManifestEntry> AllEntries()
        => _manifest.Fr.Concat(_manifest.Nfr).Concat(_manifest.Ad).Concat(_manifest.Uxdr);

    private static void AssertExactIds(ManifestEntry[] entries, string prefix, int from, int to)
    {
        string[] expected = Enumerable.Range(from, to - from + 1).Select(i => $"{prefix}{i}").ToArray();
        string[] actual = entries.Select(entry => entry.Id).ToArray();

        actual.Length.ShouldBe(actual.Distinct().Count(), $"The {prefix} manifest contains a duplicate id.");
        actual.OrderBy(id => id, System.StringComparer.Ordinal).ToArray()
            .ShouldBe(expected.OrderBy(id => id, System.StringComparer.Ordinal).ToArray(),
                $"The {prefix} manifest must map every id {prefix}{from}..{prefix}{to} exactly once (no silent coverage gap).");
    }

    private static Manifest LoadManifest()
    {
        string manifestPath = Path.Combine(
            ModuleLayout.ModuleRoot, "tests", "Hexalith.Agents.Server.Tests", "Conformance", "traceability-manifest.json");
        File.Exists(manifestPath).ShouldBeTrue($"Expected the traceability manifest at '{manifestPath}'.");

        Manifest? manifest = JsonSerializer.Deserialize<Manifest>(
            File.ReadAllText(manifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return manifest.ShouldNotBeNull("The traceability manifest could not be deserialized.");
    }

    private sealed record ManifestEntry(string Id, string Title, string Story, string VerificationPath, bool Blocker);

    private sealed record Manifest(ManifestEntry[] Fr, ManifestEntry[] Nfr, ManifestEntry[] Ad, ManifestEntry[] Uxdr);
}
