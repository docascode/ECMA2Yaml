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

$ymlOutputFolder = Join-Path $outputFolder "_yml"
& robocopy $ecmaOutputYamlFolder $ymlOutputFolder *.yml *.md /s
if ($LASTEXITCODE -ne 1)
{
    exit $LASTEXITCODE
}
exit 0