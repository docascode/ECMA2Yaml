::====================================New 
set currentDirset currentDir=%~dp0
set newIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\New
set orderNewIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\Order_New
set gitComparePath=G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\Compare

:: Clear output dir and generate xml
rd /s/q %newIntellisenseDir%
"./ECMA2Yaml/IntellisenseFileGen/bin/Debug/IntellisenseFileGen.exe" -x G:\SourceCode\DevCode\dotnet-api-docs\xml -d G:\SourceCode\DevCode\dotnet-api-docs -o %newIntellisenseDir%

:: Clear order output dir and order xml
rd /s/q %orderNewIntellisenseDir%
"./ECMA2Yaml/DiffXML/bin/Debug/DiffXML.exe" -i %newIntellisenseDir% -o %orderNewIntellisenseDir%

:: Copy to git path
ROBOCOPY %orderNewIntellisenseDir% %gitComparePath% /e /MT:16





::=========================== old
::set oldIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\old_intellisense
::set orderOldIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\order_old_intellisense
::rd /s/q G:\ECMA2Yaml-output\GenerateIntellisense\order_old_intellisense
::"./ECMA2Yaml/DiffXML/bin/Debug/DiffXML.exe" -i %oldIntellisenseDir% -o %orderOldIntellisenseDir%
:::: Copy to git path
::ROBOCOPY %orderOldIntellisenseDir% %gitComparePath% /e /MT:16
