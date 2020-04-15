$revision = ""
$commitCount = & { git rev-list --count HEAD }
$version = ''
$repoRoot = $($MyInvocation.MyCommand.Definition) | Split-Path | Split-Path

if ("$env:BUILD_REASON" -eq "PullRequest" -or "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/develop') {
    $version = '1.1.$commitCount.$env:BUILD_BUILDNUMBER-beta'
    Write-Host "prerelease"
}
if (("$env:BUILD_REASON" -eq "Manual" -or "$env:BUILD_REASON" -eq "IndividualCI") `
    -and "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/devops') {
    $version = '1.1.$commitCount.$env:BUILD_BUILDNUMBER'
    Write-Host "release"
}
if ($revision -ne '') {
    md _nuget
    nuget pack $repoRoot\ECMA2Yaml\ECMA2Yaml\Nuget\ECMA2Yaml.nuspec -outputdirectory _nuget -version $version -Prop Configuration=Release
    nuget pack $repoRoot\ECMA2Yaml\ECMAHelper\ECMAHelper.csproj -outputdirectory _nuget -version $version -Prop Configuration=Release
    Write-Host "##vso[build.addbuildtag]$version"
    Write-Host "##vso[task.setvariable variable=BuildVersionNumber;]$version"
}