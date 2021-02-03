using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class Namespace : ReflectionItem
    {
        public List<Type> Types { get; set; }

        public override void Build(ECMAStore store)
        {
            Id = Name;
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
