<#
.SYNOPSIS
Regenerates the jtest console host from the typed Open Console document.

.DESCRIPTION
1. Runs tools/JTest.HostInputs to write hosting/shell.json,
   hosting/artifact-manifest.json, and hosting/inputs/*.json with exact
   digests.
2. Invokes the backed Program Kit operation
   `dotnet generate-host console` from an explicitly named Program Kit
   checkout, replacing only the generated files under src/JTest.Cli.Host
   (GeneratedHost.csproj, Composition/Program.cs, ProgramKitGenerated/**).
   Consumer-owned files in that project are preserved.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ProgramKitRoot
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..'))
$programKitRootResolved = (Resolve-Path -LiteralPath $ProgramKitRoot).Path
$hostingRoot = Join-Path $repositoryRoot 'hosting'
$hostRoot = Join-Path $repositoryRoot 'src\JTest.Cli.Host'

Write-Host 'Writing host generation inputs'
& dotnet run --project (Join-Path $repositoryRoot 'tools\JTest.HostInputs') -c Release -- $hostingRoot
if ($LASTEXITCODE -ne 0) { throw 'Host input generation failed.' }

$staging = Join-Path ([IO.Path]::GetTempPath()) ("jtest-host-generation-" + [Guid]::NewGuid().ToString('N'))

Write-Host 'Generating the console host through the backed Program Kit operation'
& dotnet run --project (Join-Path $programKitRootResolved 'src\Orbyss.ProgramKit.CommandLine') -c Release -- `
    dotnet generate-host console `
    --shell (Join-Path $hostingRoot 'shell.json') `
    --host 'pkid:host:jtest:cli' `
    --artifact-manifest (Join-Path $hostingRoot 'artifact-manifest.json') `
    --output $staging
if ($LASTEXITCODE -ne 0) { throw 'Console host generation failed.' }

Write-Host 'Replacing the generated files (consumer-owned files are preserved)'
foreach ($generated in @('GeneratedHost.csproj', 'Composition\Program.cs', 'ProgramKitGenerated')) {
    $path = Join-Path $hostRoot $generated
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force -Confirm:$false
    }
}

New-Item -ItemType Directory -Force -Path $hostRoot | Out-Null
$stagingPrefix = $staging.TrimEnd('\') + '\'
foreach ($sourcePath in Get-ChildItem -LiteralPath $staging -Recurse -File) {
    $relativePath = $sourcePath.FullName.Substring($stagingPrefix.Length)
    if ($relativePath -eq 'Directory.Build.targets') {
        # Consumer-owned wiring lives in Directory.Build.targets; the
        # generator emits an empty placeholder that must not clobber it.
        continue
    }

    $targetPath = Join-Path $hostRoot $relativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetPath) | Out-Null
    Copy-Item -LiteralPath $sourcePath.FullName -Destination $targetPath -Force
}

Remove-Item -LiteralPath $staging -Recurse -Force -Confirm:$false
Write-Host "Generated console host at $hostRoot"
