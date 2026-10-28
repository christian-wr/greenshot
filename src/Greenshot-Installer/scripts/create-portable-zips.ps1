<#
.SYNOPSIS
    Creates the portable ZIP files (full, ARM64 and light) next to the installers of a local Release build.

.DESCRIPTION
    Uses prepare-portable.ps1, like the release workflow. The ZIP files are named after the installer that was just
    built: Greenshot-INSTALLER-<version>.exe gives Greenshot-PORTABLE-<version>.zip, Greenshot-ARM64-PORTABLE-<version>.zip
    and Greenshot-Light-PORTABLE-<version>.zip.
    Greenshot Light is its own build of Greenshot.exe (bin\Release-Light, see Greenshot.csproj), and so is the full version for
    ARM64 Windows (bin\Release-ARM64, see BuildArm64Edition in Greenshot-Installer.csproj); without -Arm64BuildArtifactsPath
    there is no ARM64 ZIP file.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRootPath,
    [Parameter(Mandatory = $true)]
    [string]$BuildArtifactsPath,
    [Parameter(Mandatory = $true)]
    [string]$LightBuildArtifactsPath,
    [string]$Arm64BuildArtifactsPath,
    [Parameter(Mandatory = $true)]
    [string]$InstallerDir
)

$ErrorActionPreference = 'Stop'

$installer = Get-ChildItem -Path $InstallerDir -Filter 'Greenshot-INSTALLER-*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installer) {
    throw "No Greenshot-INSTALLER-*.exe found in $InstallerDir"
}
$version = $installer.BaseName.Substring('Greenshot-INSTALLER-'.Length)

$prepareScript = Join-Path $RepositoryRootPath 'prepare-portable.ps1'
$editions = @(@{ Prefix = 'Greenshot'; Light = $false; Path = $BuildArtifactsPath }, @{ Prefix = 'Greenshot-Light'; Light = $true; Path = $LightBuildArtifactsPath })
if ($Arm64BuildArtifactsPath) {
    $editions += @{ Prefix = 'Greenshot-ARM64'; Light = $false; Path = $Arm64BuildArtifactsPath }
}
foreach ($edition in $editions) {
    $staging = Join-Path ([System.IO.Path]::GetTempPath()) ("greenshot-portable-" + [guid]::NewGuid().ToString('N'))
    try {
        & $prepareScript -RepositoryRootPath $RepositoryRootPath -BuildArtifactsPath $edition.Path -OutputPath $staging -Light:$edition.Light
        $zip = Join-Path $InstallerDir ("{0}-PORTABLE-{1}.zip" -f $edition.Prefix, $version)
        Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -Force
        Write-Host "Created $zip"
    }
    finally {
        Remove-Item -Path $staging -Recurse -Force -ErrorAction SilentlyContinue
    }
}
