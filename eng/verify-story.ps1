param(
    [Parameter(Mandatory = $true)]
    [string] $Story
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if ($Story -ne '5.1') {
    throw "Unsupported story '$Story'. This verifier owns Story 5.1 only."
}

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Hexalith.Agents.slnx'
$packageDirectory = Join-Path $root 'artifacts/story-5.1/packages'
$testProjects = @(
    'test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj',
    'test/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj',
    'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj',
    'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj',
    'test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj'
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
    $startInfo.FileName = 'dotnet'
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
    Write-Host 'Gate: source-build'
    dotnet restore $solution -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false /m:1 /nr:false
    dotnet build $solution -c Debug --no-restore -warnaserror -p:UseHexalithProjectReferences=true /m:1 /nr:false

    foreach ($testProject in $testProjects) {
        Write-Host "Gate: test $testProject"
        dotnet test $testProject -c Debug --no-build -p:UseHexalithProjectReferences=true /m:1 /nr:false
    }

    Write-Host 'Gate: package-build'
    dotnet restore $solution -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:NuGetAudit=false /m:1 /nr:false
    dotnet build $solution -c Release --no-restore -warnaserror -p:UseHexalithProjectReferences=false /m:1 /nr:false

    foreach ($testProject in $testProjects) {
        Write-Host "Gate: package test $testProject"
        dotnet test $testProject -c Release --no-build -p:UseHexalithProjectReferences=false /m:1 /nr:false
    }

    Write-Host 'Gate: executable dependency-mode failures'
    Assert-NativeFailure -Name 'release-source-mode' -Arguments @(
        'build', 'src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj',
        '-c', 'Release', '-p:UseHexalithProjectReferences=true', '-p:UseNuGetDeps=false',
        '-p:NuGetAudit=false', '/m:1', '/nr:false'
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
        "-p:HexalithEventStoreRoot=$missingEventStoreRoot", '-p:NuGetAudit=false', '/m:1', '/nr:false'
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
