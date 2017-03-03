using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public enum SyntaxLanguage
    {
        Default = 0,
        CSharp = 1,
        CPlusPlus = 2,
        FSharp = 3,
        Javascript = 4,
        VB = 5
    }

    public class Member : ReflectionItem
    {
        public string DisplayName { get; set; }
        public string FullDisplayName { get; set; }
        public Dictionary<string, string> Signatures { get; set; }
        public List<AssemblyInfo> AssemblyInfo { get; set; }
        public List<Parameter> TypeParameters { get; set; }
        public List<Parameter> Parameters { get; set; }
        public List<string> Attributes { get; set; }
        public Parameter ReturnValueType { get; set; }
        public Docs Docs { get; set; }
        public string Overload { get; set; }

        public void BuildName(ECMAStore store)
        {
            DisplayName = ItemType == ItemType.Constructor ? Parent.Name : Name;
            if (DisplayName.Contains('.')) //EII
            {
                var typeStr = DisplayName.Substring(0, DisplayName.LastIndexOf('.'));
                var memberStr = DisplayName.Substring(DisplayName.LastIndexOf('.') + 1);
                DisplayName = typeStr.ToDisplayName() + '.' + memberStr;
            }
            if (Parameters?.Count > 0)
            {
                DisplayName += string.Format("({0})", string.Join(",", Parameters.Select(p => p.Type.ToDisplayName())));
            }
            else if (ItemType == ItemType.Method || ItemType == ItemType.Constructor)
            {
                DisplayName += "()";
            }
            FullDisplayName = ((Type)Parent).FullName + "." + DisplayName;
        }

        //The ID of a generic method uses postfix ``n, n is the count of in method parameters, for example, System.Tuple.Create``1(``0)
        public override void BuildId(ECMAStore store)
        {
            Id = Name.Replace('.', '#');
            if (TypeParameters?.Count > 0)
            {
                Id = Id.Substring(0, Id.LastIndexOf('<')) + "``" + TypeParameters.Count;
            }
            //handle eii prefix
            Id = Id.Replace('<', '{').Replace('>', '}');
            if (Parameters?.Count > 0)
            {
                //Type conversion operator can be considered a special operator whose name is the UID of the target type,
                //with one parameter of the source type.
                //For example, an operator that converts from string to int should be Explicit(System.String to System.Int32).
                if (Name == "op_Explicit")
                {
                    Id += string.Format("({0} to {1})", Parameters.First().Type, ReturnValueType.Type);
                }
                //spec is wrong, no need to treat indexer specially, so comment this part out
                //else if (MemberType == MemberType.Property && Signatures.ContainsKey("C#") && Signatures["C#"].Contains("["))
                //{
                //    Id += string.Format("[{0}]", string.Join(",", GetParameterUids(store)));
                //}
                else
                {
                    Id += string.Format("({0})", string.Join(",", GetParameterUids(store)));
                }
            }
        }

        private List<string> GetParameterUids(ECMAStore store)
        {
            List<string> ids = new List<string>();
            foreach (var p in Parameters)
            {
                var paraUid = p.Type.Replace('+', '.').Replace('<', '{').Replace('>', '}');
                if (p.RefType == "ref" || p.RefType == "out")
                {
                    paraUid += "@";
                }
                var parent = (Type)Parent;
                paraUid = ReplaceGenericInParameterUid(parent.TypeParameters, "`", paraUid);
                paraUid = ReplaceGenericInParameterUid(TypeParameters, "``", paraUid);
                ids.Add(paraUid);
            }

            return ids;
        }

        //Example:System.Collections.Generic.Dictionary`2.#ctor(System.Collections.Generic.IDictionary{`0,`1},System.Collections.Generic.IEqualityComparer{`0})
        private static Dictionary<string, Regex> TypeParameterRegexes = new Dictionary<string, Regex>();
        private string ReplaceGenericInParameterUid(List<Parameter> typeParameters, string prefix, string paraUid)
        {
            if (typeParameters?.Count > 0)
            {
                int genericCount = 0;
                foreach (var tp in typeParameters)
                {
                    string genericPara = prefix + genericCount;
                    if (tp.Name == paraUid)
                    {
                        return genericPara;
                    }

                    Regex regex = null;
                    if (TypeParameterRegexes.ContainsKey(tp.Name))
                    {
                        regex = TypeParameterRegexes[tp.Name];
                    }
                    else
                    {
                        regex = new Regex("[^\\w]?" + tp.Name + "[^\\w]", RegexOptions.Compiled);
                        TypeParameterRegexes[tp.Name] = regex;
                    }
                    paraUid = regex.Replace(paraUid, match => match.Value.Replace(tp.Name, genericPara));
                    genericCount++;
                }
            }
            return paraUid;
        }
    }
}
