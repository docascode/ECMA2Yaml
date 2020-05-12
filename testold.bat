

::=========================== old
set gitComparePath=G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\order_intellisense
set oldIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\old_intellisense
set orderOldIntellisenseDir=G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\order_old_intellisense
rd /s/q G:\ECMA2Yaml-output\GenerateIntellisense\dotnet\order_old_intellisense
"./ECMA2Yaml/DiffXML/bin/Debug/DiffXML.exe" -i %oldIntellisenseDir% -o %orderOldIntellisenseDir%
:: Copy to git path
ROBOCOPY %orderOldIntellisenseDir% %gitComparePath% /e /MT:16
