#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Story 5.2 verifier — configure hexa through live EventStore operations.

.DESCRIPTION
    Gates the Story 5.2 evidence manifest:
      1. the solution builds warning-free in the source dependency lane;
      2. the focused Story 5.2 suites pass (aggregate replay, live command-query-projection, duplicate/conflict/
         replay determinism, cross-tenant authorization and no-disclosure, and the UI truth flow);
      3. the full owning test projects pass, so the story's changes did not regress the module;
      4. the in-scope administration seams are actually bound live rather than left deferred.

    Gate 4 is what keeps this verifier honest: the focused tests would still pass against a deferred dispatcher
    if the live types were never wired. It resolves the composed containers and asserts the live dispatcher,
    administration operations, context provider, and UI setup gateway actually come out of DI (and that an
    unconfigured host stays fail-closed). The file anchors that follow are only a cheap tripwire for a live seam
    being deleted outright — the DI suites are the real evidence.
#>

param(
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Hexalith.Agents.slnx'
$testProjects = @(
    'test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj',
    'test/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj',
    'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj',
    'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj',
    'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
)

# The focused Story 5.2 evidence: each filter names the suites that prove one acceptance criterion.
$focusedSuites = @(
    @{
        Project = 'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentStateReplay'
        Gate    = 'AC1 aggregate replay determinism'
    },
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentAdministrationOrchestrator|FullyQualifiedName~EventStoreAgentCommandDispatcher|FullyQualifiedName~EventStoreAgentAdministrationOperations'
        Gate    = 'AC1/AC4 authorize-then-dispatch over the live command path'
    },
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentSetupProjection|FullyQualifiedName~AgentSetupQueryHandler'
        Gate    = 'AC2/AC3 projected setup truth and persisted read-model end state'
    },
    @{
        Project = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentConfiguration|FullyQualifiedName~AgentsClientSetupGateway'
        Gate    = 'AC2 FrontComposer truth flow without callability inference'
    }
)

# Gate 4a: the composition suites that resolve the real containers. A deferred-only host must fail these.
$compositionSuites = @(
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentSetupComposition|FullyQualifiedName~HttpAgentAdministrationContextProvider|FullyQualifiedName~AgentsOperationEndpoints'
        Gate    = 'the server container resolves the live dispatcher, operations, and trusted context provider'
    },
    @{
        Project = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentsUiComposition'
        Gate    = 'the FrontComposer container resolves the live setup gateway only when an Agent target is named'
    }
)

# Gate 4b anchors: a tripwire for a live seam being deleted outright. Secondary to the composition suites above.
$liveBindings = @(
    @{
        Path   = 'src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs'
        Needle = 'SubmitCommandAsync'
        Gate   = 'the command dispatcher submits through the EventStore gateway'
    },
    @{
        Path   = 'src/Hexalith.Agents.Server/Projections/AgentSetupProjectionHandler.cs'
        Needle = 'IAsyncDomainProjectionRebuildHandler'
        Gate   = 'the Agent setup projection owns a real read-model slot'
    },
    @{
        Path   = 'src/Hexalith.Agents.Server/Composition/AgentSetupServiceCollectionExtensions.cs'
        Needle = 'EventStoreAgentAdministrationOperations'
        Gate   = 'the public administration surface is bound to the live implementation'
    },
    @{
        Path   = 'src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs'
        Needle = 'GetSetupAsync'
        Gate   = 'the UI setup gateway reads through the public Agents client'
    },
    @{
        Path   = 'src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs'
        Needle = 'AgentsClientSetupGateway'
        Gate   = 'the FrontComposer composition can bind the live setup gateway'
    }
)

function Invoke-Gate {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    Write-Host "Gate: $Name"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Gate '$Name' failed with exit code $LASTEXITCODE."
    }
}

Push-Location $root
try {
    if (-not $SkipBuild) {
        Invoke-Gate -Name 'restore' -Arguments @(
            'restore', $solution, '-p:Configuration=Debug', '-p:UseHexalithProjectReferences=true',
            '-p:NuGetAudit=false', '/m:1', '/nr:false'
        )
        Invoke-Gate -Name 'source-build' -Arguments @(
            'build', $solution, '-c', 'Debug', '--no-restore', '-warnaserror',
            '-p:UseHexalithProjectReferences=true', '/m:1', '/nr:false'
        )
    }

    foreach ($suite in $focusedSuites) {
        Invoke-Gate -Name "story-5.2 focused — $($suite.Gate)" -Arguments @(
            'test', $suite.Project, '-c', 'Debug', '--no-build',
            '--filter', $suite.Filter, '/m:1', '/nr:false'
        )
    }

    foreach ($testProject in $testProjects) {
        Invoke-Gate -Name "regression — $testProject" -Arguments @(
            'test', $testProject, '-c', 'Debug', '--no-build', '/m:1', '/nr:false'
        )
    }

    foreach ($suite in $compositionSuites) {
        Invoke-Gate -Name "story-5.2 composition — $($suite.Gate)" -Arguments @(
            'test', $suite.Project, '-c', 'Debug', '--no-build',
            '--filter', $suite.Filter, '/m:1', '/nr:false'
        )
    }

    Write-Host 'Gate: in-scope administration seams are still present in source'
    foreach ($binding in $liveBindings) {
        $path = Join-Path $root $binding.Path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Story 5.2 requires '$($binding.Path)' so that $($binding.Gate)."
        }

        $content = Get-Content -LiteralPath $path -Raw
        if (-not $content.Contains($binding.Needle, [StringComparison]::Ordinal)) {
            throw "Story 5.2 requires '$($binding.Path)' to keep $($binding.Gate) (missing '$($binding.Needle)')."
        }

        Write-Host "  ok: $($binding.Gate)"
    }

    Write-Host 'Story 5.2 verification succeeded.'
}
finally {
    Pop-Location
}
