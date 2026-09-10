# Verified-Current Reviewer Gate — Round 9

**Reviewer:** Independent architecture reviewer (verified-current lens)
**Date performed:** 2026-09-10
**Target:** `ARCHITECTURE-SPINE.md` (architecture-agents-2026-06-23-2), Stack table (lines 494-522) and Architecture Assumptions table (lines 767-787), cross-referenced against the round-5 Update claims in the same file and `VALIDATION-REPORT-2026-09-09-4.md`
**Method:** Direct execution of `dotnet --info` / `dotnet --list-sdks` / `dotnet --list-runtimes` against this repo's actual `global.json`-resolved toolchain; `git submodule status` against all five gitlinked submodules named in the Stack table; live WebSearch/WebFetch against `dotnet/core` release notes, NuGet.org package pages, GitLab/HeroDevs advisory database, and Fluent UI Blazor release-notes blog. Nothing accepted from the spine's own prose or from the round-8 review without independent re-verification.

## Overall Verdict: **FAIL** (one Critical finding: the headline security remediation this round does not do what the document claims)

The two items this round-5 Update pass claims to have fixed were checked from scratch. One (the `Hexalith.EventStore` gitlink) is now genuinely correct. The other — the claim that pinning `global.json` to `10.0.400` "clos[es] the `CVE-2026-69522` ... exposure" — is **false as configured**, verified by literally running the repo's own toolchain. `10.0.400` resolves to .NET Runtime `10.0.11`, a pre-fix build; the fix shipped one patch release later, in runtime `10.0.12` / SDK `10.0.401`, released 2026-09-08. The spine escaped the abandoned `10.0.3xx` band but stopped one patch short of the version that actually contains the fix it cites by CVE number.

---

## Findings

### F1 — Critical — `.NET SDK 10.0.400` does **not** contain the `CVE-2026-69522` fix the spine cites; the fix is in `10.0.401`, one patch later

**Location:** Stack table, `.NET SDK` row (line 498); `ARCH-A-4` row (line 778, "SDK sub-item retired 2026-09-09").

**Claim being checked:** "`.NET SDK` \| `10.0.400` with `rollForward: latestPatch` from root `global.json` — bumped at this round-5 Update pass ... closing the `CVE-2026-69522` (CVSS 8.8, RCE) exposure." `ARCH-A-4` goes further and marks the SDK sub-item **retired**, i.e. no longer an open risk.

**What I found, directly against this repo's own toolchain:**
```
$ dotnet --info
.NET SDK: Version: 10.0.400
Host: Version: 10.0.11
.NET SDKs installed: 10.0.302, 10.0.400
```
`global.json` (confirmed by direct read) pins `"version": "10.0.400"`, `"rollForward": "latestPatch"`. Per Microsoft's own `rollForward` semantics, `latestPatch` resolves to the *highest installed patch within the same feature band* (`10.0.4xx`) and never crosses into a new band — so it will use `10.0.401` **only if `10.0.401` is actually installed**. On this machine (and on any fresh clone whose install tooling follows `global.json`'s declared `10.0.400`), only `10.0.400` is installed, so the toolchain resolves to exactly `10.0.400` / runtime `10.0.11`.

Cross-checked against primary sources (`dotnet/core` release notes, fetched live):
- `github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md` — released **2026-08-11**. SDKs shipped in this bundle: `10.0.400`, `10.0.303`, `10.0.111`. Runtime version: `10.0.11`. `CVE-2026-69522` is **not** in this bundle's fix list.
- `github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md` — released **2026-09-08**. SDKs shipped: `10.0.401`, `10.0.112`. Runtime version: `10.0.12`. This bundle's six fixed CVEs are `CVE-2026-69439`, `CVE-2026-71328`, `CVE-2026-69522`, `CVE-2026-69304`, `CVE-2026-58649`, `CVE-2026-69806`.

So `10.0.400` is literally the SDK that shipped **one bundle before** the fix; `CVE-2026-69522` was fixed only starting with SDK `10.0.401` / runtime `10.0.12`. The spine's own headline sentence — "bumped ... to match every sibling repo's pin, closing the CVE-2026-69522 ... exposure" — is checking the wrong number: `10.0.400` escapes the abandoned `10.0.3xx` *band* (a real, separate improvement — see F2) but does not reach the patch that contains the cited fix. `ARCH-A-4`'s claim that "SDK sub-item retired 2026-09-09" is premature: the condition it purports to retire (CVE exposure) is not actually closed.

**Mitigating nuance (why this might be High rather than Critical, depending on deployment target):** `CVE-2026-69522`'s vulnerable component is `Microsoft.DiaSymReader.Native`, which HeroDevs' and GitLab's advisory writeups both describe as **Windows-only** — "the native symbol reader that the .NET shared framework carries on Windows... Linux and macOS layouts do not carry it at all." This repo's dev environment is Linux (WSL2/Ubuntu), so the specific RCE path this CVE describes may not be reachable there. `CVE-2026-69439` and `CVE-2026-71328` in the same servicing wave also appear (per search snippets) to be the same `DiaSymReader.Native` component family, so likely also Windows-only. I was not able to independently confirm the platform scope of the remaining three CVEs in the bundle (`CVE-2026-69304`, `CVE-2026-58649`, `CVE-2026-69806`) in this pass. Regardless of platform scope, the spine's claim is unqualified — it asserts the exposure is closed full stop, not "closed for our Linux dev boxes only" — and any Windows build agent, CI runner, or developer machine provisioned strictly from `global.json`'s declared `10.0.400` remains on the pre-fix runtime today.

**Fix:** Bump `global.json`'s pinned `version` to `"10.0.401"` (the actual patch containing the fix), not `"10.0.400"`. Alternatively, if the intent was always to track "latest patch, whatever that is," document that explicitly and verify at pin time — not doc-write time — which patch is actually resolved, since `rollForward: latestPatch` only rolls forward to patches that are *installed*, not to whatever is *latest upstream*. Until this is corrected, `ARCH-A-4`'s SDK sub-item should not be marked retired, and the Stack table's "closing the CVE-2026-69522 ... exposure" clause should be corrected or scoped to "escapes the abandoned 10.0.3xx band; does not yet reach the 10.0.401 patch containing the CVE-2026-69522 fix."

### F2 — Informational (confirms part of the spine is correct) — Escaping the `10.0.3xx` band was real and necessary, independent of F1

The round-8 review (`review-2026-09-09-verified-current-v8.md`, F2/F3) had already established that `10.0.303` was the last SDK ever shipped in the `10.0.3xx` band (dropped entirely as of the `10.0.12` bundle) and that a project capped there via `rollForward: latestPatch` could never reach any fix past `10.0.303`, regardless of CVE. Moving the pin's *feature band* from `10.0.3xx` to `10.0.4xx` was a correct and necessary step — it's just not sufficient by itself to reach the specific CVE fix the spine cites (F1). Both things are true at once: band-escape done correctly, patch-level claim wrong.

### F3 — Low — No newer disclosed .NET CVE found since the `10.0.12` / 2026-09-08 wave

Searched specifically for .NET/.NET 10 security advisories dated after 2026-09-08 (through today, 2026-09-10). No newer CVE or servicing release was found; the next regular Patch Tuesday cadence would not be expected until October 2026. This is a negative result (absence of evidence), consistent with the short window since the last servicing wave, not a claim that none exists.

### F4 — Confirmed — All five Stack-table gitlinks are now exact, live matches (round-8's F1 is resolved)

Ran `git -C /home/administrator/projects/hexalith/agents submodule status` against all five gitlinked submodules named in the Stack table, at the moment of this review:

| Submodule | Stack table claim | Live `git submodule status` | Match |
| --- | --- | --- | --- |
| Hexalith.EventStore | `0994c378` | `0994c37814c37dac7667a209dbd0659125aac49e` | ✓ exact prefix |
| Hexalith.Conversations | `73bcee6f` | `73bcee6f04479d4743d5a65ce929728e22687d7d` | ✓ exact prefix |
| Hexalith.Parties | `fa423985` | `fa42398552fba1c80eb2760791517659d6d1313a` | ✓ exact prefix |
| Hexalith.Tenants | `54fc4040` | `54fc4040dc6348e5560fffc246a27e72dc6558fe` | ✓ exact prefix |
| Hexalith.FrontComposer | `053b2008` | `053b2008307d4e476c0d4329e6c47763c301d43e` | ✓ exact prefix |

This closes round-8's F1 (the `Hexalith.EventStore` gitlink was ~2h11m stale at that review). All five are exact, non-stale matches right now. No `+` prefix on any line (no submodule has a checked-out commit that differs from the parent's recorded index), confirming these are the actual committed pins, not working-tree drift.

### F5 — Confirmed — `global.json` literally reads as the spine claims

Direct read of `/home/administrator/projects/hexalith/agents/global.json`:
```json
{ "sdk": { "version": "10.0.400", "rollForward": "latestPatch" } }
```
This matches the Stack table's `.NET SDK` row and `ARCH-A-4` verbatim — round-8's F2 (documentation-vs-reality gap, where the doc claimed a bump that hadn't landed) is genuinely closed; the bump to `10.0.400` really did land in the repo. The remaining problem is not "undone as claimed" (round 8's issue) but "insufficient as claimed" (this round's F1).

### F6 — Confirmed accurate — Other spot-checked Stack rows, verified live

| Row | Spine claim | Verified via | Verdict |
| --- | --- | --- | --- |
| Dapr .NET packages / Dapr Workflow | `1.18.5` | `nuget.org/packages/Dapr.Client` fetched live: latest stable is `1.18.5` (updated 2026-07-25); `1.19.0` exists only as `-preview.1`/`-preview.2` | **True**, current |
| MediatR | `14.2.0` | `nuget.org/packages/MediatR` fetched live: `14.2.0` is latest (updated 2026-07-02), no newer version listed | **True**, current |
| FluentValidation | `12.1.1` | `nuget.org` search/listing: `12.1.1` is latest | **True**, current |
| OpenTelemetry | `1.18.0` | `nuget.org/packages/opentelemetry`: `1.18.0` latest, updated 8/21/2026 | **True**, current |
| xUnit v3 (catalog) | `4.0.0` | `xunit.net/releases/v3/4.0.0` fetched live: released 2026-08-14, no newer version referenced on the page | **True**, current |
| Fluent UI Blazor | `5.0.0-rc.5-26219.1`, RC not GA (`ARCH-A-8`) | `dvoituron.com` RC5 post (2026-08-07) plus live search for a v5 GA announcement: none found as of 2026-09-10, still RC | **True**, `ARCH-A-8` substance still valid, no retirement trigger fired |

No stale, yanked, or non-existent versions found among the rows spot-checked.

### F7 — `ARCH-A-4`'s SDK sub-item should be reopened, not retired

The Architecture Assumptions table (line 778) states: "**SDK portion retired 2026-09-09 (round-5 Update):** `global.json` now pins `10.0.400`/`rollForward: latestPatch`, matching every sibling repo and closing the `CVE-2026-69522` ... exposure." Per F1, the closure claim is false as configured. A row cannot be marked retired when the condition it retires on has not actually occurred. This should revert to open (or a new interim state distinguishing "band escaped" from "CVE fix reached") with a corrected `TargetRetirementDate`, until `global.json` is bumped to `10.0.401` (or the currently-latest `10.0.4xx` patch) and re-verified.

---

## Severity Counts

| Severity | Count |
| --- | --- |
| Critical | 1 (F1) |
| High | 0 |
| Medium | 1 (F7 — direct consequence of F1, tracked separately because it's a distinct document defect: premature retirement marking) |
| Low | 1 (F3) |
| Informational | 2 (F2, F4/F5/F6 grouped as confirmations) |

## Sources Consulted

- https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md
- https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md
- https://www.herodevs.com/vulnerability-directory/cve-2026-69522
- https://advisories.gitlab.com/nuget/microsoft.diasymreader.native/CVE-2026-69522/
- https://advisories.gitlab.com/nuget/microsoft.diasymreader.native/CVE-2026-69439/
- https://www.herodevs.com/vulnerability-directory/cve-2026-71328
- https://www.bleepingcomputer.com/news/microsoft/microsoft-september-2026-patch-tuesday-fixes-966-flaws-2-zero-days/
- https://www.nuget.org/packages/Dapr.Client
- https://www.nuget.org/packages/MediatR
- https://www.nuget.org/packages/opentelemetry/
- https://xunit.net/releases/v3/4.0.0
- https://dvoituron.com/2026/08/07/fluentui-blazor-5-rc5/
- Local/direct: `dotnet --info`, `dotnet --list-sdks`, `dotnet --list-runtimes` on this repo's resolved toolchain; `/home/administrator/projects/hexalith/agents/global.json`; `git -C /home/administrator/projects/hexalith/agents submodule status references/Hexalith.EventStore references/Hexalith.Conversations references/Hexalith.Parties references/Hexalith.Tenants references/Hexalith.FrontComposer`
