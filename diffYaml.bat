"./ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s ./test/xml -o ./test/yml_MREF_After -f --repoRoot ./ --publicBranch master --publicRepoUrl https://github.com/docascode/ECMA2Yaml
"./ECMA2Yaml/ECMA2Yaml/bin/Release/ECMA2Yaml.exe" -s ./test/xml -o ./test/yml_SDP_After --SDP -f --repoRoot ./ --publicBranch master --publicRepoUrl https://github.com/docascode/ECMA2Yaml
"./ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o ./test/yml_MREF -n ./test/yml_MREF_After -l ./test --Path
IF NOT %ErrorLevel% == 0 ECHO Exist different places for MREF yml
"./ECMA2Yaml/DiffFiles/bin/Release/DiffFiles.exe" -o ./test/yml_SDP -n ./test/yml_SDP_After -l ./test --Path
IF NOT %ErrorLevel% == 0 ECHO Exist different places for SDP yml