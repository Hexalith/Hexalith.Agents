# Reviewer Gate — Web-Verification Pass

**Reviewer role:** verify every committed technology/version decision in
`ARCHITECTURE-SPINE.md` was web-researched or reality-checked, not asserted
from training data.

**Document reviewed:** `ARCHITECTURE-SPINE.md` (updated 2026-09-09, status:
final), all 755 lines, read in full (front matter, Design Paradigm, AD-1
through AD-31, Consistency Conventions, Stack table, Structural Seed, class
diagram, Capability map, External V1 Prerequisites, Architecture Assumptions,
Deferred Beyond V1).

**Grounding read first:**
`references/Hexalith.AI.Tools/hexalith-llm-instructions.md` — stated stack:
.NET 10+, C# 14+, DAPR 1.18+, .NET Aspire 13.x, Microsoft Fluent UI Blazor V5,
xUnit v3 + Shouldly + NSubstitute.

**Method:** live web search / fetch against NuGet.org, Microsoft Learn,
Dapr docs/blog, and independent blogs, run on 2026-09-09/10, i.e. against the
same calendar point the spine claims as its `updated` date. All URLs cited
below were fetched during this review; none of this review's conclusions are
asserted from model memory alone.

---

## Overall Assessment

The spine is **substantially better web-grounded than the median architecture
doc of this kind**. Several claims that read like they *could* be
training-data assertions turned out, on verification, to be correct and
current as of today (2026-09-09): Dapr Agents' Python-only `DurableAgent`,
Microsoft Agent Framework's excluded Durable-Task/Azure-Functions hosting
extension, the Dapr Conversation API's continued alpha status, and the
`rollForward: latestPatch` feature-band mechanics behind the spine's own
self-declared SDK deviation (ARCH-A-4) all check out exactly as written. The
sources list also cites concrete, fetchable URLs (NuGet package pages, Dapr
docs, Microsoft Agent Framework overview, Microsoft.Agents.AI /
Microsoft.Agents.AI.Workflows package pages) rather than vague "per Microsoft
docs" hand-waving — that is itself evidence of research, not recall.

There is one real, unflagged currency/production-readiness gap: **Fluent UI
Blazor v5 is still a release candidate, not GA**, and the spine pins an RC
package for a document whose `status: final` implies production commitment,
with no corresponding entry in `external-dependency-register.md` tracking
that risk. Everything else checked lands from low to non-issue.

---

## Findings

### 1. [HIGH] Fluent UI Blazor v5 is pinned as a release candidate, and this risk is untracked

- **Location:** Stack table, `Fluent UI Blazor | 5.0.0-rc.5-26219.1 root pin
  matching the catalog`; also referenced implicitly by AD-25 ("FrontComposer
  plus Fluent UI Blazor V5 inheritance") and the UX instructions' "no legacy
  v4/FAST tokens" rule.
- **What I checked:** Live NuGet page for `Microsoft.FluentUI.AspNetCore.Components`,
  plus independent v5 progress coverage (Denis Voituron's blog, which has
  tracked every RC), and GitHub discussion `microsoft/fluentui-blazor#3609`
  ("V5: what is the status?").
- **What I found:**
  - NuGet's latest **stable** listing for `Microsoft.FluentUI.AspNetCore.Components`
    is **4.14.4** (2026-07-30) — major version **4**, not 5.
  - The newest v5 artifact on NuGet is the **prerelease** `5.0.0-rc.5-26219.1`
    (2026-08-09) — this is exactly the version the spine pins.
  - RC1 shipped 2026-02-18; RC4 (2026-06-26) called the API surface "stable"
    and said the team was down to "polish, fixes, and remaining items"; RC5
    (2026-08-07) added more scenarios (FluentAreaChart, DataGrid master/detail,
    FluentOverflow overhaul) — i.e., the API and feature surface were *still
    moving* between RC4 and RC5, seven months after RC1. No GA announcement
    was found as of this review.
  - v4 support is stated to continue "until at least November 2026,"
    consistent with v5 not yet being ready to force migration.
- **Why it matters:** the spine's own `status: final` and its AD-17 apparatus
  (readiness gates, `RQ-1`, evidence levels) treat V1 as heading toward a
  production-like launch, and hexalith-llm-instructions.md hard-requires "V5"
  specifically (new Fluent 2 tokens, no v4/FAST fallback), so this pin may be
  unavoidable — but committing a production system to an RC UI package,
  without any register-tracked risk or exit condition, is exactly the kind of
  externally-verifiable fact this Reviewer Gate exists to catch.
  `external-dependency-register.md` (183 lines, checked in full via grep) has
  **no `EXT-*` record and no other mention of Fluent UI Blazor at all** —
  every other volatile external (tokenizer, safety adapter, secrets, even the
  platform host's own Aspire pin) has a tracked commitment record, but the UI
  framework's prerelease status does not. AD-25's `LR-UI-CONFORMANCE` gate
  will exercise this library at V1 launch without the RC-to-GA transition
  being named as a risk anywhere.
- **Recommendation:** add either an `EXT-*` record or an `ARCH-A-*` assumption
  (parallel to ARCH-A-4's SDK-alignment entry) naming "Fluent UI Blazor v5 GA"
  as a tracked, retirable risk, with a stated fallback if v5 does not GA
  before the Story that first ships UI.

### 2. [MEDIUM / informational] Microsoft Agent Framework is GA and more mature than the spine's "optional, unselected" framing suggests — not wrong, but worth a second look

- **Location:** AD-9 ("Provider and Agent Framework SDKs remain unselected
  until [EXT-PROVIDER-1] commits"), AD-18 ("Microsoft Agent Framework may be
  used inside a generation activity... Agent Framework SDK | `Unselected`" in
  the Stack table).
- **What I checked:** `learn.microsoft.com/en-us/agent-framework/overview/`
  (fetched directly, `ms.date: 2026-07-29`), NuGet pages for
  `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Workflows`, and coverage of
  the GA announcement.
- **What I found:** Microsoft Agent Framework reached **GA 1.0 on 2026-04-03**
  (Visual Studio Magazine, Microsoft Foundry blog, InfoQ all corroborate).
  `Microsoft.Agents.AI` is at **1.19.0** (2026-08-22) and
  `Microsoft.Agents.AI.Workflows` at **1.13.0** (2026-07-03) — both are
  well past 1.0, actively shipping monthly, and positioned by Microsoft as
  "the direct successor" merging Semantic Kernel and AutoGen. The framework's
  own Durable Task extension / Azure Functions hosting integration is real
  and documented (`learn.microsoft.com/en-us/agent-framework/hosting/azure-functions`,
  `learn.microsoft.com/en-us/azure/durable-task/sdks/durable-agents-microsoft-agent-framework`)
  — so AD-18's exclusion of "its Durable Task and Azure Functions hosting…
  from V1" is a real, correctly-named alternative being deliberately rejected,
  not a strawman.
- **Assessment:** this is not an error — deferring SDK selection to
  `EXT-PROVIDER-1` is a legitimate governance choice, and the AD-18 exclusion
  list is accurate. Flagging only because "Unselected" reads as if the
  ecosystem is still immature/pre-GA, when in fact by the spine's own date
  the framework has been GA for five months with two actively-versioned
  NuGet packages. Worth confirming with the architect that "Unselected" means
  "deliberately deferred to a dedicated ADR" and not "not yet viable."

### 3. [LOW / confirms accurate research] Version pins spot-checked live and found current

Checked directly against NuGet.org / vendor sources on 2026-09-09/10:

| Item | Spine value | Live-checked value | Verdict |
| --- | --- | --- | --- |
| Dapr .NET Workflow package | `1.18.5` | NuGet `Dapr.Workflow` latest stable **1.18.5** (2026-07-25), `1.19.0-preview.2` in prerelease | Matches exactly |
| Dapr runtime | `1.18+` / `1.18.5` | Dapr blog "Dapr v1.18 is now available" (2026-06-10); runtime v1.18.0 GitHub release confirmed | Matches |
| xUnit v3 | root override `3.2.2`; catalog `4.0.0` | NuGet `xunit.v3` **4.0.0** GA'd 2026-08-14 (xunit.net release notes); `3.2.x` a real prior stable line | Matches; the spine's own footnote about `4.0.0` requiring Microsoft Testing Platform v2 / a `test.runner` `global.json` entry is consistent with the v3→v4 transition being a runner-generation change, not just a version bump |
| NSubstitute | root override `5.3.0`; catalog `6.2.0` | NuGet latest **6.2.0** (2026-08-11); `5.3.0` a real prior stable | Matches |
| OpenTelemetry (.NET) | `1.18.0` | NuGet `OpenTelemetry` **1.18.0** (2026-08-21) | Matches exactly |
| .NET Aspire | not pinned by the spine itself (delegated to `EXT-HOST-1`); llm-instructions says `13.x` | Aspire jumped 9→13 and dropped the ".NET" name (Nov 2025); 13.2 (Mar 2026), 13.4 (Jun 2026) confirmed current; `external-dependency-register.md` line 183 independently states the host's actual pin as **Aspire 13.4.6** | Consistent across spine, register, and grounding doc |
| FluentValidation | `12.1.1` | NuGet `FluentValidation` **12.1.1** exists; project remains Apache-2.0, **not** commercially relicensed (unlike MediatR/AutoMapper/FluentAssertions) | Matches, and correctly *not* flagged as a licensing risk |
| .NET SDK feature bands | Agents pinned `10.0.301`; siblings `10.0.400`, called out as deviation ARCH-A-4 | Confirmed `10.0.301` and `10.0.400` are different **feature bands**, and `rollForward: latestPatch` pinned to a `.301` band *cannot* cross into the `.400` band (Microsoft Learn `global.json` docs) | The spine's self-flagged deviation is technically correct and precisely reasoned, not a documentation slip |
| Dapr Agents (Python `DurableAgent`) | AD-18 excludes "Python Dapr Agents `DurableAgent`… from V1" | `github.com/dapr/dapr-agents` and Dapr docs describe Dapr Agents as a Python developer framework (v1.0, GA); no equivalent first-class `DurableAgent` exists in the Dapr .NET SDK, which ships `Dapr.Workflow`/`Dapr.Client` at the lower level instead | Accurate characterization, correctly distinguished from the Dapr .NET Workflow SDK the spine actually adopts |
| Dapr Conversation API | Deferred Beyond V1 table: "It remains an alpha capability" | Dapr docs (`conversation_api`, `alpha-beta-apis`) confirm Conversation API is still Alpha2 as of Sept 2026 docs | Accurate, current as of today |

### 4. [LOW] MediatR 14.2.0 sits under a commercial dual-license — real, current, but worth a platform-level (not spine-level) note

- **Location:** Stack table, `MediatR | 14.2.0 from imported workspace
  catalog (catalog-pinned; not referenced by Agents projects)`.
- **What I checked:** NuGet `MediatR` page and Jimmy Bogard's release post.
- **What I found:** `14.2.0` is real and current (released 2026-07-02, per
  `jimmybogard.com/automapper-16-2-0-and-mediatr-14-2-0-released`). MediatR
  went dual-license starting at v13 (2025-07-02): free for individuals and
  organizations under $5M annual revenue, commercial/license-key-enforced
  above that threshold; 14.2.0's changelog itself is about license-key
  handling. The version number is accurately current, so this is not a
  training-data staleness issue.
- **Assessment:** the spine correctly scopes this as "not referenced by
  Agents projects," which is the right mitigation for *this* module. It's
  included here only because the workspace catalog (shared across Hexalith
  repos) carries a commercially-licensed package version, and nothing in this
  spine or the register flags that for whoever *does* reference it elsewhere
  in the platform. Not an Agents-spine defect; a one-line heads-up for
  whoever owns the shared catalog.

### 5. [LOW] bunit pin is a few point releases behind current

- **Location:** Stack table, `bunit | 2.9.0 root pin`.
- **What I checked:** NuGet `bunit` page.
- **What I found:** current latest is **2.10.3** (2026-09-08, i.e. the day
  before the spine's `updated` date). `2.9.0` was current as of roughly
  2026-08-03. This is ordinary pin drift of the kind any point-in-time
  document accumulates and not evidence of un-researched assertion — just
  noting it so the catalog owner can decide whether to bump it.

---

## What I did not independently re-verify

Given review scope and time, I did not attempt to independently verify every
narrow behavioral claim (e.g., the exact Dapr Workflow instance-id
re-creation semantics cited in AD-18, or the precise mechanics of
`WeightedRoundRobinV1` fairness). These read as internally consistent
architecture decisions rather than externally-checkable version/existence
facts, which was the specific mandate for this pass.

---

## Verdict Summary

Of the specific, externally-checkable technology/version claims in the spine,
the large majority were confirmed current and accurate against live sources
dated at or before 2026-09-09. The one substantive gap is Finding 1: a
production-facing UI library pinned at release-candidate status with no
tracked risk or exit criterion in the external dependency register.
