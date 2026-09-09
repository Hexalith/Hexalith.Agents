# Reviewer Gate — Web-Verification Pass (v4)

**Verdict: PASS WITH FINDINGS**

**Reviewer role:** verify every committed technology/version decision in
`ARCHITECTURE-SPINE.md` (updated 2026-09-09, status: final) was
web-researched or reality-checked, not asserted from training data, and that
the spine's own tracking entries (notably `ARCH-A-4`, `ARCH-A-8`) still
match live reality today.

**Document reviewed:** `ARCHITECTURE-SPINE.md`, all 758 lines, read in full
(front matter, Design Paradigm, AD-1 through AD-31, Consistency Conventions,
Stack table, Structural Seed, class diagram, Capability map, External V1
Prerequisites, Architecture Assumptions, Deferred Beyond V1). Also read in
full or by targeted grep: the actual repo `global.json`,
`Directory.Build.props`, `Directory.Packages.props` (Agents root),
`references/Hexalith.Builds/Props/Directory.Packages.props` (the workspace
catalog the spine claims to deviate from), sibling `global.json` files
(Tenants, Conversations, Parties, FrontComposer, EventStore),
`external-dependency-register.md`, `launch-readiness-register.md`, and the
two prior same-day review passes (`review-2026-09-09-verified-current-v2.md`,
`...-v3.md`) to avoid re-litigating what they already closed and to check
whether their conclusions still hold **right now**, as instructed.

**Method:** live NuGet.org, GitHub (`dotnet/core`, `dotnet/sdk`), Microsoft
Learn, and Dapr/CNCF sources, fetched today (2026-09-09). Every version claim
below was independently re-fetched in this pass, not copied from v2/v3
(though results are consistent with them where they overlap). All URLs used
are cited inline.

---

## Findings

### Critical

None.

### High

#### H1 — The .NET SDK feature band Agents pins (`10.0.3xx`) appears to have just dropped out of active servicing, one day before the spine's own "updated" date; `ARCH-A-4` frames this as low-urgency test-stack debt, not a supportability risk

- **Spine claim:** `global.json` pins `10.0.301` with `rollForward: latestPatch`; the Stack table records "siblings are on `10.0.400` (deviation ARCH-A-4)"; `ARCH-A-4` reads: "Root **test-stack** overrides (xunit.v3 3.2.2, NSubstitute 5.3.0, **SDK 10.0.301**) are aligned with the workspace catalog... by Story 5.6" — i.e., the SDK pin is bundled in with the test-stack deviation and given the same low-urgency treatment ("Story 5.6 aligns them or records a permanent override reason").
- **What I checked (live, today):**
  - `global.json` (repo root) confirms the pin: `"version": "10.0.301", "rollForward": "latestPatch"`. All five sibling modules (`Hexalith.Tenants`, `Hexalith.Conversations`, `Hexalith.Parties`, `Hexalith.FrontComposer`, `Hexalith.EventStore`) and the shared `Hexalith.Builds` catalog itself pin `10.0.400`.
  - [`dotnet/core` release-notes `README.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md): most recent .NET 10 patch is **10.0.12**, released **2026-09-08** — the day before this spine's `updated` stamp.
  - [`10.0.11.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md) (released 2026-08-11, the prior monthly patch): "10.0 SDKs that include 10.0.11 runtimes: **10.0.400, 10.0.303, 10.0.111**" — i.e., as of last month, band `3xx` was still alive and had reached `10.0.303` (two patches past the spine's `10.0.301` pin).
  - [`10.0.12.md`](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) (released 2026-09-08): "10.0 SDKs that include 10.0.12 runtimes: **10.0.401, 10.0.112**" — **no `10.0.3xx` entry at all**. Band `3xx` is absent from the most recent monthly bundle for the first time.
  - [NETSDK1240 reference](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1240): confirms this is a real, named lifecycle event, not release-note noise: "When the recommended servicing path moves to a different feature band, **the older band stops receiving updates**." (The opt-in `CheckSdkVulnerabilities` check that surfaces this as a build warning is itself only available starting in .NET 11 Preview 5, so it will not fire for this .NET 10 repo — the drop-off has to be caught by review, not tooling, which is exactly what this pass is for.)
  - A search snippet (via a `.NET 10.0.400 upgrade research` note) independently states Microsoft is treating `10.0.4xx` as "the final .NET 10 SDK feature band" for this release cadence, consistent with band `3xx` being wound down now that `4xx` is the current recommended band.
- **The gap:** `ARCH-A-4` is real and its individual facts (xunit.v3 3.2.2 vs. catalog 4.0.0, NSubstitute 5.3.0 vs. catalog 6.2.0, SDK 10.0.301 vs. catalog 10.0.400) were all independently reconfirmed accurate by this pass — see the confirmed-accurate table below. But the assumption's framing — grouping the SDK pin under "test-stack overrides" and giving it the same "Story 5.6 aligns them, or records a permanent override reason" treatment as a unit-test-runner mismatch — undersells what may now be true: the whole module's **build toolchain**, not just its test stack, may already be pinned to a feature band that stopped receiving updates the day before this spine was finalized. If band `3xx` truly is retired, `10.0.301` with `rollForward: latestPatch` resolves at best to the already-existing `10.0.303` on a dev machine that still has it installed, and to nothing (a hard restore/build failure) on a fresh machine that only has `10.0.4xx` or later installed — which is a materially different risk than "the sibling repos happen to be on a newer band."
- **Confidence caveat:** I could not find an explicit, dated Microsoft announcement titled "10.0.3xx end of life" — the finding rests on (a) the disappearance of the band from the most recent monthly bundle vs. its presence the month before, and (b) NETSDK1240's general description of how bands get retired. That is strong circumstantial evidence, not a certainty, which is exactly why it should be verified rather than left implicit in a `status: final` document.
- **Fix:** Either (a) explicitly verify band `10.0.3xx`'s support status against the [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) or by running a restore against `10.0.301`/`10.0.303` today and confirming whether new patches still land, and if retired, treat the SDK-band part of `ARCH-A-4` as a build-supportability risk with its own urgency (not bundled with the test-runner deviation) — e.g. split it into its own tracked assumption with a nearer retirement trigger than "Story 5.6"; or (b) if verification shows the band is still serviced, add that confirmation (with date and source) to `ARCH-A-4` so the next reviewer doesn't have to re-derive it from release-note diffing.

### Medium

#### M1 — Stack table / `ARCH-A-4` deviation tracking is incomplete: two more root-vs-catalog test-stack overrides exist and are untracked

- **What I checked:** Diffed the Agents root `Directory.Packages.props` (Testing `ItemGroup`) against `references/Hexalith.Builds/Props/Directory.Packages.props` (the catalog the spine's Stack table and `ARCH-A-4` claim to compare against).
- **What I found:** Beyond the three overrides the spine names (xunit.v3 family, NSubstitute, SDK), two more Testing-group packages are also root-overridden away from the catalog and are named in **neither** the Stack table **nor** `ARCH-A-4`:
  - `Microsoft.NET.Test.Sdk`: Agents root `18.6.0` vs. catalog `18.9.0` (catalog value confirmed current — [NuGet: `Microsoft.NET.Test.Sdk`](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk) latest is actually `18.10.0`, published **2026-09-09**, today).
  - `xunit.runner.visualstudio`: Agents root `3.1.5` vs. catalog `4.0.0` (this one is the runner-generation counterpart of the already-tracked xunit.v3 deviation, so it's arguably the same root cause — but it is still a distinct package version pin that the Stack table doesn't list, so a reader auditing the Stack table against the actual `.props` file cannot find it there).
- **Why it matters:** The Stack table and `ARCH-A-4` present themselves as the complete, reality-checked inventory of where Agents' pins diverge from the shared catalog. They are not quite complete — a diff of the actual project file surfaces two more divergences the spine doesn't name, one of which (`Microsoft.NET.Test.Sdk`) has no relationship to the xUnit v3/Microsoft Testing Platform v2 story already tracked, so it won't automatically get fixed as a side effect of Story 5.6's xUnit alignment work.
- **Fix:** Add `Microsoft.NET.Test.Sdk` (and, for completeness, `xunit.runner.visualstudio`) to the Stack table's override footnote or to `ARCH-A-4`'s parenthetical package list, so Story 5.6 (or whichever story reconciles the test stack) has a complete checklist instead of a partial one.

### Low

#### L1 — `bunit` pin remains one minor release behind current (unresolved from the prior same-day review)

- **Spine claim:** `bunit | 2.9.0 root pin`.
- **Live check:** [NuGet `bunit`](https://www.nuget.org/packages/bunit) latest stable is **2.10.3**, published **2026-09-08** — the day before the spine's own `updated` date. `2.9.0` was current roughly a month earlier (published 2026-08-03).
- **Assessment:** Same finding v2 raised (`review-2026-09-09-verified-current-v2.md`, Finding 5) and left open; nothing has changed except the gap is now one point release wider (`2.10.3` didn't exist yet when v2 ran its check). Ordinary pin drift, not evidence of un-researched assertion — flagged only because it is still unresolved and the gap is now current as of literally the day before this document's own date stamp.
- **Fix:** Bump the catalog/root pin to `2.10.3` or note the deferral explicitly; low urgency.

#### L2 — MediatR's commercial dual-license remains real and current; correctly unreferenced by Agents, but still an undocumented platform-catalog-level exposure

- **Spine claim:** Stack table: "MediatR | `14.2.0` from imported workspace catalog (catalog-pinned; not referenced by Agents projects)".
- **Live check:** [NuGet `MediatR`](https://www.nuget.org/packages/mediatr/) confirms `14.2.0` is current and real. MediatR has been dual-licensed (Reciprocal Public License 1.5 + a Lucky Penny Software commercial license) since v13 (2025-07-02); a Community-edition carve-out exists for orgs under $5M revenue, non-profits, and non-production use, but production use above that threshold requires a paid license key. See [Milan Jovanović's summary](https://milanjovanovic.tech/blog/mediatr-and-masstransit-going-commercial-what-this-means-for-you) and [Jimmy Bogard's announcement](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/).
- **Assessment:** Same finding as v2 (Finding 4), reconfirmed unchanged today. The spine's own mitigation ("not referenced by Agents projects") is the correct scope for this document — this is not an Agents-spine defect. It is repeated here only because the task asked for every named library to be checked, and because the underlying commercial-license fact is still true and still undocumented anywhere in `external-dependency-register.md` for whichever platform module *does* consume MediatR from the shared catalog.
- **Fix:** None required at the Agents-spine level; a one-line heads-up for the shared-catalog owner remains appropriate (as v2 already noted).

#### L3 — Platform host's Aspire pin trails the workspace catalog's Aspire pin by one patch (informational, outside spine's authority)

- **What I checked:** `external-dependency-register.md` line 183 states `EXT-HOST-1` at commit `a66cdf34` uses "an empty file-based AppHost on **Aspire 13.4.6**." The Agents/workspace catalog (`Directory.Packages.props` in `Hexalith.Builds`) pins `Aspire.Hosting` at `13.5.3`.
- **Live check:** [NuGet `aspire`](https://www.nuget.org/profiles/aspire) / InfoQ coverage confirm **13.5.3** (published 2026-08-25) is the current latest Aspire release; `13.4.6` is one patch behind.
- **Assessment:** The spine correctly states "Hosting... platform-owned through `EXT-HOST-1`... not pinned by Agents," so this is explicitly out of the spine's authority by its own design (AD-16). Noted only because the task asked to cross-check the register for version claims that should match the spine, and this is a small, real one-patch gap between two documents that quote different Aspire versions for what is nominally the same platform stack.
- **Fix:** None required of the Agents spine; informational for whoever owns `EXT-HOST-1`.

---

## Confirmed accurate — re-verified live today, no finding

| Item | Spine claim | Live-checked value (today, 2026-09-09) | Source |
| --- | --- | --- | --- |
| Fluent UI Blazor v5 / `ARCH-A-8` | Pinned `5.0.0-rc.5-26219.1`, "a release candidate, not GA" | Latest **stable** is `4.14.4` (2026-07-30); latest **v5 prerelease** is exactly `5.0.0-rc.5-26219.1` (2026-08-09); no GA release exists | [NuGet](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components) |
| Microsoft Agent Framework GA (AD-9) | "Microsoft Agent Framework... already GA" | GA'd 2026-04-03; `Microsoft.Agents.AI` latest **1.20.0** (2026-08-31, stable, independently refetched this pass) | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI), [devblogs](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/) |
| Dapr Agents (Python) GA (AD-9, AD-18) | "Dapr Agents [are]... already GA"; AD-18 excludes "Python Dapr Agents `DurableAgent`" from V1 | GA'd 2026-03-23 at KubeCon EU, Python-only framework, no .NET `DurableAgent` equivalent | [CNCF announcement](https://www.cncf.io/announcements/2026/03/23/general-availability-of-dapr-agents-delivers-production-reliability-for-enterprise-ai/) |
| Dapr Workflow package | `1.18.5` | Latest stable **1.18.5**, published 2026-07-25 | [NuGet](https://www.nuget.org/packages/Dapr.Workflow) |
| Dapr Workflow instance-id semantics (AD-18) | "Dapr 1.18 never re-creates a non-terminal instance id" | Confirmed: reusing an ID for a still-running instance is rejected; the instance-ID-reuse-policy option was removed in v1.18 | [Dapr Workflow docs](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/) |
| Dapr Conversation API alpha status (Deferred Beyond V1) | "It remains an alpha capability" | Confirmed still Alpha2, docs last touched 2026-09-08 | [Dapr Conversation overview](https://docs.dapr.io/developing-applications/building-blocks/conversation/conversation-overview/) |
| xUnit v3 catalog pin | catalog `4.0.0` | Latest stable **4.0.0**, GA'd 2026-08-14/15 | [xunit.net release notes](https://xunit.net/releases/v3/4.0.0), [NuGet](https://www.nuget.org/packages/xunit.v3) |
| NSubstitute catalog pin | catalog `6.2.0` | Latest stable **6.2.0**, published 2026-08-11 | [NuGet](https://www.nuget.org/packages/nsubstitute/) |
| OpenTelemetry .NET | `1.18.0` | Latest stable **1.18.0**, published 2026-08-21 | [NuGet](https://www.nuget.org/packages/OpenTelemetry) |
| FluentValidation | `12.1.1` | Confirmed current; project remains under its original (non-commercial) license, correctly *not* flagged as a licensing risk | [NuGet](https://www.nuget.org/packages/fluentvalidation/) |
| .NET Aspire (catalog, not spine-pinned) | catalog `13.5.3` | Latest stable **13.5.3**, published 2026-08-25 | [InfoQ](https://www.infoq.com/news/2026/04/aspire-13-2-release/), NuGet |
| .NET SDK feature-band mechanics (ARCH-A-4's own reasoning) | `10.0.301` and `10.0.400` are different feature bands; `rollForward: latestPatch` cannot cross bands | Confirmed by Microsoft Learn's `global.json`/feature-band documentation | [Learn: releases-and-support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support) |
| C# 14 / `net10.0` pairing | `LangVersion 14`, `TargetFramework net10.0` | Correct current pairing for .NET 10 | Consistent with all sources above |

---

## What this pass did not re-derive from scratch

Per the two prior same-day passes, `ARCH-A-8`'s resolution (the row itself, its wiring into `RQ-1`'s `UnretiredAssumption` blocker mechanism) and AD-9's GA clause were already validated structurally by `review-2026-09-09-verified-current-v3.md`; this pass re-fetched the underlying facts independently (see table above) rather than re-arguing the structural adequacy already covered there, and found nothing changed. `MediatR` and `bunit` were originally raised by v2 (Findings 4 and 5); this pass independently re-fetched both live rather than assuming they still hold, and confirmed they do (bunit's gap is now slightly wider — see L1).

---

## Verdict Summary

**PASS WITH FINDINGS.** No Critical issues. One High finding (H1: the `10.0.3xx` SDK feature band Agents pins appears to have just fallen out of active servicing, a more urgent supportability risk than `ARCH-A-4`'s current "test-stack alignment by Story 5.6" framing conveys). One Medium finding (M1: the Stack table's deviation list is incomplete — `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` also diverge from the catalog and aren't named). Three Low findings, two of which (L1 bunit, L2 MediatR) restate and reconfirm still-open items from the prior same-day `v2` review rather than introduce new ones, and one (L3) is a cross-document informational note outside the spine's own authority. Every other externally-checkable technology/version claim in the spine — including the two items `v3` closed (`ARCH-A-8` Fluent UI Blazor v5 RC tracking, AD-9's Agent Framework/Dapr Agents GA clause) — was independently re-fetched today and confirmed still accurate.
