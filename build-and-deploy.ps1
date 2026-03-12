#Requires -Version 7.0
param(
    [string]$TargetDir = 'C:\soft\mysqlintrospect'
)

$ErrorActionPreference = 'Stop'
$SourceDir = $PSScriptRoot
$ProjectDir = Join-Path $SourceDir 'MySqlIntrospect'
$ProcessName = 'MySqlIntrospect'

# Publish in Release mode
Write-Host 'Publishing MySqlIntrospect in Release mode...' -ForegroundColor Cyan
dotnet publish $ProjectDir -c Release -o (Join-Path $SourceDir 'publish')
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Publish failed.' -ForegroundColor Red
    exit 1
}

# Kill any running instance from the target directory
$running = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($TargetDir, [System.StringComparison]::OrdinalIgnoreCase) }
if ($running) {
    Write-Host "Stopping running $ProcessName process(es)..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# Remove existing contents from target directory
if (Test-Path $TargetDir) {
    Write-Host "Removing existing files from $TargetDir..." -ForegroundColor Yellow
    Remove-Item -Path "$TargetDir\*" -Recurse -Force
} else {
    New-Item -Path $TargetDir -ItemType Directory | Out-Null
}

# Copy published output to target directory
$publishDir = Join-Path $SourceDir 'publish'
Write-Host "Copying published files to $TargetDir..." -ForegroundColor Cyan
Copy-Item -Path "$publishDir\*" -Destination $TargetDir -Recurse -Force

Write-Host 'Deploy complete.' -ForegroundColor Green
