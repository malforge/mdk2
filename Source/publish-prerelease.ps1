# Publish to Prerelease Branch
# Safely merges current branch to prerelease and pushes, without changing current branch

$ErrorActionPreference = "Stop"

Write-Host "Checking repository state..." -ForegroundColor Cyan

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to get current branch" -ForegroundColor Red
    exit 1
}

Write-Host "Current branch: $currentBranch" -ForegroundColor Gray

# Check for uncommitted changes (including untracked files)
$status = git status --porcelain
if ($status) {
    Write-Host "ERROR: Repository has uncommitted changes:" -ForegroundColor Red
    Write-Host $status -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please commit or stash your changes before publishing." -ForegroundColor Yellow
    exit 1
}

Write-Host "OK: No uncommitted changes" -ForegroundColor Green

# Check if current branch is already prerelease
if ($currentBranch -eq "prerelease") {
    Write-Host "Already on prerelease branch. Just pushing..." -ForegroundColor Yellow
    git push origin prerelease
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to push prerelease" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK: Pushed prerelease" -ForegroundColor Green
    exit 0
}

# Fetch latest prerelease
Write-Host "Fetching latest prerelease..." -ForegroundColor Cyan
$null = git fetch origin prerelease:prerelease 2>&1
$fetchExitCode = $LASTEXITCODE

if ($fetchExitCode -ne 0) {
    Write-Host "INFO: Prerelease branch doesn't exist remotely yet" -ForegroundColor Yellow
}

# Get current commit SHA
$currentSHA = git rev-parse HEAD

Write-Host ""
Write-Host "Ready to publish:" -ForegroundColor Cyan
Write-Host "   From: $currentBranch ($($currentSHA.Substring(0,7)))" -ForegroundColor Gray
Write-Host "   To:   prerelease" -ForegroundColor Gray
Write-Host ""

# Confirm
$response = Read-Host "Continue? (y/N)"
if ($response -ne "y" -and $response -ne "Y") {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Publishing to prerelease..." -ForegroundColor Cyan

# Update local prerelease branch to current HEAD (without checking it out)
git branch -f prerelease HEAD
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to update local prerelease branch" -ForegroundColor Red
    exit 1
}

Write-Host "OK: Updated local prerelease branch" -ForegroundColor Green

# Push prerelease to remote
git push origin prerelease --force-with-lease
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to push prerelease" -ForegroundColor Red
    Write-Host ""
    Write-Host "WARNING: The remote prerelease branch may have been updated by someone else." -ForegroundColor Yellow
    Write-Host "   Fetch latest changes and try again, or use --force if you're sure." -ForegroundColor Yellow
    exit 1
}

Write-Host "OK: Pushed prerelease" -ForegroundColor Green
Write-Host ""
Write-Host "Success! Prerelease branch published." -ForegroundColor Green
Write-Host "   Your current branch ($currentBranch) remains unchanged." -ForegroundColor Gray
Write-Host ""
Write-Host "GitHub Actions will now build and publish release artifacts." -ForegroundColor Cyan

