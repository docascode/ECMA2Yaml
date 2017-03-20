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

$dependentFileListFilePath = $ParameterDictionary.context.dependentFileListFilePath
$changeListTsvFilePath = $ParameterDictionary.context.changeListTsvFilePath
$userSpecifiedChangeListTsvFilePath = $ParameterDictionary.context.userSpecifiedChangeListTsvFilePath

pushd $repositoryRoot
$currentBranch = 'master'
git branch | foreach {
    if ($_ -match "^\* (.*)") {
        $currentBranch = $matches[1]
    }
}
popd

$ecmaConfig = $ParameterDictionary.environment.publishConfigContent.ECMA2Yaml
$ecmaXmlGitUrlBase = $ecmaConfig.RepoUrl + "blob/" + $currentBranch
echo "Using $ecmaXmlGitUrlBase as url base"
$ecmaSourceXmlFolder = Join-Path $repositoryRoot $ecmaConfig.SourceXmlFolder
$ecmaOutputYamlFolder = Join-Path $repositoryRoot $ecmaConfig.OutputYamlFolder
$allArgs = @("-s", "$ecmaSourceXmlFolder", "-o", "$ecmaOutputYamlFolder", "-l", "$logFilePath", "-p", """$repositoryRoot=>$ecmaXmlGitUrlBase""");
if ($ecmaConfig.Flatten)
{
    $allArgs += "-f";
}
if (-not [string]::IsNullOrEmpty($ecmaConfig.SourceMetadataFolder))
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

echo "Executing docfx merge command" | timestamp
$docfxConfigFile = $ParameterDictionary.docset.docfxConfigFile
$docfxConfigFolder = (Get-Item $docfxConfigFile).DirectoryName
$docfxConfig = $ParameterDictionary.docset.docsetInfo
if ($docfxConfig["merge"] -ne $null)
{
	pushd $docfxConfigFolder
    $docfxExe = Join-Path $parameterDictionary.environment.packages["docfx.console"].packageRootFolder "tools/docfx.exe"
    & $docfxExe merge
    if ($LASTEXITCODE -ne 0)
    {
		popd
        exit $LASTEXITCODE
    }
	popd
}
else
{
    echo "Can't find merge config in $docfxConfigFile, merging skipped." | timestamp
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
