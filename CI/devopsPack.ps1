$revision = ""
$commitCount = & { git rev-list --count HEAD }
$revision = ''

if ("$(Build.Reason)" -eq "PullRequest" -or "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/develop') {
    $revision = '1.1.$commitCount.$(Build.BuildNumber)-beta'
    Write-Host "prerelease"
}
if (("$(Build.Reason)" -eq "Manual" -or "$(Build.Reason)" -eq "IndividualCI") `
    -and "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/devops') {
    $revision = '1.1.$commitCount.$(Build.BuildNumber)'
    Write-Host "release"
}
if ($revision -ne '') {
    md _nuget
    nuget pack ..\ECMA2Yaml\ECMA2Yaml\Nuget\ECMA2Yaml.nuspec -outputdirectory _nuget -version $revision -Prop Configuration=Release
    nuget pack ..\ECMA2Yaml\ECMAHelper\ECMAHelper.csproj -outputdirectory _nuget -version $revision -Prop Configuration=Release
}