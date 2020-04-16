$revision = ""
$commitCount = & { git rev-list --count HEAD }
$version = ''
$repoRoot = $($MyInvocation.MyCommand.Definition) | Split-Path | Split-Path

if ("$env:BUILD_REASON" -eq "PullRequest") {
    $version = "0.0.1-alpha-pr-$env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER"
    Write-Host "##vso[task.setvariable variable=NugetVersionType;]prerelease"
}
elseif (("$env:BUILD_REASON" -eq "Manual" -or "$env:BUILD_REASON" -eq "IndividualCI") `
    -and "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/develop') {
    $version = "1.1.$commitCount-beta$env:BUILD_BUILDNUMBER"
    Write-Host "##vso[task.setvariable variable=NugetVersionType;]prerelease"
}
elseif (("$env:BUILD_REASON" -eq "Manual" -or "$env:BUILD_REASON" -eq "IndividualCI") `
    -and "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/master') {
    $version = "1.1.$commitCount"
    Write-Host "##vso[task.setvariable variable=NugetVersionType;]release"
}
if ($version -ne '') {
    md _nuget
    nuget pack $repoRoot\ECMA2Yaml\ECMA2Yaml\Nuget\ECMA2Yaml.nuspec -outputdirectory _nuget\ECMA2Yaml -version $version -Prop Configuration=Release
    msbuild -t:pack $repoRoot\ECMA2Yaml\ECMAHelper\ECMAHelper.csproj -p:PackageOutputPath=_nuget\ECMAHelper -p:PackageVersion=$version -p:Configuration=Release
    Write-Host "##vso[build.addbuildtag]$version"
    Write-Host "##vso[task.setvariable variable=NeedNugetPush;]Yes"
}