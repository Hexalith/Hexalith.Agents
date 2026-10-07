[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EvidenceDirectory,
    [Parameter(Mandatory)][string]$DependencyRegisterPath
)
$ErrorActionPreference = 'Stop'
$required = @('EXT_PARTIES_GATEWAY_URL', 'EXT_PARTIES_READER_HEADERS_JSON', 'EXT_PARTIES_TARGET_SHA', 'EXT_PARTIES_COMPATIBILITY_RECEIPT',
    'EXT_PARTIES_TENANT_A', 'EXT_PARTIES_HISTORY_PARTY_ID', 'EXT_PARTIES_HISTORY_ACTOR_ID',
    'EXT_PARTIES_HISTORY_ACTION_AT', 'EXT_PARTIES_HISTORY_BINDING_VERSION', 'EXT_PARTIES_POLICY_ID', 'EXT_PARTIES_RETENTION', 'EXT_PARTIES_EXPIRY_TRIGGER')
$missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
if ($missing.Count) {
    throw ('DependencyNotAvailable: EXT-PARTIES-1. Missing read-only live readiness inputs: ' + ($missing -join ', '))
}
if ($env:EXT_PARTIES_TARGET_SHA -cnotmatch '^[0-9a-f]{40}$') { throw 'An exact requested source target SHA is required.' }
# Read probes execute the dependency seam. A local fixture exemption cannot bypass
# the authoritative consumer execution gate or establish owner acceptance.
if (-not (Test-Path -LiteralPath $DependencyRegisterPath -PathType Leaf)) { throw 'DependencyNotAvailable: EXT-PARTIES-1 authoritative register is missing.' }
$register = Get-Content -LiteralPath $DependencyRegisterPath -Raw
$section = [regex]::Match($register, '(?ms)^### EXT-PARTIES-1\b.*?(?=^### |\z)').Value
function Get-Commitment([string]$Field) {
    $pattern = '(?m)^\|\s*`' + [regex]::Escape($Field) + '`\s*\|\s*(.*?)\s*\|\s*$'
    return [regex]::Match($section, $pattern).Groups[1].Value.Trim().Trim([char]'`')
}
$acceptedTarget = Get-Commitment 'TargetVersionOrCommit'
$acceptedDate = Get-Commitment 'TargetIntegrationDate'
$acceptedContract = Get-Commitment 'CompatibilityContractAndVerificationCommand'
$acceptedCommand = [regex]::Match($acceptedContract, '(?s)Command:\s*`([^`]+)`').Groups[1].Value
$integrationDate = [DateTimeOffset]::MinValue
if ((Get-Commitment 'AcceptedStatus') -cne 'Available' -or $acceptedTarget -cne $env:EXT_PARTIES_TARGET_SHA -or
    -not [DateTimeOffset]::TryParse($acceptedDate, [ref]$integrationDate) -or
    [string]::IsNullOrWhiteSpace($acceptedCommand) -or $acceptedCommand -eq 'TBD') {
    throw 'DependencyNotAvailable: EXT-PARTIES-1 requires Available, the exact accepted target/date/command and passing prerequisite evidence before any live read.'
}
try { $receipt = Get-Content -LiteralPath $env:EXT_PARTIES_COMPATIBILITY_RECEIPT -Raw | ConvertFrom-Json -AsHashtable }
catch { throw 'DependencyNotAvailable: EXT-PARTIES-1 compatibility receipt is missing or malformed.' }
if ($receipt.recordId -cne 'EXT-PARTIES-1' -or $receipt.targetVersionOrCommit -cne $acceptedTarget -or
    $receipt.compatibilityCommand -cne $acceptedCommand -or $receipt.outcome -cne 'Pass' -or
    $receipt.evidenceLevel -lt 4 -or $receipt.prerequisitesAvailable -ne $true) {
    throw 'DependencyNotAvailable: EXT-PARTIES-1 exact-target compatibility/prerequisite proof does not match the accepted record.'
}
$retention = [TimeSpan]::Zero
if (-not [TimeSpan]::TryParse($env:EXT_PARTIES_RETENTION, [ref]$retention) -or $retention -le [TimeSpan]::Zero -or
    $env:EXT_PARTIES_EXPIRY_TRIGGER -cne 'binding-effective-at' -or
    $receipt.policyId -cne $env:EXT_PARTIES_POLICY_ID -or $receipt.retention -cne $env:EXT_PARTIES_RETENTION -or
    $receipt.expiryTrigger -cne $env:EXT_PARTIES_EXPIRY_TRIGGER) {
    throw 'DependencyNotAvailable: EXT-PARTIES-1 requires matching explicit supported production retention policy and custody prerequisites.'
}
$base = [Uri]$env:EXT_PARTIES_GATEWAY_URL
if (-not $base.IsAbsoluteUri -or $base.Scheme -notin @('http', 'https') -or $base.UserInfo -or $base.Query -or $base.Fragment) {
    throw 'The authenticated owner transport must be an absolute HTTP(S) base URL without user information.'
}
try { $headers = ConvertFrom-Json -InputObject $env:EXT_PARTIES_READER_HEADERS_JSON -AsHashtable }
catch { throw 'Owner transport headers are malformed.' }
if ($headers -isnot [System.Collections.IDictionary] -or $headers.Count -eq 0 -or
    @($headers.Keys | Where-Object { $_ -notin @('Authorization', 'dapr-api-token') }).Count) {
    throw 'Supply owner-issued Authorization and/or dapr-api-token headers; caller identity headers cannot be fabricated.'
}
$bindingVersion = 0L
$actionAt = [DateTimeOffset]::MinValue
if (-not [long]::TryParse($env:EXT_PARTIES_HISTORY_BINDING_VERSION, [ref]$bindingVersion) -or $bindingVersion -le 0 -or
    -not [DateTimeOffset]::TryParse($env:EXT_PARTIES_HISTORY_ACTION_AT, [ref]$actionAt) -or
    $env:EXT_PARTIES_HISTORY_ACTOR_ID -cnotmatch '^[0-7][0-9ABCDEFGHJKMNPQRSTVWXYZ]{25}$') {
    throw 'The owner fixture must supply an exact recorded actor, binding version and action instant.'
}

function Invoke-OwnerRead([string]$RelativePath, [hashtable]$Payload) {
    $target = [Uri]::new($base.AbsoluteUri.TrimEnd('/') + '/' + $RelativePath)
    $body = ConvertTo-Json -InputObject $Payload -Depth 12 -Compress
    $response = Invoke-WebRequest -Uri $target -Method Post -Headers $headers -ContentType 'application/json' -Body $body -TimeoutSec 30 -SkipHttpErrorCheck
    if ([int]$response.StatusCode -lt 200 -or [int]$response.StatusCode -ge 300) {
        throw ('Owner read denied or unavailable at the authenticated transport (HTTP ' + [int]$response.StatusCode + ').')
    }
    if ([Text.Encoding]::UTF8.GetByteCount($response.Content) -gt 32MB) { throw 'Owner response exceeds the source bound.' }
    try { return ConvertFrom-Json -InputObject $response.Content -AsHashtable }
    catch { throw 'Owner response is malformed.' }
}

$identity = @{ tenantId=$env:EXT_PARTIES_TENANT_A; domain='party'; aggregateId=$env:EXT_PARTIES_HISTORY_PARTY_ID }
$retained = Invoke-OwnerRead 'api/v1/identity-history/read' @{ identity=$identity; purpose='party-actor-history-v1' }
$source = $retained.stream
if ($null -eq $source -or $retained.failureReason -or $source.purpose -ne 'party-actor-history-v1' -or
    $source.identity.tenantId -cne $identity.tenantId -or $source.identity.domain -cne 'party' -or
    $source.identity.aggregateId -cne $identity.aggregateId -or $source.head -le 0 -or $source.head -gt 10000 -or
    [string]::IsNullOrWhiteSpace($source.observationId) -or [string]::IsNullOrWhiteSpace($source.authorityRevision) -or
    -not $source.observedAt -or [DateTimeOffset]$source.observedAt -gt [DateTimeOffset]::UtcNow -or
    -not $source.validUntil -or [DateTimeOffset]$source.validUntil -le [DateTimeOffset]::UtcNow) {
    throw 'Independent retained history has no exact authoritative source certificate.'
}
$covered = [Collections.Generic.HashSet[long]]::new()
$payloadBytes = 0L
$previous = 0L
foreach ($item in $source.events) {
    $position = [long]$item.sequenceNumber
    if ($position -le $previous -or $position -gt $source.head -or -not $covered.Add($position) -or
        $null -eq $item.protectionMetadata -or $item.protectionMetadata.state -notin @(0, 'Unprotected') -or
        $item.eventTypeName -cnotmatch '^Hexalith\.Parties\.Contracts\.Events\.HumanActorBinding(Established|Rebound|Revoked)$' -or
        $item.serializationFormat -cne 'json' -or $null -eq $item.payload -or
        $item.messageId -cne '' -or $null -ne $item.userId -or $null -ne $item.correlationId -or $null -ne $item.causationId) {
        throw 'The retained source contains an invalid original position, profile substitution or unreadable event.'
    }
    try { $payloadBytes += [Convert]::FromBase64String($item.payload).LongLength }
    catch { throw 'The retained source payload is not readable base64 JSON.' }
    if ($payloadBytes -gt 16MB) { throw 'The retained source exceeds the decoded payload bound.' }
    $previous = $position
}
$previous = 0L
foreach ($excluded in $source.excludedSequences) {
    $position = [long]$excluded
    if ($position -le $previous -or $position -gt $source.head -or -not $covered.Add($position)) { throw 'The source exclusion certificate overlaps or is unordered.' }
    $previous = $position
}
if ($covered.Count -ne $source.head) { throw 'The retained source certificate has a gap.' }

$queryPayload = @{ tenantId=$identity.tenantId; partyId=$identity.aggregateId; actionAt=$actionAt.ToString('O');
    expectedActorId=$env:EXT_PARTIES_HISTORY_ACTOR_ID; expectedBindingVersion=$bindingVersion }
$response = Invoke-OwnerRead 'api/v1/queries' @{ tenant=$identity.tenantId; domain='party'; aggregateId=$identity.aggregateId;
    queryType='Hexalith.Parties.Contracts.Queries.ResolveHumanActorBindingAt'; projectionType='party'; entityId=$identity.aggregateId; payload=$queryPayload }
$history = $response.payload
if ($response.success -ne $true -or $response.metadata.isDegraded -eq $true -or $response.metadata.isStale -eq $true -or
    $null -eq $history -or $history.outcome -notin @(1, 'Resolved') -or $history.contractVersion -ne 1 -or
    $history.tenantId -cne $identity.tenantId -or $history.partyId -cne $identity.aggregateId -or
    [DateTimeOffset]$history.actionAt -ne $actionAt -or $history.evidence.actorId -cne $env:EXT_PARTIES_HISTORY_ACTOR_ID -or
    $history.evidence.bindingVersion -ne $bindingVersion -or $history.bindingSourcePosition -le 0 -or
    $history.bindingSourcePosition -gt $history.sourcePosition -or $history.sourcePosition -lt $source.head -or
    [DateTimeOffset]$history.evidence.validFrom -gt $actionAt -or $null -eq $history.evidence.validUntil -or
    [DateTimeOffset]$history.evidence.validUntil -le $actionAt -or $history.evidence.custody.purpose -ne 'party-actor-history-v1' -or
    $history.evidence.custody.policyId -cne $env:EXT_PARTIES_POLICY_ID -or
    $history.evidence.custody.sourceExpiryEnforced -ne $true -or $history.evidence.custody.restoreSafe -ne $true -or
    $history.evidence.custody.derivedCopiesCovered -ne $true -or $history.evidence.custody.lifecycleRevision -le 0 -or
    [DateTimeOffset]$history.evidence.custody.expiresAt -le [DateTimeOffset]::UtcNow) {
    throw 'The gateway did not reproduce the exact retained action-time binding and original source position.'
}
$openingEvent = @($source.events | Where-Object { $_.sequenceNumber -eq $history.bindingSourcePosition })
if ($openingEvent.Count -ne 1) { throw 'The returned opening position is absent from the independently read retained source.' }
$decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($openingEvent[0].payload)) | ConvertFrom-Json -AsHashtable
if ($decoded.binding.evidence.actorId -cne $history.evidence.actorId -or $decoded.binding.evidence.bindingVersion -ne $bindingVersion -or
    $decoded.binding.evidence.tenantId -cne $identity.tenantId -or $decoded.binding.evidence.partyId -cne $identity.aggregateId) {
    throw 'The returned binding does not match its original retained source payload.'
}

$current = Invoke-OwnerRead 'api/v1/queries' @{ tenant=$identity.tenantId; domain='party'; aggregateId=$identity.aggregateId;
    queryType='Hexalith.Parties.Contracts.Queries.ResolvePartyIdentity'; projectionType='party'; entityId=$identity.aggregateId;
    payload=@{ tenantId=$identity.tenantId; partyId=$identity.aggregateId; expectedActorId=$env:EXT_PARTIES_HISTORY_ACTOR_ID } }
if ($current.success -ne $true -or $null -eq $current.payload -or
    $current.metadata.isDegraded -eq $true -or $current.metadata.isStale -eq $true -or
    $current.payload.outcome -notin @(0, 2, 'Unavailable', 'Ineligible') -or $current.payload.evidence.humanBinding) {
    throw 'The owner erased-profile fixture unexpectedly remains currently eligible.'
}
if ([DateTimeOffset]$source.validUntil -le [DateTimeOffset]::UtcNow -or
    [DateTimeOffset]$history.evidence.custody.expiresAt -le [DateTimeOffset]::UtcNow) {
    throw 'The retained source certificate or custody expired during readiness probes.'
}
$evidence = @{ qualification='ReadinessOnly'; requestedTargetSha=$env:EXT_PARTIES_TARGET_SHA; observedAt=[DateTimeOffset]::UtcNow.ToString('O');
    checks=@('complete-original-position-source-partition', 'exact-action-time-actor-and-version', 'original-binding-source-position', 'current-identity-ineligible');
    sourceHead=$history.sourcePosition; bindingSourcePosition=$history.bindingSourcePosition;
    incomplete=@('P-01-P-10-installed-matrix', 'independent-erasure-certification', 'persisted-restart-restore', 'failure-injection', 'production-custody-destruction-and-copy-cleanup') }
New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
$evidence | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $EvidenceDirectory 'live-readiness.json')
Write-Host 'Authenticated retained-history readiness probes passed for the owner-provided fixture. Erasure certification and complete Live qualification remain unavailable.'
