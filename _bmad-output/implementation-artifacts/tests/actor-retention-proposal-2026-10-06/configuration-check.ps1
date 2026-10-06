$ErrorActionPreference = 'Stop'
$proposalPath = '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/proposals/parties-identity-365-days.proposed.json'
$assemblyDirectory = '/tmp/hexalith-agents54-retention-artifacts/bin/Hexalith.Parties.Tests/debug'
[void][Reflection.Assembly]::LoadFrom((Join-Path $assemblyDirectory 'Hexalith.EventStore.Contracts.dll'))
$config = (Get-Content -LiteralPath $proposalPath -Raw | ConvertFrom-Json).Parties.Identity
$duration = [TimeSpan]::ParseExact($config.Retention, 'c', [Globalization.CultureInfo]::InvariantCulture)
$policy = [Hexalith.EventStore.Contracts.Security.IdentityHistoryPolicy]::new($config.PolicyId, $duration, $config.ExpiryTrigger)
if ($null -eq $policy -or -not $policy.IsValid) { throw 'Proposed policy is unsupported.' }
$start = [DateTimeOffset]::Parse('2026-10-06T00:00:00Z', [Globalization.CultureInfo]::InvariantCulture)
$deadline = $policy.DeriveExpiry($start)
if ($duration.TotalDays -ne 365 -or $deadline -ne $start.AddDays(365)) { throw 'Retention/expiry derivation mismatch.' }
@{ status='ConfigurationValidated'; policyId=$policy.PolicyId; retentionDays=$duration.TotalDays;
    expiryTrigger=$policy.ExpiryTrigger; effectiveAt=$start.ToString('O'); expiresAt=$deadline.ToString('O');
    custodyQualified=$false; productionEnabled=$false } | ConvertTo-Json
