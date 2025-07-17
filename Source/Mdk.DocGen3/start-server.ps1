<#
.SYNOPSIS
  Starts dotnet-serve on the project's output folder.

.DESCRIPTION
  Locates the script's directory, then serves
  bin\debug\net9.0\output from localhost:8000 (or a custom port).

.PARAMETER Port
  The port to listen on. Default is 8000.

.EXAMPLE
  ./start-server.ps1
  Starts on http://localhost:8000

  ./start-server.ps1 -Port 5000
  Starts on http://localhost:5000
#>

param(
    [int]$Port = 8000
)

# 1) Find the folder this script resides in
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# 2) Compute the target folder
$targetDir = Join-Path $scriptDir 'bin\debug\net9.0\output'

if (-not (Test-Path $targetDir)) {
    Write-Error "Directory not found: $targetDir"
    exit 1
}

# 3) Ensure dotnet-serve is installed
if (-not (Get-Command dotnet-serve -ErrorAction SilentlyContinue)) {
    Write-Error "'dotnet-serve' not found. Install it with:"
    Write-Host "  dotnet tool install --global dotnet-serve" -ForegroundColor Yellow
    exit 1
}

# 4) Launch the server
Write-Host "Serving '$targetDir' at http://localhost:$Port" -ForegroundColor Cyan
dotnet-serve --directory "$targetDir" --port $Port
