#Requires -Version 7.0
param(
    [string]$MySqlTargetDir = 'C:\soft\mysqlintrospect',
    [string]$PostgresTargetDir = 'C:\soft\postgresintrospect'
)

$ErrorActionPreference = 'Stop'
$SourceDir = $PSScriptRoot

function Deploy-Project {
    param(
        [string]$ProjectDir,
        [string]$ProcessName,
        [string]$TargetDir,
        [string]$PublishDir
    )

    Write-Host "Publishing $ProcessName in Release mode..." -ForegroundColor Cyan
    dotnet publish $ProjectDir -c Release -o $PublishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish of $ProcessName failed." -ForegroundColor Red
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
    Write-Host "Copying published files to $TargetDir..." -ForegroundColor Cyan
    Copy-Item -Path "$PublishDir\*" -Destination $TargetDir -Recurse -Force

    Write-Host "Deploy of $ProcessName complete." -ForegroundColor Green
}

Deploy-Project `
    -ProjectDir (Join-Path $SourceDir 'MySqlIntrospect') `
    -ProcessName 'MySqlIntrospect' `
    -TargetDir $MySqlTargetDir `
    -PublishDir (Join-Path $SourceDir 'publish-mysql')

Deploy-Project `
    -ProjectDir (Join-Path $SourceDir 'PostgresIntrospect') `
    -ProcessName 'PostgresIntrospect' `
    -TargetDir $PostgresTargetDir `
    -PublishDir (Join-Path $SourceDir 'publish-postgres')

Write-Host 'All deployments complete.' -ForegroundColor Green
