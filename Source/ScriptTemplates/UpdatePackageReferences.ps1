# Paths to the PackageVersion.txt files for external packages 
$pbPackagerVersionFile = "../Mdk.CommandLine/PackageVersion.txt"
$referencesVersionFile   = "../Mdk.References/PackageVersion.txt"
$pbAnalyzersVersionFile  = "../Mdk.PbAnalyzers/PackageVersion.txt"
$modAnalyzersVersionFile = "../Mdk.ModAnalyzers/PackageVersion.txt"

# Array of .csproj file paths (relative to the ScriptTemplates project)
$projectFiles = @(
    "content/0_Script/PbScript.csproj",
    "content/1_Mod/ModProject.csproj"
)

# Path to this project's PackageVersion.txt file and ReleaseNotes.txt
$currentPackageVersionFile = "PackageVersion.txt"
$releaseNotesFile          = "ReleaseNotes.txt"

# Function to update the package version and return if it was changed in any project
function Update-PackageVersion {
    param (
        [string]$packageName,
        [string]$versionFilePath,
        [string[]]$projectFiles
    )

    if (-Not (Test-Path $versionFilePath)) {
        Write-Host "Error: Version file not found at $versionFilePath" -ForegroundColor Red
        exit 1
    }

    Write-Host "Updating $packageName from $versionFilePath..."
    $newVersionRaw = (Get-Content $versionFilePath -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($newVersionRaw)) {
        Write-Host "Error: Version is empty in $versionFilePath" -ForegroundColor Red
        exit 1
    }

    # Check if the version is primary (e.g., "1.2.3")
    if ($newVersionRaw -notmatch '^\d+\.\d+\.\d+$') {
        Write-Host "Skipping update for $packageName because version '$newVersionRaw' is non-primary." -ForegroundColor Yellow
        return $false
    }

    $newVersion = $newVersionRaw
    Write-Host "New real version for ${packageName}: $newVersion"

    $updatedAny = $false

    foreach ($projectFile in $projectFiles) {
        if (-Not (Test-Path $projectFile)) {
            Write-Host "Warning: Project file $projectFile not found." -ForegroundColor Yellow
            continue
        }

        [xml]$xml = Get-Content $projectFile
        $projectUpdated = $false

        $packageRefs = $xml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageName }
        if (-not $packageRefs) {
            Write-Host "Package $packageName not found in $projectFile"
            continue
        }

        foreach ($packageRef in $packageRefs) {
            $currentVersion = $packageRef.Version
            if ($currentVersion -ne $newVersion) {
                $packageRef.Version = $newVersion
                Write-Host "Updated $packageName to version $newVersion in $projectFile"
                $projectUpdated = $true
            }
            else {
                Write-Host "$packageName is already up to date in $projectFile."
            }
        }
        if ($projectUpdated) {
            $xml.Save($projectFile)
            $updatedAny = $true
        }
    }

    return $updatedAny
}

# Function to bump this package's version
function Bump-PackageVersion {
    if (-Not (Test-Path $currentPackageVersionFile)) {
        Write-Host "Error: Current package version file not found!" -ForegroundColor Red
        exit 1
    }

    $currentVersionRaw = (Get-Content $currentPackageVersionFile -Raw).Trim()
    # Assume the current version file is primary (e.g., "1.2.3")
    Write-Host "Current version of this package: $currentVersionRaw"

    $versionParts = $currentVersionRaw.Split('.')
    $versionParts[2] = [int]$versionParts[2] + 1  # Increment the patch number
    $newVersion = $versionParts -join '.'

    Set-Content $currentPackageVersionFile -Value $newVersion
    Write-Host "Bumped this package version to: $newVersion"

    Update-ReleaseNotes $newVersion
}

# Function to update ReleaseNotes.txt with the new version and description
function Update-ReleaseNotes {
    param (
        [string]$newVersion
    )

    if (-Not (Test-Path $releaseNotesFile)) {
        Write-Host "Error: Release notes file not found!" -ForegroundColor Red
        exit 1
    }

    $currentReleaseNotes = Get-Content $releaseNotesFile
    $newEntry = "v.$newVersion`n - Updated the package versions in the template.`n"
    $updatedReleaseNotes = $newEntry + "`n" + ($currentReleaseNotes -join "`n")
    Set-Content $releaseNotesFile -Value $updatedReleaseNotes
    Write-Host "Updated ReleaseNotes.txt with new version: v.$newVersion"
}

# Track if any package versions changed
$versionChanged = $false

$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.PbPackager"   $pbPackagerVersionFile   $projectFiles)
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.ModPackager"    $pbPackagerVersionFile   $projectFiles)
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.References"     $referencesVersionFile   $projectFiles)
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.PbAnalyzers"    $pbAnalyzersVersionFile  $projectFiles)
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.ModAnalyzers"   $modAnalyzersVersionFile $projectFiles)

if ($versionChanged) {
    Bump-PackageVersion
}
else {
    Write-Host "No changes in package versions. No version bump needed."
}

Write-Host "Package reference updates completed."
