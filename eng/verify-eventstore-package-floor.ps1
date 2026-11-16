param([string] $PackageFloorProjectPath)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = Split-Path -Parent $PSScriptRoot

function Assert-EventStorePackageFloor {
    $minimumVersion = [Version]'3.128.3'
    $projectPath = if ([string]::IsNullOrWhiteSpace($PackageFloorProjectPath)) {
        'src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj'
    }
    else {
        $PackageFloorProjectPath
    }
    $arguments = @(
        'msbuild', $projectPath, '-nologo',
        '-getItem:PackageVersion', '-p:Configuration=Release',
        '-p:UseHexalithProjectReferences=false', '/nr:false'
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

        if ($effectiveVersion -lt $minimumVersion -or $versionMatch.Groups['prerelease'].Success) {
            throw "Agents requires Hexalith.EventStore $minimumVersion or later in package mode; effective version for '$identity' is $versionText. This release must include no-op result-payload forwarding, command-status reads, trusted-extension admission, and exact-domain workload status issuance."
        }

        $effectiveVersions.Add($versionText)
    }

    foreach ($requiredPackage in @('Hexalith.EventStore.Client', 'Hexalith.EventStore.DomainService')) {
        if (-not @($eventStorePackageVersions | Where-Object { $_.Identity -ieq $requiredPackage }).Count) {
            throw "Missing required workload-status capability package row: $requiredPackage."
        }
    }

    $selectedVersions = [string]::Join(', ', @($effectiveVersions | Sort-Object -Unique))
    Write-Host "Gate: EventStore package floor $($eventStorePackageVersions.Count) selected rows at $selectedVersions >= $minimumVersion"
}

Push-Location $root
try {
    try { Assert-EventStorePackageFloor }
    catch { Write-Host $_.Exception.Message; throw }
}
finally { Pop-Location }
