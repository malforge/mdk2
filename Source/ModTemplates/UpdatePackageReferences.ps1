# Paths to the PackageVersion.txt files for external packages
$modPackagerVersionFile = "../Mdk.CommandLine/PackageVersion.txt"
$referencesVersionFile = "../Mdk.References/PackageVersion.txt"
$modAnalyzersVersionFile = "../Mdk.ModAnalyzers/PackageVersion.txt"

# Path to the .csproj file (relative to the ScriptTemplates project)
$projectFile = "content/0_Mod/Mod.csproj"

# Path to this project's PackageVersion.txt file and ReleaseNotes.txt
$currentPackageVersionFile = "PackageVersion.txt"
$releaseNotesFile = "ReleaseNotes.txt"

# Function to update the package version and return if it was changed
function Update-PackageVersion {
    param (
        [string]$packageName,
        [string]$versionFilePath
    )

    # Check if version file exists
    if (-Not (Test-Path $versionFilePath)) {
        Write-Host "Error: Version file not found at $versionFilePath" -ForegroundColor Red
        exit 1
    }

    Write-Host "Updating $packageName from $versionFilePath..."

    # Read the version from the text file
    $newVersion = Get-Content $versionFilePath -Raw
    $newVersion = $newVersion.Trim()
    if ([string]::IsNullOrWhiteSpace($newVersion)) {
        Write-Host "Error: Version is empty in $versionFilePath" -ForegroundColor Red
        exit 1
    }
    Write-Host "New version for $packageName`: $newVersion"

    # Load the .csproj file as XML
    [xml]$xml = Get-Content $projectFile

    # Find the PackageReference element by Include attribute
    $packageRef = $xml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageName }

    if (-not $packageRef) {
        Write-Host "Error: Package $packageName not found in the project file" -ForegroundColor Red
        exit 1
    }

    # Check if the version is different
    $currentVersion = $packageRef.Version
    if ($currentVersion -ne $newVersion) {
        # Update the version attribute
        $packageRef.Version = $newVersion
        Write-Host "Updated $packageName to version $newVersion in $projectFile"

        # Save the updated XML back to the .csproj file
        $xml.Save($projectFile)
        return $true
    } else {
        Write-Host "$packageName is already up to date."
        return $false
    }
}

# Function to bump this package's version
function Bump-PackageVersion {
    # Check if the version file exists
    if (-Not (Test-Path $currentPackageVersionFile)) {
        Write-Host "Error: Current package version file not found!" -ForegroundColor Red
        exit 1
    }

    # Read the current version
    $currentVersion = Get-Content $currentPackageVersionFile -Raw
    $currentVersion = $currentVersion.Trim()

    Write-Host "Current version of this package`: $currentVersion"

    # Increment the version number
    $versionParts = $currentVersion.Split('.')
    $versionParts[2] = [int]$versionParts[2] + 1  # Increment the patch number (last part)
    $newVersion = $versionParts -join '.'

    # Write the new version back to the version file
    Set-Content $currentPackageVersionFile -Value $newVersion
    Write-Host "Bumped this package version to`: $newVersion"

    # Update the ReleaseNotes.txt file
    Update-ReleaseNotes $newVersion
}

# Function to update ReleaseNotes.txt with the new version and description
function Update-ReleaseNotes {
    param (
        [string]$newVersion
    )

    # Check if the release notes file exists
    if (-Not (Test-Path $releaseNotesFile)) {
        Write-Host "Error: Release notes file not found!" -ForegroundColor Red
        exit 1
    }

    # Read the current content of the release notes
    $currentReleaseNotes = Get-Content $releaseNotesFile

    # Create a new entry for the release notes
    $newEntry = "v.$newVersion`n - Updated the package versions in the template.`n"

    # Combine the new entry with the existing release notes (add a blank line between)
    $updatedReleaseNotes = $newEntry + "`n" + ($currentReleaseNotes -join "`n")

    # Write the updated release notes back to the file
    Set-Content $releaseNotesFile -Value $updatedReleaseNotes
    Write-Host "Updated ReleaseNotes.txt with new version: v.$newVersion"
}

# Track whether any package versions changed
$versionChanged = $false

# Update the package references and check if any versions changed
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.ModPackager" $modPackagerVersionFile)
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.References" $referencesVersionFile)
$versionChanged = $versionChanged -or (Update-PackageVersion "Mal.Mdk2.ModAnalyzers" $modAnalyzersVersionFile)

# If any package versions were updated, bump this package's version and update release notes
if ($versionChanged) {
    Bump-PackageVersion
} else {
    Write-Host "No changes in package versions. No version bump needed."
}

Write-Host "Package reference updates completed."
