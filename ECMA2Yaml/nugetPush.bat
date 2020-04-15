IF "%APPVEYOR_REPO_BRANCH%"=="master" (
    IF NOT DEFINED APPVEYOR_PULL_REQUEST_NUMBER (
        set NeedPublishNuget=1
    )
)
IF "%APPVEYOR_REPO_BRANCH%"=="develop" (
    set NeedPublishNuget=1
)
IF "%NeedPublishNuget%"=="1" (
    nuget push ./_nuget/Microsoft.DocAsCode.ECMA2Yaml.%APPVEYOR_BUILD_VERSION%.nupkg %opFeedKey% -Source %MYGETFEED%
    nuget push ./_nuget/Microsoft.DocAsCode.ECMAHelper.%APPVEYOR_BUILD_VERSION%.nupkg %opFeedKey% -Source %MYGETFEED%
    REM nuget push ./_nuget/Microsoft.DocAsCode.ECMA2Yaml.%APPVEYOR_BUILD_VERSION%.nupkg %nugetKey% -Source %nugetUrl%
    REM nuget push ./_nuget/Microsoft.DocAsCode.ECMAHelper.%APPVEYOR_BUILD_VERSION%.nupkg %nugetKey% -Source %nugetUrl%
    nuget sources add -name docsNugetSource -source %docsNugetFeed% -username anyuser -password %docsNugetKey%
    nuget push ./_nuget/Microsoft.DocAsCode.ECMA2Yaml.%APPVEYOR_BUILD_VERSION%.nupkg -Source %docsNugetFeed% -ApiKey any_string
    nuget push ./_nuget/Microsoft.DocAsCode.ECMAHelper.%APPVEYOR_BUILD_VERSION%.nupkg -Source %docsNugetFeed% -ApiKey any_string
)
