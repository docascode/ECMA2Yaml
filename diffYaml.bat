set currentDir=%~dp0
"./ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s %currentDir%test\xml -o %currentDir%test\_yml_MREF_After -f --repoRoot %currentDir%\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml
"./ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s %currentDir%test\xml -o %currentDir%test\_yml_SDP_After --SDP -f --repoRoot %currentDir%\ --repoBranch master --repoUrl https://github.com/docascode/ECMA2Yaml
"./ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o %currentDir%test\yml_MREF -n %currentDir%test\_yml_MREF_After -l %currentDir%test --Path
IF NOT %ErrorLevel% == 0 ECHO Exist different places for MREF yml
"./ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o %currentDir%test\yml_SDP -n %currentDir%test\_yml_SDP_After -l %currentDir%test --Path
IF NOT %ErrorLevel% == 0 ECHO Exist different places for SDP yml