param (
    [parameter(mandatory=$true)]
    [hashtable]$ParameterDictionary
)

$errorActionPreference = 'Stop'

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path
$repositoryRoot = $ParameterDictionary.environment.repositoryRoot
$outputFolder = $currentDictionary.environment.outputFolder

$ecmaConfig = $ParameterDictionary.environment.publishConfigContent.ECMA2Yaml
$ecmaOutputYamlFolder = Join-Path $repositoryRoot $ecmaConfig.OutputYamlFolder
$ecmaOutputMDFolder = Join-Path $ecmaOutputYamlFolder "overwrites"

$ymlOutputFolder = Join-Path $outputFolder "_yml"
$mdOutputFolder = Join-Path $ymlOutputFolder "overwrites"
& robocopy $ecmaOutputYamlFolder $ymlOutputFolder *.yml /s
& robocopy $ecmaOutputMDFolder $mdOutputFolder *.md /s
if ($LASTEXITCODE -ne 1)
{
    exit $LASTEXITCODE
}
exit 0