$ErrorActionPreference = "Stop"

Write-Host "Start generating yamls for test repo..."

$repoRoot = $($MyInvocation.MyCommand.Definition) | Split-Path | Split-Path

& "$repoRoot/ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s $repoRoot\test\xml -o $repoRoot\test\yml_demo --demo -f --repoRoot $repoRoot\\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml

Remove-Item "$repoRoot\test\yml_demo" -Filter VersioningTest* -Force -ErrorAction Ignore -Recurse

Write-Host "Finished. Yamls generated in $repoRoot\test\yml_demo"
