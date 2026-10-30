#!/usr/bin/env pwsh
<#
.SYNOPSIS
Verifies local Story 5.4 contracts and fails live qualification closed on unavailable exact targets.
#>
param(
    [ValidateSet('Local', 'Live')][string] $Mode = 'Local',
    [string] $ArtifactsPath = '/tmp/agents-story-5-4'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = Split-Path -Parent $PSScriptRoot
$register = Get-Content (Join-Path $root '_bmad-output/planning-artifacts/external-dependency-register.md') -Raw
$records = @('EXT-PARTIES-1', 'EXT-CONV-AI-1', 'EXT-SECRETS-1', 'EXT-HOST-1')
$blocked = @()
foreach ($record in $records) {
    $section = [regex]::Match($register, "(?ms)^### $([regex]::Escape($record))\s.*?(?=^### |\z)").Value
    if (-not $section -or $section -notmatch '\|\s*`AcceptedStatus`\s*\|\s*`Available`\s*\|') {
        $blocked += @{ Code = 'DependencyNotAvailable'; RecordId = $record }
    }
}
New-Item -ItemType Directory -Force -Path $ArtifactsPath | Out-Null
if ($Mode -eq 'Live') {
    # A consuming verifier cannot qualify a Committed record. Only its pinned owner command may do that.
    if ($blocked.Count -gt 0) {
        @{ Mode = $Mode; StoryComplete = $false; Blockers = $blocked; ExternalCalls = 0 } |
            ConvertTo-Json -Depth 8 | Set-Content (Join-Path $ArtifactsPath 'result.json')
        throw ('DependencyNotAvailable: ' + (($blocked | ForEach-Object { $_.RecordId }) -join ', '))
    }
    # Available does not invent the installed private adapters, credentials, or persisted qualification scenario.
    throw 'LiveHostQualificationRequired: bind exact deployed source/ingress/replay/spool/worker targets and persisted ACL/recovery evidence.'
}
$projects = @(
    'Hexalith.Agents.Contracts.Tests', 'Hexalith.Agents.Client.Tests', 'Hexalith.Agents.Tests',
    'Hexalith.Agents.Server.Tests', 'Hexalith.Agents.UI.Tests'
)
# The producer owns source selection and validation across every root-declared reference. Prepare its
# imports without starting resources; hand-selected roots cannot enroll newer Conversations builds.
$producer = Join-Path $root 'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj'
$manifest = Join-Path $root 'module/hexalith.module-manifest.v2.json'
$preparationDirectory = Join-Path ([IO.Path]::GetFullPath($ArtifactsPath)) ('build-preparation-' + [Guid]::NewGuid().ToString('N'))
$preparationLog = Join-Path $ArtifactsPath 'build-preparation.json'
$preparationErrorLog = Join-Path $ArtifactsPath 'build-preparation.stderr.log'
if (-not (Test-Path -LiteralPath $producer -PathType Leaf)) { throw 'BuildPreparationProducerMissing' }
$nativeErrorPreference = $PSNativeCommandUseErrorActionPreference
try {
    $PSNativeCommandUseErrorActionPreference = $false
    $preparationOutput = & dotnet run --project $producer --configuration Debug --no-launch-profile --verbosity quiet -- `
        prepare-build --manifest $manifest --mode source --workspace $preparationDirectory --output json 2> $preparationErrorLog
    $preparationExitCode = $LASTEXITCODE
}
finally {
    $PSNativeCommandUseErrorActionPreference = $nativeErrorPreference
}
$preparationOutput | Tee-Object -FilePath $preparationLog
Get-Content -LiteralPath $preparationErrorLog | ForEach-Object { Write-Host $_ }
if ($preparationExitCode -ne 0) { throw "BuildPreparationFailed: $preparationExitCode" }
try {
    $preparation = ($preparationOutput -join "`n") | ConvertFrom-Json -AsHashtable
    $prepared = $preparation.buildPreparation
    $buildEnvironment = $prepared.environment
    if ($preparation.status -cne 'prepared' -or $preparation.outcome.exitCode -cne 'Success' -or
        $prepared.mode -cne 'source' -or $prepared.workspace -cne $preparationDirectory -or
        $prepared.mappingHash -cnotmatch '^[0-9a-f]{64}$' -or $buildEnvironment -isnot [System.Collections.IDictionary]) {
        throw 'Invalid producer result'
    }
    $imports = @{
        CustomBeforeDirectoryBuildProps = 'SourceMapping.targets'
        CustomAfterMicrosoftCommonProps = 'SourceMapping.props'
        CustomBeforeDirectoryBuildTargets = 'SourceMapping.before-targets.props'
        CustomAfterDirectoryBuildTargets = 'SourceMapping.late.props'
        AfterMicrosoftNETSdkTargets = 'SourceMapping.final.targets'
    }
    if ($buildEnvironment.Count -ne ($imports.Count + 2) -or $buildEnvironment.Configuration -cne 'Debug' -or
        $buildEnvironment.HEXALITH_SOURCE_MAPPING_HASH -cne $prepared.mappingHash) { throw 'Invalid producer environment' }
    foreach ($key in $imports.Keys) {
        $expected = Join-Path $preparationDirectory "source-mapping/$($imports[$key])"
        if ($buildEnvironment[$key] -cne $expected -or -not (Test-Path -LiteralPath $expected -PathType Leaf)) {
            throw "Invalid producer import: $key"
        }
    }
}
catch {
    throw "BuildPreparationInvalid: $($_.Exception.Message)"
}
foreach ($name in $projects) {
    $project = Join-Path $root "test/$name/$name.csproj"
    $buildLog = Join-Path $ArtifactsPath "$name.build.log"
    $previousEnvironment = @{}
    try {
        foreach ($key in $buildEnvironment.Keys) {
            $previousEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
            [Environment]::SetEnvironmentVariable($key, $buildEnvironment[$key], 'Process')
        }
        & dotnet build $project --configuration Debug -m:1 -p:MinVerVersionOverride=1.0.0 --nologo 2>&1 | Tee-Object -FilePath $buildLog
        if ($LASTEXITCODE -ne 0) { throw "BuildFailed: $name" }
    }
    finally {
        foreach ($key in $previousEnvironment.Keys) {
            if ($null -eq $previousEnvironment[$key]) {
                Remove-Item -LiteralPath "Env:$key" -ErrorAction SilentlyContinue
            }
            else {
                [Environment]::SetEnvironmentVariable($key, $previousEnvironment[$key], 'Process')
            }
        }
    }
    if ((Get-Content $buildLog -Raw) -notmatch '(?m)^\s*0 Warning\(s\)\s*$') { throw "WarningGateFailed: $name" }
    $assembly = Join-Path $root "test/$name/bin/Debug/net10.0/$name.dll"
    $xml = Join-Path $ArtifactsPath "$name.xml"
    & dotnet $assembly -result-xml $xml
    if ($LASTEXITCODE -ne 0) { throw "TestsFailed: $name" }
    [xml] $results = Get-Content $xml -Raw
    $assemblies = @($results.SelectNodes('/assemblies/assembly'))
    if ($assemblies.Count -eq 0) { throw "MissingAssemblyResults: $name" }
    foreach ($run in $assemblies) {
        $total = [int] $run.GetAttribute('total')
        $passed = [int] $run.GetAttribute('passed')
        if ($total -le 0 -or $passed -ne $total -or [int] $run.GetAttribute('failed') -ne 0 -or
            [int] $run.GetAttribute('errors') -ne 0 -or [int] $run.GetAttribute('skipped') -ne 0 -or
            [int] $run.GetAttribute('not-run') -ne 0) { throw "IncompleteTestRun: $name" }
    }
    $requiredClasses = switch ($name) {
        'Hexalith.Agents.Tests' { @('TrustedEnvelopeReplayTests', 'SecurityEventLogTests', 'AgentApproverPolicyTests') }
        'Hexalith.Agents.Server.Tests' { @('Story54CurrentEvidenceTests', 'Story54BoundaryTests', 'PartiesAgentPartyDirectoryTests', 'ApproverEvidenceAdmissionTests', 'TenantAuthorityAdmissionOrderingTests', 'AuthorityCancellationAdmissionTests', 'AgentSetupQueryHandlerTests', 'AgentSetupApproverBasisTests', 'SetupReadCancellationAdmissionTests', 'SetupWriteCancellationAdmissionTests', 'SetupAuthorityLifetimeTests', 'ProviderCatalogAuthorityLifetimeTests', 'AuditQueryIsolationTests', 'AgentInteractionAuditQueryHandlerTests', 'AgentInteractionGateAdmissionTests') }
        'Hexalith.Agents.UI.Tests' { @('AgentConfigurationTests', 'ApproverPolicyTests') }
        default { @() }
    }
    foreach ($class in $requiredClasses) {
        $executed = @($results.SelectNodes("//test[@type='$name.$class' and @result='Pass']"))
        if ($executed.Count -eq 0) { throw "MissingExecutedStorySuite: $name.$class" }
    }
}
@{ Mode = $Mode; LocalContractsVerified = $true; StoryComplete = $false; LiveReadinessEstablished = $false;
    Blockers = $blocked; ExternalOwnerCalls = 0 } | ConvertTo-Json -Depth 8 |
    Set-Content (Join-Path $ArtifactsPath 'result.json')
Write-Output 'Local contracts verified. Live story acceptance remains blocked; fixtures establish no external availability.'
