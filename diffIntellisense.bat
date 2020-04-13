set currentDir=%~dp0
"./ECMA2Yaml/IntellisenseFileGen/bin/Release/IntellisenseFileGen.exe" -x %currentDir%test\xml -d %currentDir%test -o %currentDir%test\_intellisense_After
"./ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o %currentDir%test\intellisense -n %currentDir%test\_intellisense_After -l %currentDir%test --Path