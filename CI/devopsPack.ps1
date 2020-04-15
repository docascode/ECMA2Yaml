$revision = ""
$commitCount = & { git rev-list --count HEAD }
$revision = ''

if ("$env:BUILD_REASON" -eq "PullRequest" -or "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/develop') {
    $revision = '1.1.$commitCount.$env:BUILD_BUILDNUMBER-beta'
    Write-Host "prerelease"
}
if (("$env:BUILD_REASON" -eq "Manual" -or "$env:BUILD_REASON" -eq "IndividualCI") `
    -and "$env:BUILD_SOURCEBRANCH" -eq 'refs/heads/devops') {
    $revision = '1.1.$commitCount.$env:BUILD_BUILDNUMBER'
    Write-Host "release"
}
if ($revision -ne '') {
    md _nuget
    nuget pack ..\ECMA2Yaml\ECMA2Yaml\Nuget\ECMA2Yaml.nuspec -outputdirectory _nuget -version $revision -Prop Configuration=Release
    nuget pack ..\ECMA2Yaml\ECMAHelper\ECMAHelper.csproj -outputdirectory _nuget -version $revision -Prop Configuration=Release
}