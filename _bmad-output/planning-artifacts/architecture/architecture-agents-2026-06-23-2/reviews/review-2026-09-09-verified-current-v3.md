# Reviewer Gate — Web-Verification Pass (v3, delta review)

**Reviewer role:** verify every committed technology/version decision in
`ARCHITECTURE-SPINE.md` was web-researched or reality-checked, not asserted
from training data.

**Nature of this pass:** delta/verification against
`reviews/review-2026-09-09-verified-current-v2.md`, which raised two findings
against the previous spine version. This pass re-reads the current spine in
full (758 lines: front matter, Design Paradigm, AD-1 through AD-31,
Consistency Conventions, Stack table, Structural Seed, class diagram,
Capability map, External V1 Prerequisites, Architecture Assumptions,
Deferred Beyond V1), confirms two amendments the spine made in response to
v2, and re-verifies the underlying facts live rather than trusting that
nothing changed since 2026-09-09/10.

**Method:** live web fetch/search against NuGet.org, Microsoft Learn/devblogs,
Visual Studio Magazine, CNCF, run on 2026-09-09 (today, same calendar date as
the spine's own `updated` stamp — no time has actually elapsed, but the
instruction was to verify live rather than assume v2's numbers still hold).

---

## Overall Assessment

Both v2 findings were substantively addressed by the amendments described in
this task's context. Finding 1 (untracked RC risk) is now closed via a new
`ARCH-A-8` row that follows the spine's own established convention
(`ARCH-A-4`) for a tracked, retirable risk, and — importantly — that
assumption is not merely a passive note: the Architecture Assumptions
section header states `RQ-1` names every unretired row here as an
`UnretiredAssumption` blocker, so ARCH-A-8 has real teeth against a
`status: final` document, not just documentation-of-intent. Finding 2
(AD-9's "Unselected" framing under-describing Microsoft Agent Framework's
maturity) is now closed via an added clause on AD-9 itself, and that clause's
two factual claims — Microsoft Agent Framework GA, and Dapr Agents (Python)
GA — both check out live today. Nothing material has changed in the world
since v2 on the one open item (Fluent UI Blazor v5 status): it is still an
RC, not GA, so ARCH-A-8's framing remains accurate as of today.

---

## Findings

### 1. [RESOLVED] Fluent UI Blazor v5 RC risk — now tracked via ARCH-A-8

- **Location:** Architecture Assumptions table, new row `ARCH-A-8` (line
  744); Stack table still pins `Fluent UI Blazor | 5.0.0-rc.5-26219.1` (line
  480, unchanged).
- **Exact new row text:** "Fluent UI Blazor is pinned at v5
  `5.0.0-rc.5-26219.1`, a release candidate, not GA | Stack | Architecture +
  FrontComposer Maintainer | v5 reaches GA, or the RC pin is confirmed as an
  accepted production risk before the story that first ships UI."
- **Is this a reasonable resolution?** Yes. It mirrors the spine's existing
  convention exactly: `ARCH-A-4` tracks the xUnit/NSubstitute/SDK
  feature-band deviation the same way — a plain-language statement of the
  gap, an owner, and a retirement condition that is either "the thing
  resolves itself" (v5 GAs) or "a human explicitly accepts the risk"
  (permanent override reason / accepted production risk), with no synthetic
  fallback plan invented for either row. ARCH-A-8 is not weaker or more
  hand-wavy than its sibling rows; it is the same pattern applied to a new
  risk.
- **Does it understate the risk given `status: final`?** No — and the reason
  is structural, not just wording. The Architecture Assumptions section's
  own preamble (line 733: "Spine-originated assumptions, keyed so `RQ-1` can
  name them as `UnretiredAssumption` blockers...") and AD-17's rule ("`RQ-1`
  ... records an `UnretiredAssumption` blocker for every unretired Product,
  Architecture, or Governance assumption in PRD section 8.1 or in this
  spine's Architecture Assumptions index") together mean ARCH-A-8 is not a
  passive risk register entry sitting beside a `status: final` document —
  it is wired directly into the release-qualification gate (`RQ-1`). V1
  literally cannot pass release qualification while ARCH-A-8 stays
  unretired, unless a human deliberately records the accepted-risk
  confirmation named in its "Retired when" column. That is a materially
  stronger resolution than the v2 finding's minimum ask (an `EXT-*` record
  or an `ARCH-A-*` assumption "with a stated fallback"); the enforcement
  mechanism (RQ-1 blocking) substitutes for an explicit technical fallback
  in a way consistent with how `ARCH-A-4` was already handled.
- **One residual, minor note (not a defect):** the retirement condition's
  "accepted production risk" branch does not name *who* signs off beyond
  the row's Owner field ("Architecture + FrontComposer Maintainer") — there
  is no explicit escalation path if those two owners disagree, unlike, say,
  ARCH-A-6's "Product confirms or replaces." This is a stylistic
  inconsistency across rows, not a gap specific to ARCH-A-8, and does not
  rise to a finding.
- **Verdict: CLOSED.** Adequately resolved; not an understatement.

### 2. [RESOLVED, and re-verified live] AD-9's Agent Framework / Dapr Agents GA clause is factually accurate today

- **Location:** AD-9 (line 149), new clause: "...Provider and Agent
  Framework SDKs remain unselected until that record commits an immutable
  target and compatibility command — a deliberate deferral to that decision
  record, not a reflection of ecosystem immaturity, since Microsoft Agent
  Framework and Dapr Agents are both already GA..."
- **What I checked (live, today):**
  - NuGet `Microsoft.Agents.AI` — latest stable is now **1.20.0**, published
    **2026-08-31**, up from the 1.19.0 v2 saw on 2026-08-22. Still GA, still
    shipping.
  - NuGet `Microsoft.Agents.AI.Workflows` — latest stable is now **1.20.0**,
    published **2026-08-31**, up from 1.13.0 at v2's check — the two
    packages' version numbers have since converged/aligned.
  - Microsoft Agent Framework GA date: confirmed 2026-04-03 (Visual Studio
    Magazine, Microsoft devblogs, techcommunity.microsoft.com all
    corroborate "1.0" shipping for .NET and Python).
  - Dapr Agents (Python): confirmed GA. CNCF's announcement
    ("General Availability of Dapr Agents Delivers Production Reliability
    for Enterprise AI") states Dapr Agents v1.0 reached GA on **2026-03-23**
    at KubeCon + CloudNativeCon Europe (Amsterdam), and is explicitly a
    **Python-only** framework — consistent with AD-18's own separate
    characterization of it as "Python Dapr Agents `DurableAgent`" being
    excluded from V1.
- **Assessment:** both halves of the new clause's factual claim are true
  and current as of today, not just true as of the v2 review's snapshot.
  The clause correctly frames "Unselected" as governance deferral rather
  than technical immaturity, which was exactly what v2 asked the spine to
  clarify. No inaccuracy found.
- **Verdict: CLOSED.**

### 3. Spot-check: has Fluent UI Blazor v5 reached GA since the v2 review?

- **What I checked:** live NuGet page for
  `Microsoft.FluentUI.AspNetCore.Components` today, 2026-09-09.
- **What I found:** unchanged from v2's snapshot. Latest **stable** is still
  **4.14.4** (published 2026-07-30); latest **prerelease** is still
  **5.0.0-rc.5-26219.1** (published 2026-08-09) — the exact version the
  spine pins. No newer RC and no GA release have shipped in the interval.
- **Conclusion:** nothing material changed in the world here. The RC framing
  in both the Stack table and the new ARCH-A-8 row remains accurate today;
  no update to the spine is needed on this point.

---

## What I did not independently re-verify

Per this pass's scope, I did not re-check Dapr runtime/workflow versions,
xUnit/NSubstitute/OpenTelemetry/FluentValidation/MediatR/bunit pins, or the
Dapr Conversation API alpha status — v2 checked all of these live and
nothing in the current spine text around them changed versus what v2 read.
The only textual deltas between the version v2 reviewed and this version are
the new ARCH-A-8 row and the new AD-9 clause, both addressed above.

---

## Verdict Summary

- **Finding 1 (Fluent UI Blazor v5 RC, untracked risk): CLOSED.** New
  `ARCH-A-8` row is a reasonable, convention-consistent resolution, and its
  binding into `RQ-1`'s `UnretiredAssumption` blocker mechanism means it
  does not understate the risk against `status: final` — it gates release
  qualification, not just documents intent.
- **Finding 2 (AD-9 "Unselected" framing vs. Agent Framework maturity):
  CLOSED.** The new AD-9 clause's factual claims — Microsoft Agent
  Framework GA and Dapr Agents (Python) GA — both re-verified live today and
  are accurate; Agent Framework has continued shipping past the v2
  snapshot (`Microsoft.Agents.AI` / `Microsoft.Agents.AI.Workflows` both now
  at 1.20.0, 2026-08-31).
- **Material change in the world since v2:** none found. Fluent UI Blazor v5
  is still RC (`5.0.0-rc.5-26219.1`), stable line still 4.14.4; no GA
  announcement exists as of today. The only real movement is routine patch
  cadence on the already-GA Microsoft Agent Framework packages, which does
  not change any spine conclusion.
- **Overall:** the spine's response to v2 is adequate and well-grounded; no
  new HIGH or MEDIUM findings raised in this delta pass.
