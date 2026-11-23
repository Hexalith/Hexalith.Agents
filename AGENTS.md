# AI assistant instructions

Before working in this repository, read
[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
(in the `references/Hexalith.AI.Tools` submodule) and follow it.

## Git Submodules

- Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
- Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
- If nested submodules are initialized accidentally, deinitialize them before continuing.
<!-- hexalith-cicd:begin -->
CI/CD is owned by Hexalith.CiCd.

Hexalith.CiCd owns `.github/workflows/**`, `.github/hexalith-ci.json`,
`.github/dependabot.yml`, the `references/Hexalith.Builds` gitlink and its
`.gitmodules` section, the delivered CI/CD instruction blocks in `AGENTS.md`,
`CLAUDE.md` and `.github/copilot-instructions.md`, and the required
`permissions.deny` entries in `.claude/settings.json`. Do not edit those owned
files or portions from a module. Propose policy changes in Hexalith.CiCd.
Preserve instructions, settings and submodule sections outside those portions.

At the repository root, the guard forbids release configuration
(`.releaserc*`, `release.config.*`) and commitlint configuration
(`commitlint.config.*`, `.commitlintrc*`). In the root `package.json` it forbids the
top-level `release` and `commitlint` keys and dependency entries for
`semantic-release`, `@semantic-release/*` and `@commitlint/*` under
`dependencies`, `devDependencies`, `optionalDependencies` and
`peerDependencies`. It also forbids commitlint lines in `.husky/commit-msg`
and files matching `**/Directory.Build.rsp` or `**/bmad-testarch-ci/**`
throughout the repository.

When CI fails, fix the product code, tests or dependencies. If the pipeline is
defective, report the failure and run link to Hexalith.CiCd without working
around it. Before every push, run `python3 references/Hexalith.Builds/check.py`
from the module root and require success. Do not disable or suppress dependency
audits, skip or filter out test projects, weaken checks, or add CI skip markers
(including `[skip ci]` variants) or `skip-checks` commit trailers. Do not write
tests that assert workflow YAML text.

Keep product gates: compilation, tests, security and dependency scans, commit
format and package publishing. Module-specific product checks belong in custom
actions enabled through Hexalith.CiCd. Checks of CI itself, agent tooling, story
evidence or workflow YAML do not belong in module CI. Releases follow the
centrally scheduled nightly process.

Start each product branch from current `main`: fetch `origin main` immediately
before creating the branch from `origin/main`. Do not reuse an older base that
predates delivered CI/CD changes. Use the restricted module credential. Report
permission refusals without switching credentials or broadening access.
GitHub permits PR Update branch with the restricted module credential even
when it imports changed workflows. Do not use it to import owned CI/CD changes.
Fetch current `main`, create a new product branch, and reapply only product
changes instead.

Launch Claude Code from the module root. The project-local `Edit` deny rules
protect the whole-owned workflow/config files and Builds subtree while keeping
reads available. File rules cannot target JSON keys or instruction marker
ranges: these instructions and the shared guard protect the owned portions of
settings, instruction files and `.gitmodules`. Never install these denies in
machine-wide settings or work around them through another tool.
<!-- hexalith-cicd:end -->

<!-- bmad:context -->
<!-- Verified 2026-11-22 against 596bbf3. Managed by bmad-project-context. -->

## Hexalith.Agents

Hexalith.Agents implements event-sourced agent interactions and provider governance.
Use `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` for design decisions and `_bmad-output/implementation-artifacts/sprint-status.yaml` for current story status.

## Running and verifying

- Use the .NET SDK selected by `global.json` (11.0.100 with `latestPatch`).
- For Debug builds, follow “Build and test” in `CONTRIBUTING.md`. The published `hexalith-module prepare-build` checks direct dependencies against the parent’s recorded revisions. Use a separate checkout at those revisions when the working copy must stay on upstream `main`.
- Run Debug builds and per-project tests with the documented `source_dotnet` wrapper. Regenerate its build environment after moving the checkout or changing a submodule pin. Release builds use NuGet packages.

## Command handling

- Authorize provider-catalog writes before checking whether the status reader supports `("system", "provider-catalog")`. Dispatch only when it does.
- When copying an `AgentCommandIntent` during trusted admission, preserve its exact `Authority` object, identifiers, payload bytes and existing metadata. Add only authenticated `trust:` entries.

<!-- /bmad:context -->
