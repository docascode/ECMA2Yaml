using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.UndocumentedApi.Models
{
    public enum FieldType
    {
        Summary,
        ReturnValue,
        Parameters,
        TypeParameters,
        Remarks,
        Example,
        Exeptions
    }

    public enum ValidationResult
    {
        NA,
        Present,
        Missing,
        UnderDoc
    }
}
