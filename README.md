# ECMA2Yaml
> A tool to convert ECMAXML to managed reference Yaml model for DocFx to consume.


|            | Build Status  |  Package   |
| ---------- | ------------- | ---------- |
| **master** |[![Build Status](https://dev.azure.com/ceapex/Engineering/_apis/build/status/docascode.ECMA2Yaml?branchName=master)](https://dev.azure.com/ceapex/Engineering/_build?definitionId=1954) |[![Microsoft.DocAsCode.ECMA2Yaml package in docs-build-v2-prod feed in Azure Artifacts](https://docfx.feeds.visualstudio.com/2600ccab-749a-4eeb-8147-15225a64c13f/_apis/public/Packaging/Feeds/becb03e3-6c68-44ab-88e0-b6deb8f55729/Packages/ecb7a791-5e1d-44a3-af3d-1672253b90db/Badge)](https://docfx.visualstudio.com/docfx/_packaging?_a=package&feed=becb03e3-6c68-44ab-88e0-b6deb8f55729&package=ecb7a791-5e1d-44a3-af3d-1672253b90db&preferRelease=true) |
| **develop** |[![Build Status](https://dev.azure.com/ceapex/Engineering/_apis/build/status/docascode.ECMA2Yaml?branchName=develop)](https://dev.azure.com/ceapex/Engineering/_build?definitionId=1954) |

## Usage
```
ECMA2Yaml.exe <Options>
  -s, --source=VALUE         [Required] the folder path containing the ECMAXML
                               files.
  -o, --output=VALUE         [Required] the output folder to put yml files.
  -l, --log=VALUE            the log file path.
  -f, --flatten              to put all ymls in output root and not keep
                               original folder structure.
      --strict               strict mode, means that any unresolved type
                               reference will cause a warning
      --SDP                  SDP mode, generate yamls in the .NET SDP schema
                               format
      --UWP                  UWP mode, special treatment for UWP pipeline
      --demo                 demo mode, only for generating test yamls, do not
                               set --SDP or --UWP together with this option.
      --changeList=VALUE     OPS change list file, ECMA2Yaml will translate xml
                               path to yml path
      --skipPublishFilePath=VALUE
                             Pass a file to OPS to let it know which files
                               should skip publish
      --undocumentedApiReport=VALUE
                             Save the Undocumented API validation result to
                               Excel file
      --publicRepoBranch=VALUE
                             the branch that is public to contributors
      --publicRepoUrl=VALUE  the repo that is public to contributors
      --repoRoot=VALUE       the local path of the root of the repo
      --fallbackRepoRoot=VALUE
                             the local path of the root of the fallback repo
      --repoUrl=VALUE        the url of the current repo being processed
      --repoBranch=VALUE     the branch of the current repo being processed
      --yamlXMLMappingFile=VALUE
                             Mapping from generated yaml files to source XML files
```