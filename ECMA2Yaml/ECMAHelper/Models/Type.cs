using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class Type : ReflectionItem
    {
        public string FullName { get; set; }
        public List<BaseType> BaseTypes { get; set; }
        public List<VersionedCollection<string>> InheritanceChains { get; set; }
        public TypeForwardingChain TypeForwardingChain { get; set; }
        public Dictionary<string, VersionedString> InheritedMembers { get; set; }
        public List<string> IsA { get; set; }
        public List<VersionedString> Interfaces { get; set; }
        public List<Member> Members { get; set; }
        public List<Member> Overloads { get; set; }
        public List<VersionedString> ExtensionMethods { get; set; }
        private static Regex GenericRegex = new Regex("<[^<>]+>", RegexOptions.Compiled);

        public override void Build(ECMAStore store)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Name.Replace('+', '.');
                if (Id.Contains('<'))
                {
                    Id = GenericRegex.Replace(Id, match => "`" + (match.Value.Count(c => c == ',') + 1));
                }
            }
        }

        public Type DeepClone()
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, this);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as Type;
            }
        }
    }
}
