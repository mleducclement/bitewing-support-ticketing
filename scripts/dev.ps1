#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts the whole Bitewing stack for local development.

.DESCRIPTION
    Brings up the Postgres container, then launches the API and the web dev
    server as silent background processes (no extra windows, output redirected
    to logs/). Press Enter or Ctrl+C in this window to stop the API and web
    server; the database is left running.

    Tail a log while it runs, e.g.:  Get-Content -Wait logs/web.log

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

# Launch `command` (run through pwsh so PATH resolution matches a normal shell)
# hidden, with no new window, stdout/stderr going to logs/<name>.log.
function Start-Silent {
    param(
        [string]$Name,
        [string]$WorkDir,
        [string]$Command
    )

    $logDir = Join-Path $repo 'logs'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null

    Start-Process pwsh -PassThru -WindowStyle Hidden `
        -ArgumentList @('-NoProfile', '-Command', "Set-Location '$WorkDir'; $Command") `
        -RedirectStandardOutput (Join-Path $logDir "$Name.log") `
        -RedirectStandardError (Join-Path $logDir "$Name.err.log")
}

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
        $procs += Start-Silent -Name 'api' -WorkDir $repo `
            -Command 'dotnet run --project src/bitewing'
        $procs += Start-Silent -Name 'web' -WorkDir (Join-Path $repo 'src/web') `
            -Command 'npm run dev'

        Write-Host ''
        Write-Host 'API: http://localhost:5073' -ForegroundColor Green
        Write-Host 'Web: http://localhost:5173  (open this one)' -ForegroundColor Green
        Write-Host 'Login: skerrigan@bitewing.net / Password123!' -ForegroundColor Green
        Write-Host 'Logs:  logs/api.log  logs/web.log' -ForegroundColor DarkGray
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