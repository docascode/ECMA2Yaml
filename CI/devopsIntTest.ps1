$ErrorActionPreference = "Stop"

Write-Host "Start integration test..."

$currentDir = $($MyInvocation.MyCommand.Definition) | Split-Path | Split-Path

& "$currentDir/ECMA2Yaml/IntellisenseFileGen/bin/Release/IntellisenseFileGen.exe" -x $currentDir\test\xml -d $currentDir\test -o $currentDir\test\intellisense_After
& "$currentDir/ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o $currentDir\test\intellisense -n $currentDir\test\intellisense_After -l $currentDir\test --Path
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Diff found for intellisense."
}
else {
    Write-Host "Done testing intellisense..."
}

& "$currentDir/ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s $currentDir\test\xml -o $currentDir\test\yml_MREF_After -f --repoRoot $currentDir\\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml
& "$currentDir/ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o $currentDir\test\yml_MREF -n $currentDir\test\yml_MREF_After -l $currentDir\test --Path
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Diff found for MREF yml."
}
else {
    Write-Host "Done testing MREF yml..."
}

& "$currentDir/ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s $currentDir\test\xml -o $currentDir\test\yml_SDP_After --SDP -f --repoRoot $currentDir\\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml
& "$currentDir/ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o $currentDir\test\yml_SDP -n $currentDir\test\yml_SDP_After -l $currentDir\test --Path
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Diff found for SDP yml."
}
else {
    Write-Host "Done testing SDP yml..."
}

Write-Host "Finished integration test..."
