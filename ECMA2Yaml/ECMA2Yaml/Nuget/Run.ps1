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

Function TranslateChangeList([string]$changeListFile, $mapping)
{
    if (-not [string]::IsNullOrEmpty($changeListFile))
    {
        if (Test-Path $changeListFile)
        {
            $newChangeList = $changeListFile -replace "\.tsv$",".mapped.tsv"
            New-Item $newChangeList -type file -force | Out-Null
            $changeList = Import-Csv -Delimiter "`t" -Path $changeListFile -Header "Path", "Change"
            Foreach($file in $changeList)
            {
                $path = $file.Path -replace "/","\"
                if ($mapping.$path -ne $null)
                {
                    $path = $mapping.$path
                }
                Add-Content $newChangeList ($path + "`t" + $file.Change)
            }
            Write-Host "Saved new changelist to $newChangeList"
            return $newChangeList
        }
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

$jobs = $ParameterDictionary.environment.publishConfigContent.ECMA2Yaml
if ($jobs -isnot [system.array])
{
    $jobs = @($jobs)
}
foreach($ecmaConfig in $jobs)
{
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
    if (-not [string]::IsNullOrEmpty($ecmaConfig.SourceMetadataFolder))
    {
        $ecmaSourceMetadataFolder = Join-Path $repositoryRoot $ecmaConfig.SourceMetadataFolder
		if (Test-Path $ecmaSourceMetadataFolder)
		{
			$allArgs += "-m";
            $allArgs += "$ecmaSourceMetadataFolder";
		}
    }
    $printAllArgs = [System.String]::Join(' ', $allArgs)
    $ecma2yamlExeFilePath = Join-Path $currentDir $ecma2yamlExeName
    echo "Executing $ecma2yamlExeFilePath $printAllArgs" | timestamp
    & "$ecma2yamlExeFilePath" $allArgs
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

    if (-not [string]::IsNullOrEmpty($ecmaConfig.id))
    {
        $tocPath = Join-Path $ecmaOutputYamlFolder "toc.yml"
        $newTocPath = Join-Path $ecmaOutputYamlFolder $ecmaConfig.id
        if (-not (Test-Path $newTocPath))
        {
            New-Item -ItemType Directory -Force -Path $newTocPath
        }
        Move-Item $tocPath $newTocPath
    }

    $mappingFile = Join-Path $logOutputFolder "XmlYamlMapping.json"
    $mapping = GetLargeJsonContent($mappingFile)
    $newChangeList = TranslateChangeList ($ParameterDictionary.context.changeListTsvFilePath) ($mapping)
    if (-not [string]::IsNullOrEmpty($newChangeList))
    {
        $ParameterDictionary.context.changeListTsvFilePath = $newChangeList
    }
    $newChangeList = TranslateChangeList ($ParameterDictionary.context.userSpecifiedChangeListTsvFilePath) ($mapping)
    if (-not [string]::IsNullOrEmpty($newChangeList))
    {
        $ParameterDictionary.context.userSpecifiedChangeListTsvFilePath = $newChangeList
    }
}