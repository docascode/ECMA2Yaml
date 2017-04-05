param (
    [parameter(mandatory=$true)]
    [hashtable]$ParameterDictionary
)

Function GetLargeJsonContent([string]$jsonFilePath)
{
    try {
        [void][System.Reflection.Assembly]::LoadWithPartialName("System.Web.Extensions")
        $jsonSerializer= New-Object -TypeName System.Web.Script.Serialization.JavaScriptSerializer
        $jsonSerializer.MaxJsonLength  = [System.Int32]::MaxValue

        $jsonContent = Get-Content $jsonFilePath -Raw -Encoding UTF8
        return $jsonSerializer.DeserializeObject($jsonContent)
    }
    catch {
        Write-Callstack
        Write-Error "Invalid JSON file $jsonFilePath. JSON content detail: $jsonContent" -ErrorAction Continue
        throw
    }
}

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path
$ecma2yamlExeName = "ECMA2Yaml.exe"

# Main
$errorActionPreference = 'Stop'

$repositoryRoot = $ParameterDictionary.environment.repositoryRoot
$logFilePath = $ParameterDictionary.environment.logFile
$logOutputFolder = $currentDictionary.environment.logOutputFolder
$cacheFolder = $currentDictionary.environment.cacheFolder
$outputFolder = $currentDictionary.environment.outputFolder

$dependentFileListFilePath = $ParameterDictionary.context.dependentFileListFilePath
$changeListTsvFilePath = $ParameterDictionary.context.changeListTsvFilePath
$userSpecifiedChangeListTsvFilePath = $ParameterDictionary.context.userSpecifiedChangeListTsvFilePath

pushd $repositoryRoot
$publicBranch = 'master'

$publicGitUrl = & git config --get remote.origin.url
if ($publicGitUrl.EndsWith(".git"))
{
	$publicGitUrl = $publicGitUrl.Substring(0, $publicGitUrl.Length - 4)
}
& git branch | foreach {
    if ($_ -match "^\* (.*)") {
        $publicBranch = $matches[1]
    }
}
popd

if (-not [string]::IsNullOrEmpty($ParameterDictionary.environment.publishConfigContent.git_repository_url_open_to_public_contributors))
{
    $publicGitUrl = $ParameterDictionary.environment.publishConfigContent.git_repository_url_open_to_public_contributors
}
if (-not [string]::IsNullOrEmpty($ParameterDictionary.environment.publishConfigContent.git_repository_branch_open_to_public_contributors))
{
    $publicBranch = $ParameterDictionary.environment.publishConfigContent.git_repository_branch_open_to_public_contributors
}
if (-not $publicGitUrl.EndsWith("/"))
{
	$publicGitUrl += "/"
}

$ecmaConfig = $ParameterDictionary.environment.publishConfigContent.ECMA2Yaml

$ecmaXmlGitUrlBase = $publicGitUrl + "blob/" + $publicBranch
echo "Using $ecmaXmlGitUrlBase as url base"
$ecmaSourceXmlFolder = Join-Path $repositoryRoot $ecmaConfig.SourceXmlFolder
$ecmaOutputYamlFolder = Join-Path $repositoryRoot $ecmaConfig.OutputYamlFolder
$allArgs = @("-s", "$ecmaSourceXmlFolder", "-o", "$ecmaOutputYamlFolder", "-l", "$logFilePath", "-p", """$repositoryRoot=>$ecmaXmlGitUrlBase""");
if ($ecmaConfig.Flatten)
{
    $allArgs += "-f";
}
if ($ecmaConfig.StrictMode)
{
    $allArgs += "-strict";
}
if (-not [string]::IsNullOrEmpty($ecmaConfig.SourceMetadataFolder) -and (Test-Path $ecmaConfig.SourceMetadataFolder))
{
	$ecmaSourceMetadataFolder = Join-Path $repositoryRoot $ecmaConfig.SourceMetadataFolder
	$allArgs += "-m";
	$allArgs += "$ecmaSourceMetadataFolder";
}
$printAllArgs = [System.String]::Join(' ', $allArgs)
$ecma2yamlExeFilePath = Join-Path $currentDir $ecma2yamlExeName
echo "Executing $ecma2yamlExeFilePath $printAllArgs" | timestamp
& "$ecma2yamlExeFilePath" $allArgs
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

if (Test-Path $changeListTsvFilePath)
{
    $mappingFile = Join-Path $logOutputFolder "XmlYamlMapping.json"
    $mapping = GetLargeJsonContent($mappingFile)
    $newChangeList = $changeListTsvFilePath -replace "\.tsv$",".mapped.tsv"
	New-Item $newChangeList -type file -force
    $changeList = Import-Csv -Delimiter "`t" -Path $changeListTsvFilePath -Header "Path", "Change"
    Foreach($file in $changeList)
    {
        $path = $file.Path -replace "/","\"
        if ($mapping.$path -ne $null)
        {
            $path = $mapping.$path
        }
		Add-Content $newChangeList ($path + "`t" + $file.Change)
    }
    echo "Saved new changelist to $newChangeList" | timestamp
	$ParameterDictionary.context.changeListTsvFilePath = $newChangeList
}

if (-not [string]::IsNullOrEmpty($userSpecifiedChangeListTsvFilePath))
{
	if (Test-Path $userSpecifiedChangeListTsvFilePath)
	{
		$mappingFile = Join-Path $logOutputFolder "XmlYamlMapping.json"
		$mapping = GetLargeJsonContent($mappingFile)
		$newChangeList = $userSpecifiedChangeListTsvFilePath -replace "\.tsv$",".mapped.tsv"
		New-Item $newChangeList -type file -force
		$changeList = Import-Csv -Delimiter "`t" -Path $userSpecifiedChangeListTsvFilePath -Header "Path", "Change"
		Foreach($file in $changeList)
		{
			$path = $file.Path -replace "/","\"
			if ($mapping.$path -ne $null)
			{
				$path = $mapping.$path
			}
			Add-Content $newChangeList ($path + "`t" + $file.Change)
		}
		echo "Saved new changelist to $newChangeList" | timestamp
		$ParameterDictionary.context.userSpecifiedChangeListTsvFilePath = $newChangeList
	}
}
