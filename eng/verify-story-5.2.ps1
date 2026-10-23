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

.PARAMETER SkipBuild
    Skips restore and build while retaining the package floor, focused tests, regressions, composition, and anchors.

.PARAMETER PackageFloorOnly
    Runs only the effective package-mode Hexalith.EventStore version-floor check.

.PARAMETER PackageFloorProjectPath
    Overrides the project evaluated by the package-floor check. Relative paths resolve from the repository root.
#>

param(
    [switch] $SkipBuild,
    [switch] $PackageFloorOnly,
    [string] $PackageFloorProjectPath
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Hexalith.Agents.slnx'
$testProjects = @(
    @{ Path = 'test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj'; MinimumExpectedTests = 529 },
    @{ Path = 'test/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj'; MinimumExpectedTests = 6 },
    @{ Path = 'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj'; MinimumExpectedTests = 787 },
    @{ Path = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'; MinimumExpectedTests = 573 },
    @{ Path = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'; MinimumExpectedTests = 1073 }
)

function Assert-EventStorePackageFloor {
    $minimumVersion = [Version]'3.106.0'
    $projectPath = if ([string]::IsNullOrWhiteSpace($PackageFloorProjectPath)) {
        'src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj'
    }
    else {
        $PackageFloorProjectPath
    }
    $arguments = @(
        'msbuild', $projectPath, '-nologo',
        '-getItem:PackageVersion', '-p:Configuration=Release',
        '-p:UseHexalithProjectReferences=false', '-p:NuGetAudit=false', '/nr:false'
    )
    $nativeErrorPreference = $PSNativeCommandUseErrorActionPreference
    $standardErrorPath = [IO.Path]::GetTempFileName()
    try {
        $PSNativeCommandUseErrorActionPreference = $false
        $output = & dotnet @arguments 2> $standardErrorPath
        $exitCode = $LASTEXITCODE
        $standardError = @(Get-Content -LiteralPath $standardErrorPath)
    }
    finally {
        $PSNativeCommandUseErrorActionPreference = $nativeErrorPreference
        Remove-Item -LiteralPath $standardErrorPath -Force -ErrorAction SilentlyContinue
    }

    if ($exitCode -ne 0) {
        $output | ForEach-Object { Write-Host $_ }
        $standardError | ForEach-Object { Write-Host $_ }
        throw "Unable to resolve the effective package-mode Hexalith.EventStore PackageVersion rows."
    }

    $standardError | ForEach-Object { Write-Host $_ }

    try {
        $evaluation = [string]::Join("`n", $output) | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Unable to parse the effective package-mode PackageVersion evaluation. $($_.Exception.Message)"
    }

    $eventStorePackageVersions = @(
        $evaluation.Items.PackageVersion |
            Where-Object {
                $identity = [string] $_.Identity
                $identity.Equals('Hexalith.EventStore', [StringComparison]::OrdinalIgnoreCase) -or
                $identity.StartsWith('Hexalith.EventStore.', [StringComparison]::OrdinalIgnoreCase)
            }
    )
    if ($eventStorePackageVersions.Count -eq 0) {
        throw "No effective package-mode Hexalith.EventStore PackageVersion rows were found."
    }

    $effectiveVersions = [System.Collections.Generic.List[string]]::new()
    foreach ($packageVersion in $eventStorePackageVersions) {
        $identity = [string] $packageVersion.Identity
        $versionText = ([string] $packageVersion.Version).Trim()
        $versionMatch = [regex]::Match(
            $versionText,
            '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)' +
                '(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*))?' +
                '(?:\+(?<metadata>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$'
        )
        if (-not $versionMatch.Success) {
            throw "The effective package-mode PackageVersion for '$identity' ('$versionText') is not a valid SemVer 2 version."
        }

        try {
            $effectiveVersion = [Version]::new(
                [int] $versionMatch.Groups['major'].Value,
                [int] $versionMatch.Groups['minor'].Value,
                [int] $versionMatch.Groups['patch'].Value
            )
        }
        catch {
            throw "The effective package-mode PackageVersion for '$identity' ('$versionText') is outside the supported version range."
        }

        $isPrereleaseAtFloor =
            $effectiveVersion -eq $minimumVersion -and $versionMatch.Groups['prerelease'].Success
        if ($effectiveVersion -lt $minimumVersion -or $isPrereleaseAtFloor) {
            throw "Story 5.2 requires Hexalith.EventStore $minimumVersion or later in package mode; effective version for '$identity' is $versionText. This release must include no-op result-payload forwarding, command-status reads, and trusted-extension admission."
        }

        $effectiveVersions.Add($versionText)
    }

    $selectedVersions = [string]::Join(', ', @($effectiveVersions | Sort-Object -Unique))
    Write-Host "Gate: EventStore package floor $($eventStorePackageVersions.Count) selected rows at $selectedVersions >= $minimumVersion"
}

# The focused Story 5.2 evidence: each class list names the suites that prove one acceptance criterion.
$focusedSuites = @(
    @{
        Assembly = 'test/Hexalith.Agents.Contracts.Tests/bin/Debug/net10.0/Hexalith.Agents.Contracts.Tests.dll'
        Classes  = @('Hexalith.Agents.Contracts.Tests.AgentOperationContractsTests')
        Gate     = 'AC1 additive receipt contracts, named effects, and legacy payload compatibility'
    },
    @{
        Assembly = 'test/Hexalith.Agents.Tests/bin/Debug/net10.0/Hexalith.Agents.Tests.dll'
        Classes  = @(
            'Hexalith.Agents.Tests.AgentStateReplayTests',
            'Hexalith.Agents.Tests.AgentLifecycleConfigurationVersionTests',
            'Hexalith.Agents.Tests.AgentSetupDomainResultTests'
        )
        Gate = 'AC1 aggregate replay, result payload, and configuration-version determinism'
    },
    @{
        Assembly = 'test/Hexalith.Agents.Server.Tests/bin/Debug/net10.0/Hexalith.Agents.Server.Tests.dll'
        Classes  = @(
            'Hexalith.Agents.Server.Tests.AgentAdministrationOrchestratorTests',
            'Hexalith.Agents.Server.Tests.AgentProviderSelectionOrchestratorTests',
            'Hexalith.Agents.Server.Tests.EventStoreAgentCommandDispatcherTests',
            'Hexalith.Agents.Server.Tests.AgentInteractionRequestOrchestratorTests',
            'Hexalith.Agents.Server.Tests.ServerSerializationConformanceTests'
        )
        Gate = 'AC1/AC3 live dispatch and safe command-result mapping'
    },
    @{
        Assembly = 'test/Hexalith.Agents.Server.Tests/bin/Debug/net10.0/Hexalith.Agents.Server.Tests.dll'
        Classes  = @('Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests')
        Gate = 'AC1/AC2/AC5 gateway admission, exact replay, and reserved-extension trust'
    },
    @{
        Assembly = 'test/Hexalith.Agents.Server.Tests/bin/Debug/net10.0/Hexalith.Agents.Server.Tests.dll'
        Classes  = @(
            'Hexalith.Agents.Server.Tests.AgentActivationApproverRevalidationTests',
            'Hexalith.Agents.Server.Tests.EventStoreAgentAdministrationOperationsTests'
        )
        Gate = 'AC4/AC6/AC7 activation version fence and fail-closed retry lanes'
    },
    @{
        Assembly = 'test/Hexalith.Agents.Server.Tests/bin/Debug/net10.0/Hexalith.Agents.Server.Tests.dll'
        Classes  = @(
            'Hexalith.Agents.Server.Tests.AgentSetupProjectionTests',
            'Hexalith.Agents.Server.Tests.AgentSetupQueryHandlerTests'
        )
        Gate = 'AC2/AC3 projected setup truth and persisted read-model end state'
    },
    @{
        Assembly = 'test/Hexalith.Agents.UI.Tests/bin/Debug/net10.0/Hexalith.Agents.UI.Tests.dll'
        Classes  = @(
            'Hexalith.Agents.UI.Tests.AgentConfigurationTests',
            'Hexalith.Agents.UI.Tests.AgentsClientSetupGatewayTests'
        )
        Gate = 'AC3/AC4 FrontComposer truth flow and retained activation attempt'
    }
)

# Gate 4a: the composition suites that resolve the real containers. A deferred-only host must fail these.
$compositionSuites = @(
    @{
        Assembly = 'test/Hexalith.Agents.Server.Tests/bin/Debug/net10.0/Hexalith.Agents.Server.Tests.dll'
        Classes  = @(
            'Hexalith.Agents.Server.Tests.AgentSetupCompositionTests',
            'Hexalith.Agents.Server.Tests.HttpAgentAdministrationContextProviderTests',
            'Hexalith.Agents.Server.Tests.AgentsOperationEndpointsTests'
        )
        Gate = 'AC1/AC5 the server container resolves live dispatch and trusted gateway composition'
    },
    @{
        Assembly = 'test/Hexalith.Agents.UI.Tests/bin/Debug/net10.0/Hexalith.Agents.UI.Tests.dll'
        Classes  = @('Hexalith.Agents.UI.Tests.AgentsUiCompositionTests')
        Gate     = 'AC3/AC4 the FrontComposer container resolves the live setup gateway only when an Agent target is named'
    }
)

# Gate 4b anchors: a tripwire for a live seam being deleted outright. Secondary to the composition suites above.
$liveBindings = @(
    @{
        Path   = 'src/Hexalith.Agents.EventStore/AgentsEventStoreServiceCollectionExtensions.cs'
        Needle = 'AddIdempotencyIntentAdapter'
        Gate   = 'the platform gateway can explicitly register all Agents admission adapters'
    },
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

function Invoke-TestGate {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Assembly,
        [string[]] $Classes = @(),
        [ValidateRange(1, [int]::MaxValue)] [int] $MinimumExpectedTests = 1
    )

    $assemblyName = [IO.Path]::GetFileNameWithoutExtension($Assembly)
    $path = if ([IO.Path]::IsPathRooted($Assembly)) { $Assembly } else { Join-Path $root $Assembly }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Gate '$Name' requires built test assembly '$path'. Run without -SkipBuild or build it first."
    }

    $selections = @(
        foreach ($class in $Classes) { @{ Option = '-class'; Value = $class } }
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
        $rowPrefix = '^\s*' + [regex]::Escape($assemblyName) + '\s+Total:'
        $rows = @()
        for ($index = $headers[0] + 1; $index -lt @($output).Count; $index++) {
            if ([string] $output[$index] -cmatch $rowPrefix) {
                $rows += [string] $output[$index]
            }
        }
        if ($rows.Count -eq 0) {
            throw "Gate '$Name' produced no test summary for assembly '$assemblyName' and '$($selection.Value)'."
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
        if ([int] $summary.Groups['Total'].Value -lt $MinimumExpectedTests) {
            throw "Gate '$Name' executed $($summary.Groups['Total'].Value) tests; expected at least $MinimumExpectedTests for '$($selection.Value)'."
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
    try {
        Assert-EventStorePackageFloor
    }
    catch {
        Write-Host $_.Exception.Message
        throw
    }
    if ($PackageFloorOnly) {
        return
    }

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
        Invoke-TestGate `
            -Name "story-5.2 focused — $($suite.Gate)" `
            -Assembly $suite.Assembly `
            -Classes $suite.Classes
    }

    foreach ($testProject in $testProjects) {
        $assembly = [IO.Path]::GetFileNameWithoutExtension($testProject.Path)
        $path = [IO.Path]::Combine([IO.Path]::GetDirectoryName($testProject.Path), 'bin', 'Debug', 'net10.0', "$assembly.dll")
        Invoke-TestGate -Name "regression — $($testProject.Path)" -Assembly $path -MinimumExpectedTests $testProject.MinimumExpectedTests
    }

    foreach ($suite in $compositionSuites) {
        Invoke-TestGate `
            -Name "story-5.2 composition — $($suite.Gate)" `
            -Assembly $suite.Assembly `
            -Classes $suite.Classes
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
