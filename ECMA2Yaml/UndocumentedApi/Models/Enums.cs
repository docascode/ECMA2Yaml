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
