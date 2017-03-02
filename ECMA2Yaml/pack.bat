REM call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsMSBuildCmd.bat"
REM msbuild ECMA2Yaml.sln /P:Config=Release /t:Clean,Build
md _nuget
nuget pack ECMA2Yaml\Nuget\ECMA2Yaml.nuspec -outputdirectory _nuget -version %1
nuget pack ECMAHelper\ECMAHelper.csproj -outputdirectory _nuget -version %1