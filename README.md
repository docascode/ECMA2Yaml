# ECMA2Yaml
> A tool to convert ECMAXML to managed reference Yaml model for DocFx to consume.


|            | Build Status  |  Package   |
| ---------- | ------------- | ---------- |
| **master** |[![master status](https://ci.appveyor.com/api/projects/status/drvly5p5lc5y99ij/branch/master?svg=true)](https://ci.appveyor.com/project/TianqiZhang/ecma2yaml-xuttd/branch/master) |[![NuGet](https://img.shields.io/nuget/v/Microsoft.DocAsCode.ECMA2Yaml.svg)](http://www.nuget.org/packages/Microsoft.DocAsCode.ECMA2Yaml/) |
|  **develop**   |[![develop status](https://ci.appveyor.com/api/projects/status/drvly5p5lc5y99ij/branch/develop?svg=true)](https://ci.appveyor.com/project/TianqiZhang/ecma2yaml-xuttd/branch/develop) |[![MyGet](https://img.shields.io/myget/op-dev/vpre/Microsoft.DocAsCode.ECMA2Yaml.svg?label=myget)](https://www.myget.org/feed/op-dev/package/nuget/Microsoft.DocAsCode.ECMA2Yaml)

## Usage
```
ECMA2Yaml.exe <Options>
  -s, --source=VALUE         [Required] the folder path containing the ECMAXML files.
  -o, --output=VALUE         [Required] the output folder to put yml files.
  --fs, --fallbackSource=VALUE
                             the folder path containing the fallback ECMAXML files.
  -m, --metadata=VALUE       the folder path containing the overwrite MD files for metadata.
  -l, --log=VALUE            the log file path.
  -f, --flatten              to put all ymls in output root and not keep original folder structure.
  -p, --pathUrlMapping=VALUE1=>VALUE2
                             map local xml path to the Github url.
  --fp, --fallbackPathUrlMapping=VALUE1=>VALUE2
                             map local xml path to the Github url for fallbacks.
  --strict                   strict mode, means that any unresolved type reference will cause a warning
  --changeList=VALUE         OPS change list file, ECMA2Yaml will translate xml path to yml path
  --skipPublishFilePath=VALUE
                             Pass a file to OPS to let it know which files should skip publish
```