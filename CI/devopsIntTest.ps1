$ErrorActionPreference = "Stop"

Write-Host "Start integration test..."

$repoRoot = $($MyInvocation.MyCommand.Definition) | Split-Path | Split-Path

& "$repoRoot/ECMA2Yaml/IntellisenseFileGen/bin/Release/IntellisenseFileGen.exe" -x $repoRoot\test\xml -d $repoRoot\test -o $repoRoot\test\_intellisense_After
& "$repoRoot/ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o $repoRoot\test\intellisense -n $repoRoot\test\_intellisense_After -l $repoRoot\test --Path
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Diff found for intellisense."
}
else {
    Write-Host "Done testing intellisense..."
}

& "$repoRoot/ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s $repoRoot\test\xml -o $repoRoot\test\_yml_SDP_After --SDP -f --repoRoot $repoRoot\\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml
& "$repoRoot/ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o $repoRoot\test\yml_SDP -n $repoRoot\test\_yml_SDP_After -l $repoRoot\test --Path
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Diff found for SDP yml."
}
else {
    Write-Host "Done testing SDP yml..."
}

& "$repoRoot/ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s $repoRoot\test\xml -o $repoRoot\test\_yml_UWP_SDP_After --UWP --SDP -f --repoRoot $repoRoot\\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml
& "$repoRoot/ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o $repoRoot\test\yml_UWP_SDP -n $repoRoot\test\_yml_UWP_SDP_After -l $repoRoot\test --Path
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Diff found for UWP SDP yml."
}
else {
    Write-Host "Done testing UWP SDP yml..."
}

Write-Host "Finished integration test..."
