param (
    [parameter(mandatory=$true)]
    [hashtable]$ParameterDictionary
)

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path
$ecma2yamlExeName = "ECMA2Yaml.exe"

# Main
$errorActionPreference = 'Stop'
$source = $($MyInvocation.MyCommand.Definition)

$repositoryRoot = $ParameterDictionary.environment.repositoryRoot
$logLevel = $ParameterDictionary.environment.logLevel
$logFilePath = $ParameterDictionary.environment.logFile
$buildOutputSubfolder = $ParameterDictionary.docset.buildOutputSubfolder
$logOutputFolder = $currentDictionary.environment.logOutputFolder
$dependencyPackages = $ParameterDictionary.environment.packages

$dependentFileListFilePath = $ParameterDictionary.context.dependentFileListFilePath
$changeListTsvFilePath = $ParameterDictionary.context.changeListTsvFilePath
$userSpecifiedChangeListTsvFilePath = $ParameterDictionary.context.userSpecifiedChangeListTsvFilePath

$currentBranch = ''
git branch | foreach {
    if ($_ -match "^\* (.*)") {
        $currentBranch += $matches[1]
    }
}

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
    $mapping = Get-Content $mappingFile | ConvertFrom-Json
    $newChangeList = $changeListTsvFilePath -replace "\.tsv$",".mapped.tsv"
    $stringBuilder = New-Object System.Text.StringBuilder
    $changeList = Import-Csv -Delimiter "`t" -Path $changeListTsvFilePath -Header "Path", "Change"
    Foreach($file in $changeList)
    {
        $path = $file.Path -replace "/","\"
        if ($mapping.$path -ne $null)
        {
            $path = $mapping.$path
        }
        $stringBuilder.AppendLine($path + "`t" + $file.Change)
    }
    $stringBuilder.ToString() | Set-Content $newChangeList
    echo "Saved new changelist to $newChangeList" | timestamp
	$ParameterDictionary.context.changeListTsvFilePath = $newChangeList
}