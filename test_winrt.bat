::====================================New 
set currentDirset currentDir=%~dp0
set newIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\winrt\New
set orderNewIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\winrt\NewOrder
set gitComparePath=G:\ECMA2Yaml-output\GenerateIntellisense\winrt\Compare
set xmlPath=G:\SourceCode\DevCode\winrt-api-build-ppe\winrt-api-build\xml
set repoPath=G:\SourceCode\DevCode\winrt-api-build

:: Clear output dir and generate xml
rd /s/q %newIntellisenseDir%
"./ECMA2Yaml/IntellisenseFileGen/bin/Debug/IntellisenseFileGen.exe" -x  %xmlPath% -d  %repoPath%  -o %newIntellisenseDir%

:: Clear order output dir and order xml
rd /s/q %orderNewIntellisenseDir%
"./ECMA2Yaml/DiffXML/bin/Debug/DiffXML.exe" -i %newIntellisenseDir% -o %orderNewIntellisenseDir%

:: Copy to git path
ROBOCOPY %orderNewIntellisenseDir% %gitComparePath% /e /MT:16
