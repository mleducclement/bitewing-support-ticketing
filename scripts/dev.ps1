#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts the whole Bitewing stack for local development.

.DESCRIPTION
    Brings up the Postgres container, then launches the API and the web dev
    server (each in its own window). Press Enter or Ctrl+C in this window to
    stop the API and web server; the database is left running.

.PARAMETER Prod
    Build the web bundle and serve it single-origin from the API (how it runs
    on Render). No Vite dev server; open http://localhost:5073.

.PARAMETER StopDb
    Also stop the Postgres container on exit. Seeded data survives in the volume.

.EXAMPLE
    ./scripts/dev.ps1

.EXAMPLE
    ./scripts/dev.ps1 -Prod
#>
[CmdletBinding()]
param(
    [switch]$Prod,
    [switch]$StopDb
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo

$procs = @()

try {
    Write-Host 'Starting Postgres...' -ForegroundColor Cyan
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed' }

    if ($Prod) {
        Write-Host 'Building web bundle...' -ForegroundColor Cyan
        Push-Location src/web
        npm run build
        Pop-Location
        if ($LASTEXITCODE -ne 0) { throw 'npm run build failed' }

        Write-Host ''
        Write-Host 'App (API + bundle): http://localhost:5073' -ForegroundColor Green
        Write-Host 'Login: skerrigan@bitewing.net / Password123!' -ForegroundColor Green
        Write-Host 'Ctrl+C to stop.' -ForegroundColor DarkGray
        Write-Host ''
        # Foreground: Ctrl+C ends the run and drops into finally.
        dotnet run --project src/bitewing
    }
    else {
        $procs += Start-Process pwsh -PassThru -ArgumentList @(
            '-NoExit', '-Command', "Set-Location '$repo'; dotnet run --project src/bitewing")
        $procs += Start-Process pwsh -PassThru -ArgumentList @(
            '-NoExit', '-Command', "Set-Location '$repo/src/web'; npm run dev")

        Write-Host ''
        Write-Host 'API: http://localhost:5073' -ForegroundColor Green
        Write-Host 'Web: http://localhost:5173  (open this one)' -ForegroundColor Green
        Write-Host 'Login: skerrigan@bitewing.net / Password123!' -ForegroundColor Green
        Write-Host ''
        Read-Host 'Press Enter (or Ctrl+C) to stop the API and web server'
    }
}
finally {
    foreach ($p in $procs) {
        if ($p -and -not $p.HasExited) {
            $p.Kill($true)  # $true: also kill the child process tree (dotnet / node)
        }
    }
    if ($StopDb) {
        Write-Host 'Stopping Postgres...' -ForegroundColor Cyan
        docker compose down
    }
    Pop-Location
}
