param (
    [parameter(mandatory=$true)]
    [hashtable]$ParameterDictionary
)

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path
$ecma2yamlExeName = "ECMA2Yaml.exe"

# Main
$errorActionPreference = 'Stop'

$logFilePath = $ParameterDictionary.environment.logFile

$jobs = $ParameterDictionary.environment.publishConfigContent.JoinTOCPlugin
if ($jobs -isnot [system.array])
{
    $jobs = @($jobs)
}
foreach($JoinTOCConfig in $jobs)
{
	$topTOC = Join-Path $repositoryRoot $JoinTOCConfig.TopLevelTOC
	$refTOC = Join-Path $repositoryRoot $JoinTOCConfig.ReferenceTOC

	$conceptualTOC = Join-Path $repositoryRoot $JoinTOCConfig.ConceptualTOC
	$refTOCUrl = $JoinTOCConfig.ReferenceTOCUrl
	$conceptualTOCUrl = $JoinTOCConfig.ConceptualTOCUrl
	$landingPageFolder = $JoinTOCConfig.OutputFolder
    $allArgs = @("-joinTOC", "-topLevelTOC", "$topTOC", "-refTOC", "$refTOC", "-l", "$logFilePath");

	if (-not [string]::IsNullOrEmpty($conceptualTOC) -and (Test-Path $conceptualTOC))
    {
        $allArgs += "-conceptualTOC";
        $allArgs += "$conceptualTOC";
    }

	if (-not [string]::IsNullOrEmpty($refTOCUrl))
    {
        $allArgs += "-refTOCUrl";
        $allArgs += "$refTOCUrl";
    }

	if (-not [string]::IsNullOrEmpty($conceptualTOCUrl))
    {
        $allArgs += "-conceptualTOCUrl";
        $allArgs += "$conceptualTOCUrl";
    }

	if ($JoinTOCConfig.HideEmptyNode)
	{
		$allArgs += "-hideEmptyNode";
	}

	if ($JoinTOCConfig.ContainerPageMetadata)
	{
		$allArgs += "-landingPageMetadata";
		$meta = $JoinTOCConfig.ContainerPageMetadata;
		$json = ConvertTo-Json $meta -Compress
		$json = $json -replace """","\"""
		$allArgs += "$json";
	}

	if (-not [string]::IsNullOrEmpty($landingPageFolder))
    {
		$landingPageFolder = Join-Path $repositoryRoot $landingPageFolder
        $allArgs += "-o";
        $allArgs += "$landingPageFolder";
    }

    $printAllArgs = [System.String]::Join(' ', $allArgs)
    $ecma2yamlExeFilePath = Join-Path $currentDir $ecma2yamlExeName
    echo "Executing $ecma2yamlExeFilePath $printAllArgs" | timestamp
    & "$ecma2yamlExeFilePath" $allArgs
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

	if ($JoinTOCConfig.Debug)
	{
		$outputFolder = $currentDictionary.environment.outputFolder

		$fusionTOCOutputFolder = Join-Path $outputFolder "_fusionTOC"

		if (-not [string]::IsNullOrEmpty($landingPageFolder))
		{
			& robocopy $landingPageFolder $fusionTOCOutputFolder *.yml
		}
		
		& copy $refTOC "$fusionTOCOutputFolder/ref_toc.yml"
		& copy $conceptualTOC "$fusionTOCOutputFolder/conceptual_toc.yml"
		if ($LASTEXITCODE -ne 1)
		{
			exit $LASTEXITCODE
		}
	}

	exit 0
}

