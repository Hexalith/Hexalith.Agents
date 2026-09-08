#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Story 5.3 verifier — govern provider models and pricing through live operations.

.DESCRIPTION
    Gates the Story 5.3 evidence manifest:
      1. the solution builds warning-free in the source dependency lane;
      2. the focused Story 5.3 suites pass (aggregate/replay, live EventStore command-query-projection,
         duplicate/conflict/regression, cross-tenant auth and no-disclosure including selection-denied,
         UI truth-flow, and poison-secret sweep);
      3. the full owning test projects pass, so the story's changes did not regress the module;
      4. the live catalog seams are actually bound rather than left deferred.
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

$focusedSuites = @(
    @{
        Project = 'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj'
        Filter  = 'FullyQualifiedName~ProviderCatalogAggregate|FullyQualifiedName~ProviderCatalogStateReplay|FullyQualifiedName~ProviderCatalogVersionRegression|FullyQualifiedName~ProviderSecretLeak'
        Gate    = 'AC1 aggregate replay, pricing, version regression, and poison-secret sweep'
    },
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Filter  = 'FullyQualifiedName~EventStoreProviderCatalogOperations|FullyQualifiedName~ProviderCatalogEventStoreIntegration|FullyQualifiedName~ProviderCatalogQuery|FullyQualifiedName~ProviderCatalogAuthorization'
        Gate    = 'AC1-AC4 live EventStore command-query-projection and cross-tenant selection denial'
    },
    @{
        Project = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        Filter  = 'FullyQualifiedName~ProviderCatalogTests|FullyQualifiedName~ProviderCatalogUiTests'
        Gate    = 'AC2 FrontComposer catalog truth flow without callability inference'
    }
)

$compositionSuites = @(
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Filter  = 'FullyQualifiedName~ProviderCatalogComposition|FullyQualifiedName~AgentSetupComposition'
        Gate    = 'the server container resolves the live catalog operations and projected reader'
    },
    @{
        Project = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        Filter  = 'FullyQualifiedName~AgentsUiComposition'
        Gate    = 'the FrontComposer container resolves the live catalog gateway from AddAgentsUiSetup'
    }
)

$liveBindings = @(
    @{
        Path   = 'src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs'
        Needle = 'ProviderCatalogCommandAcceptance'
        Gate   = 'the public catalog surface returns a structured accepted identity'
    },
    @{
        Path   = 'src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionHandler.cs'
        Needle = 'IAsyncDomainProjectionRebuildHandler'
        Gate   = 'the provider-catalog projection owns a real read-model slot'
    },
    @{
        Path   = 'src/Hexalith.Agents.Server/Ports/ProjectedProviderCatalogReader.cs'
        Needle = 'IProviderCatalogReader'
        Gate   = 'selection reads the live projected catalog'
    },
    @{
        Path   = 'src/Hexalith.Agents.Client/AgentsClient.cs'
        Needle = 'WithProviderCatalog'
        Gate   = 'the public client can bind live catalog operations'
    },
    @{
        Path   = 'src/Hexalith.Agents.UI/Services/Gateways/AgentsClientProviderCatalogGateway.cs'
        Needle = 'IProviderCatalogGateway'
        Gate   = 'the UI catalog gateway reads and writes through the public Agents client'
    },
    @{
        Path   = 'src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs'
        Needle = 'AgentsClientProviderCatalogGateway'
        Gate   = 'the FrontComposer composition can bind the live catalog gateway'
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
        Invoke-Gate -Name "story-5.3 focused — $($suite.Gate)" -Arguments @(
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
        Invoke-Gate -Name "story-5.3 composition — $($suite.Gate)" -Arguments @(
            'test', $suite.Project, '-c', 'Debug', '--no-build',
            '--filter', $suite.Filter, '/m:1', '/nr:false'
        )
    }

    Write-Host 'Gate: in-scope catalog seams are still present in source'
    foreach ($binding in $liveBindings) {
        $path = Join-Path $root $binding.Path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Story 5.3 requires '$($binding.Path)' so that $($binding.Gate)."
        }

        $content = Get-Content -LiteralPath $path -Raw
        if (-not $content.Contains($binding.Needle, [StringComparison]::Ordinal)) {
            throw "Story 5.3 requires '$($binding.Path)' to keep $($binding.Gate) (missing '$($binding.Needle)')."
        }

        Write-Host "  ok: $($binding.Gate)"
    }

    Write-Host 'Story 5.3 verification succeeded.'
}
finally {
    Pop-Location
}
