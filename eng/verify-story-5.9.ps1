#!/usr/bin/env pwsh
<#
.SYNOPSIS
Verifies the approved Story 5.9 preparation; full qualification remains blocked by Stories 5.5 and 5.7.
.DESCRIPTION
Copies source into a private temporary workspace, builds Debug with source references, runs focused xUnit v3
classes and every owning project, and saves logs/results outside the disposable workspace. By default the
command reports the unmet full-story gates after checking preparation. -PreparationOnly selects the bounded
approved scope; its success establishes no live readiness, Provider exclusion, or dependency availability.
#>
param(
    [switch] $PreparationOnly,
    [string] $ArtifactsPath,
    [switch] $InDisposableWorkspace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = Split-Path -Parent $PSScriptRoot
if (-not $ArtifactsPath) {
    $ArtifactsPath = Join-Path $root 'artifacts/story-5-9'
}
$ArtifactsPath = [IO.Path]::GetFullPath($ArtifactsPath)

if (-not $InDisposableWorkspace) {
    $arguments = @('-NoProfile', '-File', $PSCommandPath, '-InDisposableWorkspace', '-ArtifactsPath', $ArtifactsPath)
    if ($PreparationOnly) { $arguments += '-PreparationOnly' }
    $workspaceTemp = & mktemp -d
    try {
        & env "TMPDIR=$workspaceTemp" "TMP=$workspaceTemp" "TEMP=$workspaceTemp" pwsh @arguments
        $result = $LASTEXITCODE
    }
    finally {
        Remove-Item -LiteralPath $workspaceTemp -Recurse -Force
    }
    exit $result
}
if (-not $env:TMPDIR -or -not (Test-Path -LiteralPath $env:TMPDIR -PathType Container)) {
    throw 'A private TMPDIR is required inside the verifier workspace.'
}
# Resolve every existing symlink component, including an ancestor of a not-yet-created output path.
$canonicalTemp = [IO.Path]::TrimEndingDirectorySeparator([string] (& realpath -m -- $env:TMPDIR))
$sourceRoot = [IO.Path]::TrimEndingDirectorySeparator([string] (& realpath -e -- $root))
if ($canonicalTemp -ceq $sourceRoot -or $canonicalTemp.StartsWith("$sourceRoot/", [StringComparison]::Ordinal)) {
    throw 'TMPDIR must be outside the source checkout.'
}
$ArtifactsPath = [IO.Path]::TrimEndingDirectorySeparator([string] (& realpath -m -- $ArtifactsPath))
if ($ArtifactsPath -ceq $canonicalTemp -or
    $ArtifactsPath.StartsWith($canonicalTemp.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
    throw 'Useful logs and results must be saved outside TMPDIR.'
}

$projects = @(
    @{ Name = 'Hexalith.Agents.Contracts.Tests'; Classes = @('AgentContractsRoundTripTests', 'AgentOperationContractsTests') },
    @{ Name = 'Hexalith.Agents.Tests'; Classes = @('AgentLaunchReadinessTests') },
    @{ Name = 'Hexalith.Agents.Server.Tests'; Classes = @('AgentSetupProjectionTests', 'AgentsOperationEndpointsTests', 'EventStoreAgentAdministrationOperationsTests') },
    @{ Name = 'Hexalith.Agents.UI.Tests'; Classes = @('LaunchReadinessPresentationTests', 'LaunchReadinessSurfaceTests', 'LocalizationResourceTests') }
)
$runs = [Collections.Generic.List[object]]::new()
$fullStoryGates = @(
    'Story 5.5 authoritative live readiness and Provider-state contracts',
    'Story 5.7 live activation and Provider exclusion proof',
    'Live public readiness path and full Story 5.9 conformance'
)
function Invoke-Tests {
    param([string] $Name, [string] $Class)
    $selection = if ($Class) { $Class } else { 'regression' }
    $assembly = Join-Path $workspace "test/$Name/bin/Debug/net11.0/$Name.dll"
    $xml = Join-Path $ArtifactsPath "$Name.$selection.xml"
    $log = Join-Path $ArtifactsPath "$Name.$selection.log"
    $arguments = @($assembly, '-failSkips', '-result-xml', $xml)
    if ($Class) { $arguments += @('-class', "*.$Class") }
    Write-Host "Preparation gate: $Name / $selection"
    # Mapping belongs to build processes. Test processes may launch independent MSBuild conformance probes,
    # which must see the original environment rather than inherit this verifier's forced source selection.
    $mappedEnvironment = @{}
    $nativeErrors = $PSNativeCommandUseErrorActionPreference
    try {
        foreach ($key in $previousEnvironment.Keys) {
            $mappedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
            if ($null -eq $previousEnvironment[$key]) { Remove-Item -LiteralPath "Env:$key" -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable($key, $previousEnvironment[$key], 'Process') }
        }
        $PSNativeCommandUseErrorActionPreference = $false
        & dotnet @arguments 2>&1 | Tee-Object -FilePath $log | ForEach-Object {
            if ([string] $_ -match 'xUnit|Discover|Starting:|Finished:|TEST EXECUTION SUMMARY|Total:|\[FAIL\]') {
                Write-Host $_
            }
        }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $PSNativeCommandUseErrorActionPreference = $nativeErrors
        foreach ($key in $mappedEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($key, $mappedEnvironment[$key], 'Process')
        }
    }
    if ($exitCode -ne 0) { throw "TestsFailed: $Name/$selection ($log)" }
    [xml] $results = Get-Content -LiteralPath $xml -Raw
    $assemblies = @($results.SelectNodes('/assemblies/assembly'))
    if ($assemblies.Count -ne 1) { throw "MissingAssemblyResults: $Name/$selection" }
    $result = $assemblies[0]
    $total = [int] $result.GetAttribute('total')
    if ($total -le 0 -or [int] $result.GetAttribute('passed') -ne $total -or
        [int] $result.GetAttribute('failed') -ne 0 -or [int] $result.GetAttribute('errors') -ne 0 -or
        [int] $result.GetAttribute('skipped') -ne 0 -or [int] $result.GetAttribute('not-run') -ne 0) {
        throw "IncompleteTestRun: $Name/$selection"
    }
    $runs.Add(@{ Project = $Name; Selection = $selection; Total = $total; Passed = $total; Results = $xml })
}

$preparationVerified = $false
$failure = 'PreparationIncomplete'
$locationPushed = $false
function Write-Receipt {
    @{
        Scope = 'Approved preparation only'; PreparationVerified = $preparationVerified
        FullStoryComplete = $false; SprintStatus = 'backlog'; LiveReadinessEstablished = $false
        ProviderExclusionProven = $false; DependencyAvailabilityChanged = $false
        UnmetFullStoryGates = $fullStoryGates; Failure = $failure; Runs = @($runs.ToArray())
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'result.json')
}
New-Item -ItemType Directory -Force -Path $ArtifactsPath | Out-Null
Write-Receipt
try {
    $gitRoot = Get-Item -LiteralPath (Join-Path $root '.git') -Force
    if (-not $gitRoot.PSIsContainer -or $gitRoot.LinkType) {
        throw 'UnsupportedGitRoot: .git must be an ordinary directory; linked-worktree pointer roots are unsupported.'
    }
    $workspace = Join-Path $env:TMPDIR 'source'
    New-Item -ItemType Directory -Force -Path $workspace | Out-Null
    & rsync -a --exclude=.git --exclude=bin --exclude=obj --exclude=node_modules --exclude=artifacts `
        --exclude=.artifacts --exclude=.bmad-loop --exclude=_bmad-output --exclude=evidence --exclude=nupkgs `
        --exclude=.agents --exclude=.claude "$root/" "$workspace/"
    if ($LASTEXITCODE -ne 0) { throw 'Disposable source copy failed.' }
    # Mapping must inspect an isolated directory-backed Git copy; linked-worktree pointers can escape it.
    & cp -a (Join-Path $root '.git') (Join-Path $workspace '.git')
    if ($LASTEXITCODE -ne 0) { throw 'Disposable Git identity copy failed.' }
    Get-ChildItem (Join-Path $root 'references') -Directory | ForEach-Object {
        $marker = Join-Path $_.FullName '.git'
        if (Test-Path -LiteralPath $marker -PathType Leaf) {
            Copy-Item -LiteralPath $marker -Destination (Join-Path $workspace "references/$($_.Name)/.git")
        }
    }
    $report = '_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent (Join-Path $workspace $report)) | Out-Null
    Copy-Item -LiteralPath (Join-Path $root $report) -Destination (Join-Path $workspace $report)
    Push-Location $workspace
    $locationPushed = $true
    # Enrolled dependencies require imports selected and validated by the Builds producer.
    $producer = 'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj'
    $preparedDirectory = Join-Path $env:TMPDIR 'build-preparation'
    $producerOutput = & dotnet run --project $producer -c Debug -p:MinVerVersionOverride=1.0.0 `
        --no-launch-profile --verbosity quiet -- prepare-build --manifest module/hexalith.module-manifest.v2.json `
        --mode source --workspace $preparedDirectory --output json `
        2> (Join-Path $ArtifactsPath 'build-preparation.stderr.log')
    if ($LASTEXITCODE -ne 0) { throw 'BuildPreparationFailed: see build-preparation.stderr.log' }
    $producerOutput | Set-Content -LiteralPath (Join-Path $ArtifactsPath 'build-preparation.json')
    $prepared = ($producerOutput -join "`n") | ConvertFrom-Json -AsHashtable
    if ($prepared.status -cne 'prepared' -or $prepared.outcome.exitCode -cne 'Success' -or
        $prepared.buildPreparation.mode -cne 'source' -or $prepared.buildPreparation.mappingHash -cnotmatch '^[0-9a-f]{64}$') {
        throw 'InvalidBuildPreparation'
    }
    $previousEnvironment = @{}
    foreach ($key in $prepared.buildPreparation.environment.Keys) {
        $previousEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $prepared.buildPreparation.environment[$key], 'Process')
    }
    # Keep the broad build gate separate from focused owning-project evidence.
    $broadLog = Join-Path $ArtifactsPath 'solution.build.log'
    $nativeErrors = $PSNativeCommandUseErrorActionPreference
    try {
        $PSNativeCommandUseErrorActionPreference = $false
        & dotnet build Hexalith.Agents.slnx -c Debug -m:1 -warnaserror -p:UseHexalithProjectReferences=true `
            -p:MinVerVersionOverride=1.0.0 /nr:false --nologo 2>&1 | Tee-Object -FilePath $broadLog
        $broadExitCode = $LASTEXITCODE
    }
    finally { $PSNativeCommandUseErrorActionPreference = $nativeErrors }

    foreach ($project in $projects) {
        $name = $project.Name
        $buildLog = Join-Path $ArtifactsPath "$name.build.log"
        & dotnet build "test/$name/$name.csproj" -c Debug -m:1 -warnaserror -p:UseHexalithProjectReferences=true `
            -p:MinVerVersionOverride=1.0.0 /nr:false --nologo 2>&1 | Tee-Object -FilePath $buildLog
        if ($LASTEXITCODE -ne 0) { throw "BuildFailed: $name ($buildLog)" }
        foreach ($class in $project.Classes) { Invoke-Tests -Name $name -Class $class }
        Invoke-Tests -Name $name
    }
    if ($broadExitCode -ne 0) { throw "BroadSolutionBuildFailed: see $broadLog; focused evidence is retained separately." }
    $preparationVerified = $true
    $failure = $null
}
catch { $failure = $_.Exception.Message }
finally {
    if (Test-Path variable:previousEnvironment) {
        foreach ($key in $previousEnvironment.Keys) {
            if ($null -eq $previousEnvironment[$key]) { Remove-Item -LiteralPath "Env:$key" -ErrorAction SilentlyContinue }
            else { [Environment]::SetEnvironmentVariable($key, $previousEnvironment[$key], 'Process') }
        }
    }
    if ($locationPushed) { Pop-Location }
    Write-Receipt
}
if ($failure) { throw $failure }
Write-Host 'Approved preparation verified. Full Story 5.9 remains incomplete and its sprint entry stays in backlog.'
foreach ($gate in $fullStoryGates) { Write-Host "Unmet full-story gate: $gate" }
if (-not $PreparationOnly) {
    throw 'FullStoryQualificationBlocked: Stories 5.5/5.7 and live public readiness/Provider exclusion evidence are still required.'
}
