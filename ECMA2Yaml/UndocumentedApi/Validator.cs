using ECMA2Yaml.Models;
using ECMA2Yaml.UndocumentedApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.UndocumentedApi
{
    public class DotnetDocValidator : Validator
    {
        public override HashSet<string> UnderDocStrings => new HashSet<string>() { "tbd", "to be supplied", "add content here", "to be added", "to be added." };
        public override HashSet<string> MissingStrings => new HashSet<string>() { };
        public override int SummaryLengthRequirement => 30;
        public override int ParametersLengthRequirement => 10;
        public override int ReturnsLengthRequirement => 10;
    }

    public class UWPDocValidator : Validator
    {
        public override HashSet<string> UnderDocStrings => new HashSet<string>() { };
        public override HashSet<string> MissingStrings => new HashSet<string>() { "tbd" };
        public override int SummaryLengthRequirement => 1;
        public override int ParametersLengthRequirement => 1;
        public override int ReturnsLengthRequirement => 1;
    }

    public abstract class Validator
    {
        public virtual HashSet<string> UnderDocStrings { get; }
        public virtual HashSet<string> MissingStrings { get; }
        public virtual int SummaryLengthRequirement { get; }
        public virtual int ParametersLengthRequirement { get; }
        public virtual int ReturnsLengthRequirement { get; }

        public Dictionary<FieldType, ValidationResult> ValidateItem(ReflectionItem item)
        {
            return new Dictionary<FieldType, ValidationResult>()
            {
                {FieldType.Summary, ValidateSummary(item) },
                {FieldType.ReturnValue, ValidateReturnValue(item) },
                {FieldType.Parameters, ValidateParameters(item) },
                {FieldType.TypeParameters, ValidateTypeParameters(item) }
            };
        }

        public ValidationResult ValidateSummary(ReflectionItem item)
        {
            return ValidateSimpleString(item.Docs.Summary, SummaryLengthRequirement);
        }

        public ValidationResult ValidateReturnValue(ReflectionItem item)
        {
            if (item.ReturnValueType == null
                || string.IsNullOrEmpty(item.ReturnValueType.Type)
                || item.ReturnValueType.Type == "System.Void"
                || item.ItemType == ItemType.Event)
            {
                return ValidationResult.NA;
            }

            var result = ValidateSimpleString(item.Docs.Returns, ReturnsLengthRequirement);

            // fix bug 84378(https://ceapex.visualstudio.com/web/wi.aspx?pcguid=7d644393-99ad-41c8-ac53-7fa79294c720&id=84378)
            if ((result == ValidationResult.Missing) && (item.ItemType == ItemType.Field || item.ItemType == ItemType.Property))
            {
                return ValidationResult.NA;
            }
            else
            {
                return result;
            }
        }

        public ValidationResult ValidateParameters(ReflectionItem item)
        {
            if (item.Parameters == null || item.Parameters.Count == 0)
            {
                return ValidationResult.NA;
            }
            foreach(var param in item.Parameters)
            {
                if (!item.Docs.Parameters.ContainsKey(param.Name))
                {
                    return ValidationResult.Missing;
                }
                var paramResult = ValidateSimpleString(item.Docs.Parameters[param.Name], ParametersLengthRequirement);
                if (paramResult != ValidationResult.Present)
                {
                    return paramResult;
                }
            }
            return ValidationResult.Present;
        }

        public ValidationResult ValidateTypeParameters(ReflectionItem item)
        {
            if (item.TypeParameters == null || item.TypeParameters.Count == 0)
            {
                return ValidationResult.NA;
            }
            foreach (var param in item.TypeParameters)
            {
                if (!item.Docs.TypeParameters.ContainsKey(param.Name))
                {
                    return ValidationResult.Missing;
                }
                var paramResult = ValidateSimpleString(item.Docs.TypeParameters[param.Name], ParametersLengthRequirement);
                if (paramResult != ValidationResult.Present)
                {
                    return paramResult;
                }
            }
            return ValidationResult.Present;
        }

        private ValidationResult ValidateSimpleString(string str, int lengthRequirement)
        {
            if (IsMissing(str))
            {
                return ValidationResult.Missing;
            }
            if (IsUnderDoc(str, lengthRequirement))
            {
                return ValidationResult.UnderDoc;
            }
            return ValidationResult.Present;
        }

        private bool IsUnderDoc(string str, int lengthRequirement)
        {
            return UnderDocStrings.Contains(str.ToLower()) || str.Length < lengthRequirement;
        }

        private bool IsMissing(string str)
        {
            return string.IsNullOrWhiteSpace(str) || MissingStrings.Contains(str.ToLower());
        }
    }
}
