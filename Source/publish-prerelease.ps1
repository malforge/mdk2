# Publish to Prerelease Branch
# Safely merges current branch to prerelease and pushes, without changing current branch

$ErrorActionPreference = "Stop"

Write-Host "🔍 Checking repository state..." -ForegroundColor Cyan

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to get current branch" -ForegroundColor Red
    exit 1
}

Write-Host "📍 Current branch: $currentBranch" -ForegroundColor Gray

# Check for uncommitted changes (including untracked files)
$status = git status --porcelain
if ($status) {
    Write-Host "❌ Repository has uncommitted changes:" -ForegroundColor Red
    Write-Host $status -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please commit or stash your changes before publishing." -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ No uncommitted changes" -ForegroundColor Green

# Check if current branch is already prerelease
if ($currentBranch -eq "prerelease") {
    Write-Host "ℹ️  Already on prerelease branch. Just pushing..." -ForegroundColor Yellow
    git push origin prerelease
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to push prerelease" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Pushed prerelease" -ForegroundColor Green
    exit 0
}

# Fetch latest prerelease
Write-Host "📥 Fetching latest prerelease..." -ForegroundColor Cyan
git fetch origin prerelease:prerelease 2>$null
$fetchExitCode = $LASTEXITCODE

if ($fetchExitCode -ne 0) {
    Write-Host "ℹ️  Prerelease branch doesn't exist remotely yet" -ForegroundColor Yellow
}

# Get current commit SHA
$currentSHA = git rev-parse HEAD

Write-Host ""
Write-Host "📦 Ready to publish:" -ForegroundColor Cyan
Write-Host "   From: $currentBranch ($($currentSHA.Substring(0,7)))" -ForegroundColor Gray
Write-Host "   To:   prerelease" -ForegroundColor Gray
Write-Host ""

# Confirm
$response = Read-Host "Continue? (y/N)"
if ($response -ne "y" -and $response -ne "Y") {
    Write-Host "❌ Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🚀 Publishing to prerelease..." -ForegroundColor Cyan

# Update local prerelease branch to current HEAD (without checking it out)
git branch -f prerelease HEAD
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to update local prerelease branch" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Updated local prerelease branch" -ForegroundColor Green

# Push prerelease to remote
git push origin prerelease --force-with-lease
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to push prerelease" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️  The remote prerelease branch may have been updated by someone else." -ForegroundColor Yellow
    Write-Host "   Fetch latest changes and try again, or use --force if you're sure." -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Pushed prerelease" -ForegroundColor Green
Write-Host ""
Write-Host "✨ Success! Prerelease branch published." -ForegroundColor Green
Write-Host "   Your current branch ($currentBranch) remains unchanged." -ForegroundColor Gray
Write-Host ""
Write-Host "🔗 GitHub Actions will now build and publish release artifacts." -ForegroundColor Cyan
