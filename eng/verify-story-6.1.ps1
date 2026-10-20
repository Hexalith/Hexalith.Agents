#!/usr/bin/env pwsh
<#
.SYNOPSIS
Verifies Story 6.1's Level-2 source/package evidence; live runtime and recovery qualification stay closed.
#>
param(
    [ValidateSet('Local', 'Live')][string] $Mode = 'Local',
    [ValidateSet('Source', 'Package', 'Both')][string] $DependencyMode = 'Both',
    [string] $ArtifactsPath = '/tmp/agents-story-6-1'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = Split-Path -Parent $PSScriptRoot
$register = Get-Content (Join-Path $root '_bmad-output/planning-artifacts/external-dependency-register.md') -Raw
$records = @('EXT-HOST-1', 'EXT-PROTECTION-1', 'EXT-TOPOLOGY-1', 'EXT-SECRETS-1', 'EXT-PARTIES-1',
    'EXT-CONV-AI-1', 'EXT-PROVIDER-1', 'EXT-SAFETY-1', 'EXT-TOKEN-1', 'OD-DAPR-SECURITY-1',
    'OD-INITIAL-OUTPUT-SAFETY-STATUS-1', 'OD-RATE-CONCURRENCY-CONSUMPTION-1')
$blocked = @()
foreach ($record in $records) {
    $section = [regex]::Match($register, "(?ms)^### $([regex]::Escape($record))\s.*?(?=^### |\z)").Value
    if (-not $section -or $section -notmatch '\|\s*`AcceptedStatus`\s*\|\s*`Available`\s*\|') {
        $blocked += @{ Code = 'DependencyNotAvailable'; RecordId = $record }
    }
}
New-Item -ItemType Directory -Force -Path $ArtifactsPath | Out-Null
if ($Mode -eq 'Live') {
    @{ Mode = $Mode; EvidenceLevel4 = 'Blocked'; EvidenceLevel5 = 'Blocked'; Blockers = $blocked; ExternalCalls = 0 } |
        ConvertTo-Json -Depth 8 | Set-Content (Join-Path $ArtifactsPath 'live-result.json')
    if ($blocked.Count -gt 0) { throw ('DependencyNotAvailable: ' + (($blocked | ForEach-Object { $_.RecordId }) -join ', ')) }
    throw 'LiveHostQualificationRequired: exact private adapters, matched Dapr security evidence and production-like frozen-cohort/RTO evidence must be qualified by their owners.'
}
$projects = @('Hexalith.Agents.Contracts.Tests', 'Hexalith.Agents.Client.Tests', 'Hexalith.Agents.Tests', 'Hexalith.Agents.Server.Tests', 'Hexalith.Agents.UI.Tests')
$minimums = @{ 'Hexalith.Agents.Contracts.Tests' = 529; 'Hexalith.Agents.Client.Tests' = 6; 'Hexalith.Agents.Tests' = 787; 'Hexalith.Agents.Server.Tests' = 573; 'Hexalith.Agents.UI.Tests' = 1073 }
$domainClasses = @('ConversationInteractionDirectoryAggregateTests', 'ConversationEffectLeaseCommitRaceTests', 'InteractionPermitCreationOutboxTests',
    'ConversationDeletionBarrierRejectsBeforePermitInteractionWorkflowAndProviderTests', 'StaleReservedWorkerCannotActAfterClosingTests', 'CrossTenantProtectionKeyAliasSubstitutionTests', 'AutomaticInteractionTwinPolicyTests', 'ConversationDirectoryTwinPolicyTests', 'PostingSuppressionDecisionTests')
$serverClasses = @('AutomaticInteractionWorkflowTests', 'AutomaticInteractionRestartIntegrationTests', 'WorkflowStartOutboxTests',
    'Fr8OrderedAcceptanceApproverResolutionAndMembershipLeaseTests', 'Fr8RateApproverContextBudgetAndSafetyFailuresCreateNoMembershipEffectTests',
    'ProviderOutputSafetyDecisionPhasePinTests', 'ProviderCannotStartWithoutPhasePinnedOutputSafetyDecisionTests', 'OutputSafetySuccessorCannotRelabelAuthorizedAttemptTests',
    'PreProviderCapacityDeletionDispositionTests', 'GeneratedProposalMutationLeaseTests', 'GeneratedOutputDiscardedWhenProposalMutationCommitLosesTests',
    'GenerationFailureProviderLeaseTests', 'AutomaticInteractionIsolationTests', 'PermitAndCreationOutboxLostAckTests',
    'ApproverResolutionImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests', 'MembershipCommitImmediatelyBeforeAndAfterClosingEffectiveTests',
    'ProjectionLagCannotErasePermitTests', 'AlternateDurableOwnerGuardTests', 'DuplicateCommandTimerAndVersionRecoveryTests',
    'OpenOrMismatchedDaprSecurityDecisionBlocksWorkloadTests', 'WorkflowExecutionStateBoundaryTests',
    'WorkflowStartImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests', 'PreProviderCapacityCannotRemainQueuedOrAdmittedAfterClosingTests',
    'GenerationFailureCannotInventProposalVersionOrRetainOutputTests', 'WorkflowActivityFailureSafetyTests', 'InitialOutputSafetyUsesPhasePinnedStatusTests', 'AutomaticInteractionCheckpointAgreementTests', 'AutomaticPostingClosingRecoveryTests')
$lanes = switch ($DependencyMode) { 'Source' { @('Source') }; 'Package' { @('Package') }; default { @('Source', 'Package') } }
$runs = @()
foreach ($lane in $lanes) {
    $configuration = if ($lane -eq 'Source') { 'Debug' } else { 'Release' }
    $source = if ($lane -eq 'Source') { 'true' } else { 'false' }
    $lanePath = Join-Path $ArtifactsPath $lane.ToLowerInvariant()
    New-Item -ItemType Directory -Force -Path $lanePath | Out-Null
    foreach ($name in $projects) {
        $project = Join-Path $root "test/$name/$name.csproj"
        $buildLog = Join-Path $lanePath "$name.build.log"
        & dotnet build $project --configuration $configuration -m:1 "-p:UseHexalithProjectReferences=$source" -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 --nologo 2>&1 |
            Tee-Object -FilePath $buildLog
        if ($LASTEXITCODE -ne 0) { throw "BuildFailed: $lane/$name ($buildLog)" }
        if ((Get-Content $buildLog -Raw) -notmatch '(?m)^\s*0 Warning\(s\)\s*$') { throw "WarningGateFailed: $lane/$name" }
        $assembly = Join-Path $root "test/$name/bin/$configuration/net10.0/$name.dll"
        $xml = Join-Path $lanePath "$name.xml"
        & dotnet $assembly -result-xml $xml 2>&1 | Tee-Object -FilePath (Join-Path $lanePath "$name.log")
        if ($LASTEXITCODE -ne 0) { throw "TestsFailed: $lane/$name" }
        [xml] $results = Get-Content $xml -Raw
        $assemblies = @($results.SelectNodes('/assemblies/assembly'))
        if ($assemblies.Count -ne 1) { throw "MissingAssemblyResults: $lane/$name" }
        $run = $assemblies[0]
        $total = [int] $run.GetAttribute('total'); $passed = [int] $run.GetAttribute('passed')
        $minimum = $minimums[$name]
        if ($lane -eq 'Package' -and $name -eq 'Hexalith.Agents.Server.Tests') { $minimum = 549 }
        if ($total -lt $minimum -or $passed -ne $total -or [int] $run.GetAttribute('failed') -ne 0 -or [int] $run.GetAttribute('errors') -ne 0 -or
            [int] $run.GetAttribute('skipped') -ne 0 -or [int] $run.GetAttribute('not-run') -ne 0) { throw "IncompleteTestRun: $lane/$name" }
        $required = if ($name -eq 'Hexalith.Agents.Tests') { $domainClasses } elseif ($name -eq 'Hexalith.Agents.Server.Tests') { $serverClasses } else { @() }
        foreach ($class in $required) {
            if (@($results.SelectNodes("//test[@type='$name.$class' and @result='Pass']")).Count -eq 0) { throw "MissingExecutedStorySuite: $lane/$name.$class" }
        }
        $runs += @{ Lane = $lane; Project = $name; Total = $total; Passed = $passed; Skipped = 0; Results = $xml }
    }
}
@{ Mode = $Mode; GoalALocalVerified = $true; EvidenceLevel2 = 'Verified'; EvidenceLevel4 = 'Blocked'; EvidenceLevel5 = 'Blocked';
    GoalB = 'Deferred'; UserProposalActionIntentOutbox = 'DeferredToEpic7'; LiveReadinessEstablished = $false; ExternalOwnerCalls = 0;
    Blockers = $blocked; Runs = $runs; RecoveryCohortManifest = @{ Source = 'AutomaticInteractionRestartIntegrationTests'; ClockOrRtoClaim = $false;
        Model = 'Public Handle/Apply with faults before and after every command checkpoint'; BusinessIdentities = @('Permit', 'Interaction', 'Workflow', 'Attempt', 'Version', 'Message') } } |
    ConvertTo-Json -Depth 10 | Set-Content (Join-Path $ArtifactsPath 'result.json')
Write-Output 'Level 2 verified. Live Dapr (Level 4) and production-like frozen-cohort/RTO (Level 5) remain Blocked; fixtures establish no external availability.'
