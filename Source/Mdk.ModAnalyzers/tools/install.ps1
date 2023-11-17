param($installPath, $toolsPath, $package, $project)

$analyzersPath = join-path $installPath "analyzers\dotnet\cs"

# Install the language agnostic analyzers.
foreach ($analyzerFilePath in Get-ChildItem $analyzersPath -Filter *.dll)
{
    if ($project.Object.AnalyzerReferences)
    {
        $project.Object.AnalyzerReferences.Add($analyzerFilePath.FullName)
    }
}
