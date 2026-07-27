<#
.SYNOPSIS
Builds the local Program Kit NuGet feed JTest consumes.

.DESCRIPTION
Packs the explicit Program Kit package projects listed below from an
explicitly named Program Kit checkout into packages/local-feed, using
Program Kit's own build/ProgramKit.Pack.proj, and records the produced
package digests and the source commit in packages/local-feed.manifest.json.

No discovery is performed: the Program Kit root must be supplied, and only
the exact projects listed here are packed. The Program Kit checkout is
treated as read-only source truth; packing writes only its normal bin/obj
build outputs and the requested package output directory.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ProgramKitRoot,

    [string] $OutputDirectory,

    [string] $ManifestPath
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..'))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'packages\local-feed'
}
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $repositoryRoot 'packages\local-feed.manifest.json'
}

$programKitRootResolved = (Resolve-Path -LiteralPath $ProgramKitRoot).Path
$packProject = Join-Path $programKitRootResolved 'build\ProgramKit.Pack.proj'
if (-not (Test-Path -LiteralPath $packProject)) {
    throw "Not a Program Kit checkout (missing build/ProgramKit.Pack.proj): $programKitRootResolved"
}

# Exact package projects consumed directly or transitively by JTest.
# This is the complete program-kit-role package set from Program Kit's own
# workspace package manifest, packed in full so transitive Program Kit
# dependencies always resolve from the local feed.
$packageProjects = @(
    'src/Orbyss.ProgramKit.Architecture/Orbyss.ProgramKit.Architecture.csproj',
    'src/Orbyss.ProgramKit.Artifacts/Orbyss.ProgramKit.Artifacts.csproj',
    'src/Orbyss.ProgramKit.CommandLine/Orbyss.ProgramKit.CommandLine.csproj',
    'src/Orbyss.ProgramKit.Development/Orbyss.ProgramKit.Development.csproj',
    'src/Orbyss.ProgramKit.DotNet/Orbyss.ProgramKit.DotNet.csproj',
    'src/Orbyss.ProgramKit.Modularity/Orbyss.ProgramKit.Modularity.csproj',
    'src/Orbyss.ProgramKit.Modularity.InProcess/Orbyss.ProgramKit.Modularity.InProcess.csproj',
    'src/Orbyss.ProgramKit.Planning/Orbyss.ProgramKit.Planning.csproj',
    'src/Orbyss.ProgramKit.Quality/Orbyss.ProgramKit.Quality.csproj',
    'src/Orbyss.ProgramKit.Serialization.JSON/Orbyss.ProgramKit.Serialization.JSON.csproj',
    'src/Orbyss.ProgramKit.Tasks/Orbyss.ProgramKit.Tasks.csproj',
    'src/Orbyss.ProgramKit.Tasks.Core/Orbyss.ProgramKit.Tasks.Core.csproj',
    'src/Orbyss.ProgramKit.Tasks.Hosting/Orbyss.ProgramKit.Tasks.Hosting.csproj',
    'src/Orbyss.ProgramKit.Tasks.InProcess/Orbyss.ProgramKit.Tasks.InProcess.csproj',
    'src/Orbyss.ProgramKit.Tasks.Schedules/Orbyss.ProgramKit.Tasks.Schedules.csproj',
    'src/Orbyss.ProgramKit.Tasks.Schedules.Cronos/Orbyss.ProgramKit.Tasks.Schedules.Cronos.csproj',
    'src/Orbyss.ProgramKit.Workbench/Orbyss.ProgramKit.Workbench.csproj'
)

$outputResolved = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputResolved) {
    Get-ChildItem -LiteralPath $outputResolved -Filter '*.nupkg' |
        Remove-Item -Force -Confirm:$false
}
else {
    New-Item -ItemType Directory -Path $outputResolved | Out-Null
}

$programKitCommit = (& git -C $programKitRootResolved rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not read the Program Kit commit.' }
$programKitDirty = (& git -C $programKitRootResolved status --porcelain)

# Program Kit's pack target expects up-to-date Release outputs (its source
# gate hashes compiler outputs during builds), so restore and build each
# listed package project normally before invoking the pack target. The full
# solution is deliberately not built: fixture projects need their own local
# feeds and are irrelevant to this package set.
$programKitNuGetConfig = Join-Path $programKitRootResolved 'NuGet.Config'
foreach ($relativeProject in $packageProjects) {
    $projectPath = Join-Path $programKitRootResolved ($relativeProject -replace '/', '\')
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Listed Program Kit project not found: $relativeProject"
    }
    Write-Host "Restoring and building $relativeProject"
    & dotnet restore $projectPath --configfile $programKitNuGetConfig --locked-mode
    if ($LASTEXITCODE -ne 0) { throw "Restore failed for $relativeProject" }
    & dotnet build $projectPath -c Release --no-restore /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $relativeProject" }
}

foreach ($relativeProject in $packageProjects) {
    $projectPath = Join-Path $programKitRootResolved ($relativeProject -replace '/', '\')
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Listed Program Kit project not found: $relativeProject"
    }
    Write-Host "Packing $relativeProject"
    & dotnet msbuild $packProject /t:Pack /nologo /v:minimal `
        "/p:ProgramKitPackageProject=$projectPath" `
        "/p:PackageOutputPath=$outputResolved"
    if ($LASTEXITCODE -ne 0) { throw "Pack failed for $relativeProject" }
}

$packages = Get-ChildItem -LiteralPath $outputResolved -Filter '*.nupkg' |
    Sort-Object Name |
    ForEach-Object {
        [ordered]@{
            file   = $_.Name
            digest = "sha256:$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())"
        }
    }

$manifest = [ordered]@{
    programKitCommit = $programKitCommit
    programKitDirty  = [bool]$programKitDirty
    feedDirectory    = 'packages/local-feed'
    packages         = @($packages)
}

$json = ($manifest | ConvertTo-Json -Depth 5) -replace "`r`n", "`n"
[IO.File]::WriteAllText(
    [IO.Path]::GetFullPath($ManifestPath),
    $json + "`n",
    [Text.UTF8Encoding]::new($false))

Write-Host "Feed ready: $($packages.Count) packages at $outputResolved"
Write-Host "Manifest: $ManifestPath (Program Kit commit $programKitCommit)"
