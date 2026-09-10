# Verified-Current Reviewer Gate — Round 8

**Reviewer:** Independent architecture reviewer (verified-current lens)
**Date performed:** 2026-09-09
**Target:** `ARCHITECTURE-SPINE.md` (architecture-agents-2026-06-23-2), full ~798-line document
**Method:** Live WebSearch/WebFetch against primary sources (dotnet/core release notes, NuGet.org, Dapr docs/blog, xunit.net, CNCF/InfoQ GA announcements, sentinelone vulnerability DB), plus direct filesystem/git cross-check of this repo's `global.json`, `Directory.Packages.props`, `Directory.Build.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`, and `git ls-tree`/`git log` on all five submodule gitlinks. Every version and CVE claim below was re-checked live today; no prior round's "already verified" note was trusted.

## Overall Verdict: **PASS WITH FINDINGS**

No fabricated/hallucinated technology or version claim was found. The spine's headline claim — a live unpatched RCE exposure from an abandoned `10.0.3xx` .NET SDK band — is **independently confirmed true** against `dotnet/core` release notes, which is a genuinely strong result for a "verified-current" pass. The findings below are real but narrower: one stale gitlink pin, one gap between a documented "immediate" remediation and actual repo state, and a couple of unresourced/soft claims.

---

## Findings

### F1 — Medium — `Hexalith.EventStore` gitlink in Stack table is stale (drifted ~2h11m same day)

The Stack table (line ~503) states: `Hexalith.EventStore | parent gitlink `e302432c` at the spine `updated` date`.

Direct repo check:
- `git ls-tree HEAD -- references/Hexalith.EventStore` → committed gitlink is `0994c37814c37dac7667a209dbd0659125aac49e` (`0994c378`), not `e302432c`.
- `git log` inside the EventStore submodule confirms `e302432c` (commit time 2026-09-09 11:43:41+02:00, "feat: add editorial review, rubric closure, and technology currentness updates for architecture spine") is a direct **ancestor** of the current pin `0994c378` (commit time 2026-09-09 13:54:09+02:00, "fix: update EventStore Phase 4 readiness documentation and architecture reconciliation").
- So the cited gitlink is ~2h11m out of date as of this review, same calendar day as the spine's own `updated: 2026-09-09` frontmatter.

The other four sibling gitlinks in the same table row-set are all **exactly correct**: `Hexalith.Conversations` = `73bcee6f` ✓ (matches `git ls-tree HEAD`), `Hexalith.Parties` = `fa423985` ✓, `Hexalith.Tenants` = `54fc4040` ✓, `Hexalith.FrontComposer` = `053b2008` ✓.

This is not a functional defect but does violate the "verified-current" bar: a reader trusting the Stack table's EventStore pin today gets a two-hour-stale value. Given how fast this spine is churning same-day, any future round should re-pull `git ls-tree HEAD` for all five gitlinks immediately before finalizing, not from memory of an earlier pass.

### F2 — Medium/High — ARCH-A-4's "immediate" SDK remediation has not actually landed in the repo

ARCH-A-4 and the Stack table assert the .NET SDK escalation "has already fired" and that the SDK pin should be "bumped past the capped `10.0.3xx` band immediately." I independently confirmed this underlying technical claim is **true** (see F3). However, the repo's actual `global.json` (checked directly, not from the doc) still reads:
```json
{ "sdk": { "version": "10.0.301", "rollForward": "latestPatch" } }
```
— i.e., still capped in the abandoned `10.0.3xx` band, unchanged. The spine correctly *describes* the exposure and correctly labels the remediation urgency as "immediate," but the remediation itself has not been executed as of this review. This is a documentation-vs-reality gap worth calling out explicitly in the gate rather than leaving implicit inside prose: either bump `global.json` now, or add an explicit line in ARCH-A-4/Stack noting the SDK bump is tracked-but-not-yet-applied with an owner and date.

### F3 — Informational (confirms spine is CORRECT, not a new problem) — CVE-2026-69522 / `10.0.3xx` abandonment claim is independently verified true

This is the most load-bearing factual claim in the whole document, so it was checked from primary sources rather than accepted on the spine's internal consistency:

- `github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md` (2026-08-11 bundle): SDKs shipped = `10.0.400`, **`10.0.303`**, `10.0.111` — i.e. `10.0.303` genuinely was the last `10.0.3xx` SDK ever released.
- `github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md` (2026-09-08 bundle): SDKs shipped = `10.0.401`, `10.0.112` only — **no `10.0.3xx` member**, confirming the band was dropped exactly as the spine states.
- The 10.0.12 release notes list six CVEs fixed, including **CVE-2026-69522**; corroborated independently via Microsoft's own September 2026 servicing/KB pages and third-party CVE trackers (senserva.com Patch Tuesday, cvedetails.com) as a CVSS 8.8 .NET/.NET Framework RCE, patched by the 8.0.31 / 9.0.20 / 10.0.12(→10.0.401/10.0.112) wave released 2026-09-08.
- Net effect: a project capped at `10.0.303` via `rollForward: latestPatch` (exactly this repo's `global.json`, confirmed directly) genuinely cannot reach the CVE-2026-69522 fix today. The spine's "live unpatched-RCE exposure today, not a future risk" framing is accurate, not alarmist or hallucinated.
- .NET 10 itself remains in support (documented support window 2025-11-11 to 2028-11-14), so this is a supported-but-exposed-by-pin situation, not an EOL problem.

No action needed on this finding beyond F2 (closing the gap between "escalated" and "executed").

### F4 — Low/Informational — Confirmed-accurate claims (spot-checked, no issues found)

All of the following were independently verified live and found **accurate as stated**:

| Claim | Verdict | Source |
| --- | --- | --- |
| Dapr Workflow 1.18: "never re-creates a non-terminal instance id" | **True**, and stronger in 1.18 specifically — Dapr docs confirm a running instance ID is rejected with a conflict error, and v1.18 removed the `IGNORE`/`TERMINATE` SDK override options that could previously force recreation | docs.dapr.io workflow-features-concepts |
| Microsoft Agent Framework is GA | **True** — GA'd 2026-04-03, unifying Semantic Kernel + AutoGen | InfoQ, Visual Studio Magazine, learn.microsoft.com/agent-framework |
| Dapr Agents is GA | **True** — CNCF-announced GA (v1.0) 2026-03-23 at KubeCon EU | cncf.io, diagrid.io |
| `Microsoft.Agents.AI` / `Microsoft.Agents.AI.Workflows` NuGet packages exist, stable, fit-for-purpose, not renamed/deprecated | **True** — both at stable v1.20.0, published 2026-08-31 | nuget.org/packages/Microsoft.Agents.AI(.Workflows) |
| MediatR `14.2.0` is current; commercial-license claim is accurate and "operationally live" | **True** — 14.2.0 published 2026-07-02 is latest; NuGet page itself documents the `MediatR.io` license-key requirement | nuget.org/packages/MediatR |
| No CVE found against MediatR 14.2.0 | Unverifiable-negative (no advisory surfaced in any searched source) | web search, no hits |
| Dapr .NET SDK `1.18.5` (Dapr.Client/Dapr.Workflow) is the latest **stable** release (1.19.0 is preview-only) | **True** | nuget.org/packages/Dapr.Client, Dapr.Workflow |
| Dapr Conversation API "remains an alpha capability" (Deferred-Beyond-V1 table) | **True** — still Alpha2 as of Sept 2026 docs | docs.dapr.io/developing-applications/building-blocks/conversation |
| Fluent UI Blazor v5 pinned at `5.0.0-rc.5-26219.1` is a real, current RC, **not yet GA** (ARCH-A-8) | **True** — RC5 shipped 2026-08-07; no GA announcement found anywhere as of 2026-09-09 | dvoituron.com, github.com/microsoft/fluentui-blazor milestones |
| xUnit v3 `4.0.0` (catalog) is real and requires Microsoft Testing Platform v2 / a `global.json` `test.runner` entry on .NET 10 SDK, exactly as ARCH-A-4 claims | **True** — released 2026-08-14; MTP v1 support dropped in 4.0.0 | xunit.net/releases/v3/4.0.0, xunit.net MTP docs |
| OpenTelemetry .NET `1.18.0` is current and not affected by known CVEs (CVE-2026-40182/40891 were fixed in 1.15.2, well before 1.18.0) | **True** | github.com/open-telemetry/opentelemetry-dotnet, sentinelone.com CVE DB |

### F5 — Low — Repo cross-check of Stack-table package pins: all match, one caveat

Directly reading this repo's `Directory.Packages.props` and the imported `references/Hexalith.Builds/Props/Directory.Packages.props` catalog confirms every numeric pin the spine's Stack table states is exactly what's in the repo: Fluent UI Blazor `5.0.0-rc.5-26219.1` ✓, xunit.v3/assert/extensibility.core `3.2.2` root override vs catalog `4.0.0` ✓, xunit.runner.visualstudio `3.1.5` root override vs catalog `4.0.0` ✓, Shouldly `4.3.0` ✓, NSubstitute `5.3.0` root override vs catalog `6.2.0` ✓, bunit `2.9.0` ✓, Dapr packages `1.18.5` (catalog) ✓, MediatR `14.2.0` (catalog) ✓, FluentValidation `12.1.1` (catalog) ✓, OpenTelemetry core packages `1.18.0` (catalog) ✓. `global.json` SDK `10.0.301`/`rollForward: latestPatch` ✓ matches spine exactly (see F2 for the remediation-gap angle on this one). No contradictions found between the spine's Stack table and the actual repo state, other than the EventStore gitlink in F1.

### Assumption substance checks (TBD dates not re-flagged, per instructions)

- **ARCH-A-8** (Fluent UI v5 RC, not GA): substance still valid today — confirmed still RC5, no GA (F4).
- **ARCH-A-4** (SDK/test-stack deviation): substance still valid and, per F3, the escalation condition it describes is independently confirmed to have actually fired; per F2 the "immediate" remediation is documented but not yet executed in the repo.
- Other `[ASSUMPTION]`-tagged rows with `TBD` retirement dates (A-1 through A-19, ARCH-A-1/2/3/6/7/9/10/11/12) are not web-verifiable technology claims (they are governance/product decisions) and were out of scope for this lens; none were re-flagged for their TBD status per instructions.

---

## Severity Counts

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 0 |
| Medium | 2 (F1, F2) |
| Low | 2 (F4's negative-CVE note, F5) |
| Informational | 1 (F3 — confirms spine is correct) |

## Sources Consulted (representative, not exhaustive)

- https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md
- https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md
- https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md
- https://www.nuget.org/packages/Microsoft.Agents.AI/, https://www.nuget.org/packages/Microsoft.Agents.AI.Workflows/
- https://www.nuget.org/packages/MediatR/
- https://www.nuget.org/packages/Dapr.Client, https://www.nuget.org/packages/Dapr.Workflow
- https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/
- https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/
- https://www.cncf.io/announcements/2026/03/23/general-availability-of-dapr-agents-delivers-production-reliability-for-enterprise-ai/
- https://www.infoq.com/news/2026/08/agent-framework-harness-ga/ , https://visualstudiomagazine.com/articles/2026/04/06/microsoft-ships-production-ready-agent-framework-1-0-for-net-and-python.aspx
- https://dvoituron.com/2026/08/07/fluentui-blazor-5-rc5/ , https://github.com/microsoft/fluentui-blazor/milestone/44
- https://xunit.net/releases/v3/4.0.0 , https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
- https://github.com/open-telemetry/opentelemetry-dotnet (RELEASENOTES.md core-1.18.0), https://www.sentinelone.com/vulnerability-database/cve-2026-40182/
- Local: `/home/administrator/projects/hexalith/agents/global.json`, `Directory.Packages.props`, `Directory.Build.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`, `git ls-tree HEAD` and `git log` on all five `references/*` submodules.
