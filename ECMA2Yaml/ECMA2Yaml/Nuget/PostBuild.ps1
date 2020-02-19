param (
    [parameter(mandatory=$true)]
    [hashtable]$ParameterDictionary
)

$errorActionPreference = 'Stop'

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path
$postBuildExeFilePath = Join-Path $currentDir "ECMA2Yaml_PostBuild.exe"
$repositoryRoot = $ParameterDictionary.environment.repositoryRoot
$outputFolder = $currentDictionary.environment.outputFolder
$dependencyFilePath = $ParameterDictionary.environment.fullDependentListFilePath
$docsetName = $ParameterDictionary.docset.docsetInfo.docset_name
$jobs = $ParameterDictionary.docset.docsetInfo.ECMA2Yaml
if (!$jobs)
{
	$jobs = $ParameterDictionary.environment.publishConfigContent.ECMA2Yaml
}
if ($jobs -isnot [system.array])
{
    $jobs = @($jobs)
}
foreach($ecmaConfig in $jobs)
{
    $runId = $ecmaConfig.id
    if (-not $runId)
    {
        $runId = $docsetName
    }

    $allArgs = @()
	$xmlYamlMappingFile = Join-Path $outputFolder "XMLYamlMapping_${runId}.json"
	$allArgs += "--xmlYamlMappingFile"
	$allArgs += "$xmlYamlMappingFile"

	$allArgs += "--fullDependencyFile"
	$allArgs += "$dependencyFilePath"

	$printAllArgs = [System.String]::Join(' ', $allArgs)
    echo "Executing $postBuildExeFilePath $printAllArgs" | timestamp
    & "$postBuildExeFilePath" $allArgs
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

	$ecmaOutputYamlFolder = Join-Path $repositoryRoot $ecmaConfig.OutputYamlFolder
	$ymlOutputFolder = Join-Path $outputFolder "_yml"
	& robocopy $ecmaOutputYamlFolder $ymlOutputFolder *.yml /s /np /nfl /ndl

	$ecmaOutputMDFolder = Join-Path $ecmaOutputYamlFolder "overwrites"
	if (Test-Path $ecmaOutputMDFolder) 
	{
	    $mdOutputFolder = Join-Path $ymlOutputFolder "overwrites"
	    & robocopy $ecmaOutputMDFolder $mdOutputFolder *.md /s /np /nfl /ndl
	}
}
exit 0