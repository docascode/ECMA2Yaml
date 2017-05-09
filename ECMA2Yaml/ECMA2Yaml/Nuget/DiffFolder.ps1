﻿param (
    [parameter(mandatory=$true)]
    [hashtable]$ParameterDictionary
)

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path
$ecma2yamlExeName = "ECMA2Yaml.exe"

# Main
$errorActionPreference = 'Stop'

$repositoryRoot = $ParameterDictionary.environment.repositoryRoot
$logFilePath = $ParameterDictionary.environment.logFile
$changeListPath = $ParameterDictionary.context.changeListTsvFilePath
$cacheFolder = $ParameterDictionary.environment.cacheFolder
$outputFolder = $currentDictionary.environment.outputFolder
$logOutputFolder = $currentDictionary.environment.logOutputFolder

if ([string]::IsNullOrEmpty($changeListPath) -or -not (Test-Path $changeListPath))
{
	Write-Host "Change $changeListPath list not exist, exiting..."
	exit 0
}

$jobs = $ParameterDictionary.environment.publishConfigContent.DiffFolder
if ($jobs -isnot [system.array])
{
    $jobs = @($jobs)
}
foreach($folder in $jobs)
{
	$folderToDiff = Join-Path $repositoryRoot $folder
	$cacheFile = Join-Path $cacheFolder "DiffFolderMD5Cache/$folder/md5Cache.json"
	$changelistBefore = Join-Path $logOutputFolder "ChangeListUpdateLog/$folder/before.tsv"
	$changelistAfter = Join-Path $logOutputFolder "ChangeListUpdateLog/$folder/after.tsv"

	Write-Host "Saving change list to $changelistBefore"
	New-Item -Force $changelistBefore
	copy-item $changeListPath $changelistBefore -Force

    $allArgs = @("-diffFolder", "-changelistFile", "$changeListPath", "-folderToDiff", "$folderToDiff", "-l", "$logFilePath", "-cacheFile", "$cacheFile", "-p", """$repositoryRoot=>""");

    $printAllArgs = [System.String]::Join(' ', $allArgs)
    $ecma2yamlExeFilePath = Join-Path $currentDir $ecma2yamlExeName
    echo "Executing $ecma2yamlExeFilePath $printAllArgs" | timestamp
    & "$ecma2yamlExeFilePath" $allArgs
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }
	Write-Host "Saving change list to $changelistAfter"
	New-Item -Force $changelistAfter
	copy-item $changeListPath $changelistAfter -Force
}
exit 0

