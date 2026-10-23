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
$eventStoreCoordinatorProject = 'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj'
$eventStoreClientProject = 'references/Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj'
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
        Classes = @('*ProviderCatalogAggregateTests', '*ProviderCatalogStateReplayTests', '*ProviderCatalogVersionRegressionTests', '*ProviderSecretLeakTests', '*TenantProviderEnablementAggregateTests', '*DataHandlingGraceDeadlineTests')
        Gate    = 'AC1 aggregate replay, pricing, version regression, and poison-secret sweep'
    },
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Classes = @('*EventStoreProviderCatalogOperationsTests', '*ProviderCatalogEventStoreIntegrationTests', '*ProviderCatalogCoordinationIntegrationTests', '*ProviderCatalogQueryTests', '*ProviderCatalogAuthorizationTests', '*TenantProviderCatalogIntegrationTests', '*ProviderCatalogMigrationTests', '*ProjectedProviderCatalogReaderTests', '*HttpAgentAdministrationContextProviderTests')
        Gate    = 'AC1-AC4 live EventStore command-query-projection and cross-tenant selection denial'
    },
    @{
        Project = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        Classes = @('*ProviderCatalogTests', '*ProviderCatalogUiTests', '*ProviderCatalogGovernanceUiTests')
        Gate    = 'AC2 FrontComposer catalog truth flow without callability inference'
    }
)

$compositionSuites = @(
    @{
        Project = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        Classes = @('*ProviderCatalogCompositionTests', '*AgentSetupCompositionTests')
        Gate    = 'the server container resolves the live catalog operations and projected reader'
    },
    @{
        Project = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        Classes = @('*AgentsUiCompositionTests')
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

function Invoke-TestGate {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Project,
        [string[]] $Classes = @(),
        [string[]] $Methods = @()
    )

    $assembly = [IO.Path]::GetFileNameWithoutExtension($Project)
    $path = Join-Path $root ([IO.Path]::Combine([IO.Path]::GetDirectoryName($Project), 'bin', 'Debug', 'net10.0', "$assembly.dll"))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Gate '$Name' requires built test assembly '$path'."
    }

    $selections = @(
        foreach ($class in $Classes) { @{ Option = '-class'; Value = $class } }
        foreach ($method in $Methods) { @{ Option = '-method'; Value = $method } }
    )
    if ($selections.Count -eq 0) {
        $selections = @(@{ Option = ''; Value = 'all tests' })
    }

    Write-Host "Gate: $Name"
    foreach ($selection in $selections) {
        $arguments = @($path, '-failSkips')
        if ($selection.Option) {
            $arguments += @($selection.Option, $selection.Value)
        }

        $nativeErrorPreference = $PSNativeCommandUseErrorActionPreference
        try {
            # Stream progress immediately while retaining this invocation's summary evidence.
            $PSNativeCommandUseErrorActionPreference = $false
            $output = @()
            & dotnet @arguments 2>&1 | Tee-Object -Variable output | ForEach-Object { Write-Host $_ }
            $exitCode = $LASTEXITCODE
        }
        finally {
            $PSNativeCommandUseErrorActionPreference = $nativeErrorPreference
        }

        if ($exitCode -ne 0) {
            throw "Gate '$Name' failed for '$($selection.Value)' with exit code $exitCode."
        }

        $headers = @()
        for ($index = 0; $index -lt @($output).Count; $index++) {
            if (([string] $output[$index]).Trim() -ceq '=== TEST EXECUTION SUMMARY ===') {
                $headers += $index
            }
        }
        if ($headers.Count -eq 0) {
            throw "Gate '$Name' produced no test summary for '$($selection.Value)'."
        }
        if ($headers.Count -ne 1) {
            throw "Gate '$Name' produced ambiguous test summary sections for '$($selection.Value)'."
        }

        # Only the expected assembly's row within the runner summary is authoritative.
        $rowPrefix = '^\s*' + [regex]::Escape($assembly) + '\s+Total:'
        $rows = @()
        for ($index = $headers[0] + 1; $index -lt @($output).Count; $index++) {
            if ([string] $output[$index] -cmatch $rowPrefix) {
                $rows += [string] $output[$index]
            }
        }
        if ($rows.Count -eq 0) {
            throw "Gate '$Name' produced no test summary for assembly '$assembly' and '$($selection.Value)'."
        }
        if ($rows.Count -ne 1) {
            throw "Gate '$Name' produced ambiguous test summary rows for '$($selection.Value)'."
        }

        # xUnit emits only Total: 0 when a native selector matches no tests.
        $rowPattern = $rowPrefix + '\s*(?<Total>\d+)(?:,\s*Errors:\s*(?<Errors>\d+),\s*Failed:\s*(?<Failed>\d+),\s*Skipped:\s*(?<Skipped>\d+),\s*Not Run:\s*(?<NotRun>\d+))?(?:,\s*Time:\s*[^\r\n]+)?\s*$'
        $summary = [regex]::Match($rows[0], $rowPattern)
        if (-not $summary.Success) {
            throw "Gate '$Name' produced an invalid test summary row for '$($selection.Value)'."
        }
        if ([int] $summary.Groups['Total'].Value -le 0) {
            throw "Gate '$Name' executed 0 tests; selection matched nothing: $($selection.Value)."
        }
        if (-not $summary.Groups['Errors'].Success) {
            throw "Gate '$Name' produced an incomplete test summary row for '$($selection.Value)'."
        }
        if ([int] $summary.Groups['Errors'].Value -gt 0 -or [int] $summary.Groups['Failed'].Value -gt 0) {
            throw "Gate '$Name' reported errors or failures for '$($selection.Value)'."
        }
        if ([int] $summary.Groups['Skipped'].Value -gt 0 -or [int] $summary.Groups['NotRun'].Value -gt 0) {
            throw "Gate '$Name' skipped or did not run tests for '$($selection.Value)'."
        }
    }
}

Push-Location $root
try {
    if (-not $SkipBuild) {
        Invoke-Gate -Name 'restore' -Arguments @(
            'restore', $solution, '-p:Configuration=Debug', '-p:UseHexalithProjectReferences=true',
            '/m:1', '/nr:false'
        )
        Invoke-Gate -Name 'source-build' -Arguments @(
            'build', $solution, '-c', 'Debug', '--no-restore', '-warnaserror',
            '-p:UseHexalithProjectReferences=true', '/m:1', '/nr:false'
        )
        Invoke-Gate -Name 'EventStore coordinator restore' -Arguments @(
            'restore', $eventStoreCoordinatorProject, '/m:1', '/nr:false'
        )
        Invoke-Gate -Name 'EventStore coordinator build' -Arguments @(
            'build', $eventStoreCoordinatorProject, '-c', 'Debug', '--no-restore', '-warnaserror',
            '/m:1', '/nr:false'
        )
        Invoke-Gate -Name 'EventStore client restore' -Arguments @(
            'restore', $eventStoreClientProject, '/m:1', '/nr:false'
        )
        Invoke-Gate -Name 'EventStore client build' -Arguments @(
            'build', $eventStoreClientProject, '-c', 'Debug', '--no-restore', '-warnaserror',
            '/m:1', '/nr:false'
        )
    }

    Invoke-TestGate -Name 'story-5.3 focused — EventStore coordinated stream guard and terminal conflict status' `
        -Project $eventStoreCoordinatorProject `
        -Classes @('*CoordinatedCommandActorTests', '*CommandStatusControllerTests') `
        -Methods @('*Fenced_stale_source_after_a_target_reconciliation_miss*', '*Handle_CoordinatedSourceConflict*')
    Invoke-TestGate -Name 'story-5.3 focused — EventStore gateway owning tenant status' `
        -Project $eventStoreClientProject `
        -Methods @('*GetCommandStatusAsync_UsesConfiguredPathAndDeserializesRejectedStatus', '*GetCommandStatusAsync_PreservesCompletedZeroEventCount')

    foreach ($suite in $focusedSuites) {
        Invoke-TestGate -Name "story-5.3 focused — $($suite.Gate)" -Project $suite.Project -Classes $suite.Classes
    }

    foreach ($testProject in $testProjects) {
        Invoke-TestGate -Name "regression — $testProject" -Project $testProject
    }

    foreach ($suite in $compositionSuites) {
        Invoke-TestGate -Name "story-5.3 composition — $($suite.Gate)" -Project $suite.Project -Classes $suite.Classes
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
