#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the same build / lint / test steps CI runs, locally, before a push.

.DESCRIPTION
    Backend: dotnet build + dotnet test. The integration tests spin up a
    Postgres container via Testcontainers, so a Docker daemon must be running
    (docker compose is not required).

    Frontend: npm run lint + npm run build, in src/web.

    With no switch, runs both stacks. -Api or -Web runs just that one.

.EXAMPLE
    ./scripts/check.ps1

.EXAMPLE
    ./scripts/check.ps1 -Web
#>
[CmdletBinding()]
param(
    [switch]$Api,
    [switch]$Web
)

$ErrorActionPreference = 'Stop'
# Handle native-command failures ourselves via $LASTEXITCODE, uniformly across
# PowerShell versions.
$PSNativeCommandUseErrorActionPreference = $false

$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo

# No switch given: run everything.
if (-not $Api -and -not $Web) {
    $Api = $true
    $Web = $true
}

function Invoke-Step {
    param(
        [string]$Label,
        [scriptblock]$Action
    )

    Write-Host ''
    Write-Host "> $Label" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed (exit $LASTEXITCODE)"
    }
}

try {
    if ($Api) {
        docker info *> $null
        if ($LASTEXITCODE -ne 0) {
            throw 'Docker does not appear to be running; the integration tests need it.'
        }

        Invoke-Step 'dotnet build' { dotnet build bitewing.sln -c Release }
        Invoke-Step 'dotnet test' { dotnet test bitewing.sln -c Release --no-build }
    }

    if ($Web) {
        Push-Location (Join-Path $repo 'src/web')
        try {
            Invoke-Step 'npm run lint' { npm run lint }
            Invoke-Step 'npm run build' { npm run build }
        }
        finally {
            Pop-Location
        }
    }

    Write-Host ''
    Write-Host 'All checks passed.' -ForegroundColor Green
}
finally {
    Pop-Location
}
