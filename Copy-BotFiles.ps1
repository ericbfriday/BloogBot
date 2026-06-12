[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$TargetDirectories,

    [Parameter()]
    [string]$SourceDirectory = 'D:\dev\bloog\bloogbot\Bot'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SourceDirectory -PathType Container)) {
    throw "Source directory not found: $SourceDirectory"
}

$sourceFullPath = (Resolve-Path -LiteralPath $SourceDirectory).Path.TrimEnd('\')
$excludedDirectory = Join-Path $sourceFullPath 'mmaps'
$excludedFiles = @(
    'botsettings.json',
    'bootstrapperSettings.json'
)

foreach ($targetDirectory in $TargetDirectories) {
    if ([string]::IsNullOrWhiteSpace($targetDirectory)) {
        continue
    }

    if (-not (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    $targetFullPath = (Resolve-Path -LiteralPath $targetDirectory).Path.TrimEnd('\')

    Write-Host "Copying from '$sourceFullPath' to '$targetFullPath'..."

    $robocopyArguments = @(
        $sourceFullPath,
        $targetFullPath,
        '/E',
        '/R:2',
        '/W:1',
        '/XD', $excludedDirectory,
        '/XF'
    ) + $excludedFiles

    & robocopy @robocopyArguments | Out-Host
    $robocopyExitCode = $LASTEXITCODE

    if ($robocopyExitCode -ge 8) {
        throw "Robocopy failed for '$targetFullPath' with exit code $robocopyExitCode."
    }
}

Write-Host 'Copy completed successfully.'
