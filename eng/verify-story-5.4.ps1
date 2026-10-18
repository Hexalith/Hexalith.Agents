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
foreach ($name in $projects) {
    $project = Join-Path $root "test/$name/$name.csproj"
    $buildLog = Join-Path $ArtifactsPath "$name.build.log"
    & dotnet build $project --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 --nologo 2>&1 | Tee-Object -FilePath $buildLog
    if ($LASTEXITCODE -ne 0) { throw "BuildFailed: $name" }
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
        'Hexalith.Agents.Server.Tests' { @('Story54CurrentEvidenceTests', 'Story54BoundaryTests', 'ApproverEvidenceAdmissionTests', 'TenantAuthorityAdmissionOrderingTests', 'AuthorityCancellationAdmissionTests', 'AgentSetupQueryHandlerTests', 'SetupReadCancellationAdmissionTests', 'SetupWriteCancellationAdmissionTests', 'AuditQueryIsolationTests', 'AgentInteractionAuditQueryHandlerTests', 'AgentInteractionGateAdmissionTests') }
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
