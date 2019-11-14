set currentDir=%~dp0
"./ECMA2Yaml/IntellisenseFileGen/bin/Release/IntellisenseFileGen.exe" -x %currentDir%test-intellisense\xml -d %currentDir%test-intellisense -o %currentDir%test-intellisense\intellisense_After
"./ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o %currentDir%test-intellisense\intellisense -n %currentDir%test-intellisense\intellisense_After -l %currentDir%test-intellisense --Path