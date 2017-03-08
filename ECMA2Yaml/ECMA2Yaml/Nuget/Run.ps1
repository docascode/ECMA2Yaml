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

# $changeList = Import-Csv -Delimiter "`t" -Path $changeListTsvFilePath -Header "FileName", "Change"