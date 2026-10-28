param(
    [Parameter(Mandatory = $true)]
    [string] $Story,
    [ValidateSet('Both', 'Package')]
    [string] $DependencyMode = 'Both'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if ($Story -ne '5.1') {
    throw "Unsupported story '$Story'. This verifier owns Story 5.1 only."
}

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Hexalith.Agents.slnx'
$consumerAuthorityValidator = Join-Path $root 'references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1'
$authoritativeCatalog = Join-Path $root 'references/Hexalith.Builds/Props/Directory.Packages.props'
$packageDirectory = Join-Path $root 'artifacts/story-5.1/packages'
$testProjects = @(
    [PSCustomObject]@{
        Path = 'test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj'
        DebugMinimumExpectedTests = 529
        ReleaseMinimumExpectedTests = 529
    }
    [PSCustomObject]@{
        Path = 'test/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj'
        DebugMinimumExpectedTests = 6
        ReleaseMinimumExpectedTests = 6
    }
    [PSCustomObject]@{
        Path = 'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj'
        DebugMinimumExpectedTests = 787
        ReleaseMinimumExpectedTests = 787
    }
    [PSCustomObject]@{
        Path = 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj'
        DebugMinimumExpectedTests = 573
        ReleaseMinimumExpectedTests = 549
    }
    [PSCustomObject]@{
        Path = 'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
        DebugMinimumExpectedTests = 1073
        ReleaseMinimumExpectedTests = 1073
    }
)
$unrelatedPackage = Join-Path $packageDirectory 'Unrelated.Package.1.0.0.nupkg'

function Assert-NativeFailure {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,
        [Parameter(Mandatory = $true)]
        [string] $ExpectedDiagnostic
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    $startInfo.WorkingDirectory = $root
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "Negative gate '$Name' could not start dotnet."
        }

        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $combinedOutput = $standardOutput.GetAwaiter().GetResult() + $standardError.GetAwaiter().GetResult()
        if ($process.ExitCode -eq 0) {
            throw "Negative gate '$Name' unexpectedly succeeded."
        }

        if (-not $combinedOutput.Contains($ExpectedDiagnostic, [StringComparison]::Ordinal)) {
            throw "Negative gate '$Name' did not emit '$ExpectedDiagnostic'. Output:`n$combinedOutput"
        }

        Write-Host "Gate: negative $Name rejected as intended (exit $($process.ExitCode))"
    }
    finally {
        $process.Dispose()
    }
}

$script:SourceMappingEnvironmentNames = @()

function Enable-PlatformSourceMapping {
    $cli = Join-Path $root 'references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj'
    $manifest = Join-Path $root 'module/hexalith.module-manifest.v2.json'
    $workspaceParent = Join-Path $root 'artifacts/story-5.1'
    New-Item -ItemType Directory -Force -Path $workspaceParent | Out-Null
    $workspace = Join-Path $workspaceParent ('source-mapping-' + [guid]::NewGuid().ToString('N'))
    Write-Host 'Gate: platform source mapping'
    $lines = @(dotnet run --project $cli -v q -- prepare-build --manifest $manifest --mode source --workspace $workspace --output json)
    $raw = @($lines | Where-Object { $_.TrimStart().StartsWith('{') } | Select-Object -Last 1) -join ''
    $parsed = $raw | ConvertFrom-Json
    if ($parsed.status -ne 'prepared' -or $parsed.buildPreparation.mode -ne 'source') {
        throw "Platform source mapping was not prepared. Output:`n$($lines -join "`n")"
    }

    $properties = @()
    if ($null -ne $parsed.buildPreparation.environment) {
        $properties = @($parsed.buildPreparation.environment.PSObject.Properties)
    }

    $script:SourceMappingEnvironmentNames = @($properties | ForEach-Object { $_.Name } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    foreach ($property in $properties) {
        if ([string]::IsNullOrWhiteSpace($property.Name)) {
            continue
        }

        [System.Environment]::SetEnvironmentVariable($property.Name, [string] $property.Value)
    }
}

function Disable-PlatformSourceMapping {
    foreach ($name in $script:SourceMappingEnvironmentNames) {
        [System.Environment]::SetEnvironmentVariable($name, $null)
    }

    $script:SourceMappingEnvironmentNames = @()
}

function New-UnrelatedPackageArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $archive = [System.IO.Compression.ZipArchive]::new(
        $stream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false)
    try {
        $entry = $archive.CreateEntry('Unrelated.Package.nuspec')
        $writer = [System.IO.StreamWriter]::new($entry.Open())
        try {
            $writer.Write('<?xml version="1.0"?><package><metadata><id>Unrelated.Package</id><version>1.0.0</version><authors>Story verifier</authors><description>Output safety sentinel.</description></metadata></package>')
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $archive.Dispose()
        $stream.Dispose()
    }
}

Push-Location $root
try {
    Write-Host 'Gate: shared package authority'
    $exclusionFile = Join-Path $root 'eng/package-authority-exclusions.txt'
    $exclusions = @(Get-Content -LiteralPath $exclusionFile |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.TrimStart().StartsWith('#') }) -join ';'
    pwsh -NoProfile -File $consumerAuthorityValidator -RepositoryRoot $root -CatalogPath $authoritativeCatalog -ExcludedPath $exclusions

    Write-Host 'Gate: effective EventStore package floor'
    pwsh -NoProfile -File (Join-Path $root 'eng/verify-story-5.2.ps1') -PackageFloorOnly

    if ($DependencyMode -eq 'Both') {
        Enable-PlatformSourceMapping
        Write-Host 'Gate: source-build'
        dotnet restore $solution -p:Configuration=Debug -p:UseHexalithProjectReferences=true /m:1 /nr:false
        dotnet build $solution -c Debug --no-restore -warnaserror -p:UseHexalithProjectReferences=true /m:1 /nr:false

        foreach ($testProject in $testProjects) {
            Write-Host "Gate: test $($testProject.Path) (minimum $($testProject.DebugMinimumExpectedTests))"
            dotnet test --project $testProject.Path -c Debug --no-build -p:UseHexalithProjectReferences=true --minimum-expected-tests $testProject.DebugMinimumExpectedTests --fail-skips on
        }
    }

    Disable-PlatformSourceMapping
    Write-Host 'Gate: package-build'
    dotnet restore $solution -p:Configuration=Release -p:UseHexalithProjectReferences=false /m:1 /nr:false
    dotnet build $solution -c Release --no-restore -warnaserror -p:UseHexalithProjectReferences=false /m:1 /nr:false

    foreach ($testProject in $testProjects) {
        Write-Host "Gate: package test $($testProject.Path) (minimum $($testProject.ReleaseMinimumExpectedTests))"
        dotnet test --project $testProject.Path -c Release --no-build -p:UseHexalithProjectReferences=false --minimum-expected-tests $testProject.ReleaseMinimumExpectedTests --fail-skips on
    }

    Write-Host 'Gate: executable dependency-mode failures'
    Assert-NativeFailure -Name 'release-source-mode' -Arguments @(
        'build', 'src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj',
        '-c', 'Release', '-p:UseHexalithProjectReferences=true', '-p:UseNuGetDeps=false',
        '/m:1', '/nr:false'
    ) -ExpectedDiagnostic 'Release builds must use package references for external Hexalith libraries.'
    Assert-NativeFailure -Name 'pack-source-mode' -Arguments @(
        'pack', 'src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj',
        '-c', 'Debug', '--no-build', '-p:UseHexalithProjectReferences=true', '-p:UseNuGetDeps=false',
        '-p:PackageVersion=0.0.0-negative', '/m:1', '/nr:false'
    ) -ExpectedDiagnostic 'Package creation must not use external Hexalith project references.'
    $missingEventStoreRoot = Join-Path $root 'artifacts/story-5.1/missing-eventstore'
    Assert-NativeFailure -Name 'missing-source-root' -Arguments @(
        'build', 'src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj',
        '-c', 'Debug', '-p:UseHexalithProjectReferences=true', '-p:UseNuGetDeps=false',
        "-p:HexalithEventStoreRoot=$missingEventStoreRoot", '/m:1', '/nr:false'
    ) -ExpectedDiagnostic "Source dependency 'references/Hexalith.EventStore' is missing."

    Write-Host 'Gate: exact-package-inventory'
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
    New-UnrelatedPackageArchive -Path $unrelatedPackage
    python3 scripts/pack-release-packages.py $packageDirectory 0.0.0-story-5-1
    if (-not (Test-Path -LiteralPath $unrelatedPackage -PathType Leaf)) {
        throw 'Package automation deleted an unrelated caller-owned archive.'
    }

    Remove-Item -LiteralPath $unrelatedPackage
    python3 scripts/validate-nuget-packages.py $packageDirectory

    Write-Host 'Gate: isolated-package-consumer'
    python3 scripts/validate-consumer-package-references.py $packageDirectory
}
finally {
    if (Test-Path -LiteralPath $unrelatedPackage -PathType Leaf) {
        Remove-Item -LiteralPath $unrelatedPackage
    }

    Pop-Location
}
