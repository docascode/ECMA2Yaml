# Managed Reference Provisioning Job (NuGet)

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function DownloadAndUnzip
{
    param([string]$url, [string]$downloadPath, [string]$unzipPath)

    if (Test-Path $downloadPath)
    {
        Remove-Item $downloadPath
    }
    Invoke-WebRequest -Uri $url -OutFile $downloadPath
    Expand-Archive -Path $downloadPath -DestinationPath $unzipPath -Force
}

$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

# Download URLs
$mdocUrl = "https://github.com/mono/api-doc-tools/releases/download/mdoc-5.7.4.12/mdoc-5.7.4.12.zip"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

pushd $scriptPath

# Folder provisioning
New-Item _bin -Type Directory -Force           # Tool download folder

# Download Paths
$binPath = Join-Path $scriptPath "\_bin"
$mdocOutput = Join-Path $scriptPath "\_bin\mdoc.zip"
$nugetOutput = Join-Path $scriptPath "\_bin\nuget.exe"
$mdocPath = Join-Path $binPath "mdoc.exe"

# Download Triggers
if (-not (Test-Path $mdocPath)) {
    DownloadAndUnzip $mdocUrl $mdocOutput $binPath
}
if (-not (Test-Path $nugetOutput)) {
    Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetOutput
}

# Test Library Building
pushd CatLibraryV1
& $msbuildPath CatLibrary.sln /p:Configuration=Release
popd
pushd CatLibraryV2
& $msbuildPath CatLibrary.sln /p:Configuration=Release
popd

New-Item frameworks\CatLibrary\cat-1.0 -Type Directory -Force
New-Item frameworks\CatLibrary\cat-2.0 -Type Directory -Force
Copy-Item "CatLibraryV1\CatLibrary\bin\Release\*" -Destination "frameworks\CatLibrary\cat-1.0\" -Recurse -Force -Container
Copy-Item "CatLibraryV2\CatLibrary\bin\Release\*" -Destination "frameworks\CatLibrary\cat-2.0\" -Recurse -Force -Container

& $mdocPath update -o xml -fx frameworks\CatLibrary -lang docid -lang vb.net -lang c++/cli -lang fsharp --delete

popd
